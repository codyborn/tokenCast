using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TokenCast.Models;

namespace TokenCast.Controllers
{
    public static class CanviaController
    {
        private const string AccessToken = "x-access-token";
        private const string UserId = "x-user-id";
        private const string Operation = "operation";
        private const string Device = "device";
        private const string Title = "title";
        private const string Details = "details";
        private const string Thumbnail = "thumbnail";
        private const string PushFront = "push_front";
        private const string Url = "url";
        private const string AppJson = "application/json";
        private const string AppxUrlencoded = "application/x-www-form-urlencoded";
        private const string ClientId = "client_id";
        private const string ClientSecret = "client_secret";
        private const string RefreshToken = "refresh_token";
        private const string GrantType = "grant_type";
        private const string Code = "code";
        private const string AuthorizationCode = "authorization_code";
        private const string AuthScheme = "Bearer";

        public static HttpStatusCode CastToCanviaDevice(CanviaAccount canviaAccount, DeviceModel deviceModel)
        {

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add(AccessToken, canviaAccount?.jwt);
            client.DefaultRequestHeaders.Add(UserId, canviaAccount?.email);

            var body = new Dictionary<string, string>()
            {
                {Url, deviceModel.currentDisplay.tokenImageUrl.ToString()},
                {Operation, PushFront},
                {Device, deviceModel.id},
                {Title, deviceModel.currentDisplay.tokenName},
                {Details, deviceModel.currentDisplay.tokenMetadata},
                {Thumbnail, deviceModel.currentDisplay.tokenImageUrl.ToString()}
            };

            var json = JsonConvert.SerializeObject(body, Formatting.Indented);
            var httpContent = new StringContent(json, Encoding.UTF8, AppJson);
            var response = client.PostAsync(CanviaConfig.castToDeviceUri, httpContent).Result;
            return response.StatusCode;
        }

        public static bool GetCanviaDevices(CanviaAccount canviaAccount)
        {
            if (canviaAccount == null)
            {
                return false;
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue(AppJson));
            client.DefaultRequestHeaders.Add(AccessToken, canviaAccount.jwt);
            client.DefaultRequestHeaders.Add(UserId, canviaAccount.email);
            var response = client.GetAsync(CanviaConfig.listDevicesURL).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var canviaDevices = JsonConvert.DeserializeObject<List<CanviaDevice>>(responseString);

            var devices = canviaDevices.ToDictionary(device => device.device_name, device => device.device_id);

            canviaAccount.canviaDevices = devices;
            
            return true;
        }

        public static bool SetCanviaAccessAndRefreshTokens(CanviaAccount canviaAccount, bool justRefresh)
        {
            var canviaSecret = AppSettings.LoadAppSettings().CanviaSecret;
            var queryParams = new Dictionary<string, string>()
            {
                {ClientId, CanviaConfig.clientId},
                {ClientSecret, canviaSecret}
            };

            if (justRefresh)
            {
                queryParams.Add(RefreshToken, canviaAccount.refreshToken);
                queryParams.Add(GrantType, RefreshToken);
            }
            else
            {
                queryParams.Add(Code, canviaAccount.code);
                queryParams.Add(GrantType, AuthorizationCode);

            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue(AppxUrlencoded));

            var response = client.PostAsync(CanviaConfig.accessCodeUri, new FormUrlEncodedContent(queryParams)).Result;
            var responseString = response?.Content.ReadAsStringAsync().Result;

            if (response == null || !response.IsSuccessStatusCode)
            {
                return false;
            }
            
            dynamic data = JObject.Parse(responseString);
            canviaAccount.accessToken = data.access_token.ToString();
            canviaAccount.refreshToken = data.refresh_token.ToString();
            return true;
        }

        public static bool SetCanviaJWTAndUserId(CanviaAccount canviaAccount)
        {
            if (canviaAccount.accessToken == null)
            {
                return false;
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthScheme, canviaAccount.accessToken);

            var response = client.PostAsync(CanviaConfig.accessCodeToJWTUri, null).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            if (response == null || !response.IsSuccessStatusCode)
            {
                return false;
            }
            
            dynamic data = JObject.Parse(responseString);
            canviaAccount.jwt = data.token;
            canviaAccount.email = data.email;
            return true;
        }

        private class CanviaDevice
        {
            public string device_name { get; set; }
            public string user_id { get; set; }
            public string device_id { get; set; }
        }
    }
}