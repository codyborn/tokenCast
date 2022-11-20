using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TokenCast
{
    using Microsoft.Extensions.Configuration;
    public class AppSettings
    {
        public string StorageConnectionString { get; set; }
        public string IPFSSecret { get; set; }
        public string CanviaSecret { get; set; }
        public string AlchemySecret { get; set; } = "Pv5OZnvrHBMn7XMxe6GaCtPEoo3c0ing";
        public static AppSettings LoadAppSettings()
        {
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            AppSettings appSettings = configRoot.Get<AppSettings>();
            return appSettings;
        }
    }
}
