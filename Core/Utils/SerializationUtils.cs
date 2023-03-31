using Newtonsoft.Json.Linq;

namespace HentaiChanBot.Utils {
    internal static class SerializationUtils {
        public static string SerializeAsUri(object obj) {
            var json = JObject.FromObject(obj);
            var str = string.Empty;
            foreach (var prop in json.Properties()) {
                str += $"&{prop.Name}={prop.Value}";
            }
            return str;
        }
    }
}
