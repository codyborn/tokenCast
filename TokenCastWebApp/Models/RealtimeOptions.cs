using Microsoft.Extensions.Options;

namespace TokenCastWebApp.Models
{
    public class RealtimeOptions : BaseOptions
    {
        public int CacheItemExpirationTime { get; set; } = 60;

        public int ExpirationScanFrequency { get; set; } = 60;
    }

    public class BaseOptions : IOptions
    {
        public string ApiName { get; set; }

        public string IdentityUrl { get; set; }
    }

    public interface IOptions { }

}
