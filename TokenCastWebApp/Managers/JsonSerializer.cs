using System.Text.Json;
using TokenCastWebApp.Managers.Interfaces;

namespace TokenCastWebApp.Managers
{
    public class DefaultJsonSerializer : IJsonSerializer
    {
        static JsonSerializerOptions _serializationOptions;
        static DefaultJsonSerializer()
        {
            _serializationOptions = SerializationOptionsFactory.GetBaseOptions();
            foreach (var converter in SerializationOptionsFactory.GetNumberConverters())
                _serializationOptions.Converters.Add(converter);
        }

        public TOut Deserialize<TOut>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TOut>(json, _serializationOptions);
        }

        public string Serialize<TIn>(TIn item)
        {
            return System.Text.Json.JsonSerializer.Serialize(item, item.GetType(), _serializationOptions);
        }
    }

    public class SystemTextJsonSerializer : ISystemTextJsonSerializer
    {
        readonly JsonSerializerOptions _serializationOptions;
        public SystemTextJsonSerializer()
        {
            _serializationOptions = SerializationOptionsFactory.GetBaseOptions();
            foreach (var converter in SerializationOptionsFactory.GetNumberConverters())
                _serializationOptions.Converters.Add(converter);
        }

        public JsonSerializerOptions Options => _serializationOptions;

        public TOut Deserialize<TOut>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TOut>(json, _serializationOptions);
        }

        public string Serialize<TIn>(TIn item)
        {
            return System.Text.Json.JsonSerializer.Serialize(item, item.GetType(), _serializationOptions);
        }

        public TOut Deserialize<TOut>(byte[] json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TOut>(json, _serializationOptions);
        }

        public byte[] SerializeToBytes<TIn>(TIn item)
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(item, item.GetType(), _serializationOptions);
        }
    }
}
