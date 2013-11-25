using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Web.Script.Serialization;

namespace UseJson {
    class Program {
        static void Main(string[] args) {
            var jsonStringA = @"{
	""ColumnNames"": [""StatusId"", ""Name""],
	""Types"": [""int"", ""string""],
	""PrimaryKeys"": [""StatusId""],
	""Data"":""ABC""
}";
            var data = DeserializeObject<MergeScriptFileData>(jsonStringA);

            var serializer = new JavaScriptSerializer();
            using (var sr = new StreamReader("JsonExample1.txt")) {
                var jsonStringB = sr.ReadToEnd();
                Dictionary<string, dynamic> d = (Dictionary<string, dynamic>) serializer.DeserializeObject(jsonStringB);
            }

            System.Json.JsonObject
            
        }

        public static T DeserializeObject<T>(string jsonString) {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream()) {
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(jsonString);
                writer.Flush();
                stream.Position = 0;
                return (T) ser.ReadObject(stream);
            }
        }
    }

    


    [DataContract]
    class MergeScriptFileData {
        [DataMember]
        public List<string> ColumnNames { get; set; }
        [DataMember]
        public List<string> Types { get; set; }
        [DataMember]
        public List<string> PrimaryKeys { get; set; }
        [DataMember]
        public string Data { get; set; }
    }
}
