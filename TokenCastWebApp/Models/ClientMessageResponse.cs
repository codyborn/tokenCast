namespace TokenCastWebApp.Models
{
    public sealed class ClientMessageResponse
    {
        public bool Success { get; set; }
        public EventType Event { get; set; }
        public string Message { get; set; }
        public string DeviceId { get; set; }
    }

    public enum EventType
    {
        Heartbeat,
        NFTUpdated,
        Online,
        Offline
    }
}
