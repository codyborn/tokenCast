using System;
using System.Collections.Generic;

namespace TokenCast.Models
{
    public static class CanviaConfig
    {
        public static readonly string clientId = "659fb248-18dc-497e-9b31-3419935e555f";
        
        private static readonly string canviaBackendOAuthURL = "https://prod.palacio.life/backend/oauth";

        private static readonly string baseEndpoint = "https://prod.palacio.life/backend/api/v1";
        
        public static readonly string listDevicesURL = String.Concat(baseEndpoint, "/devices");
        
        public static readonly string accessCodeUri = String.Concat(canviaBackendOAuthURL, "/token");
        
        public static readonly string accessCodeToJWTUri = String.Concat(baseEndpoint, "/auth/login/token");
        
        public static readonly string castToDeviceUri = String.Concat(baseEndpoint, "/devices/queue_operations");
    }
}