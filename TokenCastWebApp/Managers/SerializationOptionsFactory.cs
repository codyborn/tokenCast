using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TokenCastWebApp.Managers
{
    public class SerializationOptionsFactory
    {
        public static JsonSerializerOptions GetBaseOptions()
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            };
            return options;
        }

        public static IEnumerable<JsonConverter> GetNumberConverters()
        {
            yield return new JsonStringEnumConverter();
        }
    }
}
