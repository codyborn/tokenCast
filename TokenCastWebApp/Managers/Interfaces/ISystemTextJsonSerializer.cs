namespace TokenCastWebApp.Managers.Interfaces
{
    public interface IJsonSerializer
    {
        TOut Deserialize<TOut>(string json);
        string Serialize<TIn>(TIn item);
    }

    public interface ISystemTextJsonSerializer : IJsonSerializer
    {
        TOut Deserialize<TOut>(byte[] json);
        byte[] SerializeToBytes<TIn>(TIn item);
    }
}
