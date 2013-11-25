using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SqlMergeScriptGenerator {
    class Program {
        public static T DeserializeJsonObjectFromString<T>(string jsonString) {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream()) {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(jsonString);
                writer.Flush();
                stream.Position = 0;
                return (T)ser.ReadObject(stream);
            }
        }

        public static IEnumerable<string> ReadLinesNoComments(string file) {
            var lines = new List<string>();
            using (var sr = new StreamReader(file)) {
                string line;
                while ((line = sr.ReadLine()) != null) {
                    if (!line.StartsWith(@"//")) {
                        lines.Add(line);
                    }
                }
            }
            return lines.AsEnumerable<string>();
        }

        static void Main(string[] args) {
            // Read FilesToInclude to get the files to use
            var files = ReadLinesNoComments(ConfigurationManager.AppSettings["FilesToIncludeTextfile"]);
            var allmerges = "";
            foreach (var file in files) {
                var jsonData = DeserializeJsonObjectFromString<MergeScriptFileData>(String.Join("\n", ReadLinesNoComments(file)));
                var tableName = jsonData.TableName;
                var columnNames = jsonData.Columns.Select(c => c.Name);
                var types = jsonData.Columns.Select(c => c.Type).ToList();
                var merge = String.Format("-- Merge Auto-Generated for {0} ON [{1}]\n", tableName, DateTime.Now);
                
                foreach (var d in jsonData.Data) {
                    for (int i = 0; i < types.Count; i++) {
                        if(types[i] == "string") {
                            d[i] = String.Format("'{0}'", d[i].Replace("'","''")); // Escape strings in single quotes so that they go into sql correctly
                        }
                    }
                }

                var nonPrimaryKeys = columnNames.Where(col => jsonData.PrimaryKeys.IndexOf(col) == -1);

                // Add premerge statements
                merge += (String.Join("\n", jsonData.PreMergeStatements) + "\n");

                // Create merge statement
                merge += String.Format(";MERGE INTO {0} AS tgt\n", jsonData.TableName);
                merge += "USING";
                merge += "( VALUES\n";

                merge += String.Join(",\n", jsonData.Data.Select(d => String.Format("\t({0})", String.Join(", ", d))));

                merge += "\n) AS src (\n";
                merge += String.Join(",\n", columnNames.Select(c => String.Format("\t\t{0}", c)));
                merge += ")\n";

                merge += "\tON ";
                merge += (String.Join(" AND ", jsonData.PrimaryKeys.Select(pk => String.Format("tgt.{0} = src.{1}", pk, pk))) + "\n");

                if (nonPrimaryKeys.Count() > 0) {
                    merge += "WHEN MATCHED THEN\n";
                    merge += "\tUPDATE SET\n";
                    merge += (String.Join(",\n", nonPrimaryKeys.Select(npk => String.Format("\t\ttgt.{0} = src.{1}", npk, npk))) + "\n");
                }

                merge += "WHEN NOT MATCHED THEN\n";
                merge += "\tINSERT (";
                merge += String.Join(", ", columnNames);
                merge += ")\n";
                merge += "\tVALUES (\n";
                merge += (String.Join(",\n", columnNames.Select(c => String.Format("\t\tsrc.{0}", c))) + "\n");
                merge += "\t)\n";
                merge += ";\n";

                // Add post merge statements
                merge += (String.Join("\n", jsonData.PostMergeStatements) + "\n\n");

                Console.WriteLine(String.Format("Created {0} data...", jsonData.TableName));

                // Add the merge statement to the overall one
                allmerges += merge;
            }
            // Save the merge script
            var dir = Directory.GetCurrentDirectory();
            string path = Path.Combine(dir,"MergeScript.sql");
            using (StreamWriter sw = new StreamWriter(path)) {
                sw.Write(allmerges);
            }
            Console.WriteLine(String.Format("Saved {0}", path));
            Console.ReadLine();
        }
    }

    [DataContract]
    class MergeScriptFileData {
        [DataMember]
        public string TableName { get; set; }
        [DataMember]
        public List<Column> Columns { get; set; }
        [DataMember]
        public List<string> PrimaryKeys { get; set; }
        [DataMember]
        public List<string> PreMergeStatements { get; set; }
        [DataMember]
        public List<string> PostMergeStatements { get; set; }
        [DataMember]
        public List<List<string>> Data { get; set; }
    }

    [DataContract]
    class Column {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Type { get; set; }
    }
}
