using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TokenCast;

namespace TokenCastWebApp.Controllers
{
    public class MediaController : Controller
    {
        // Proxy the IPFS data through this server
        // for preventing CORS restrictions
        public HttpContent Index(string ipfsHash)
        {
            string projectId = "252N9pW5n3IDsqq4ws3kU2HU8lZ";
            string projectSecret = AppSettings.LoadAppSettings().IPFSSecret;
            String ipfsUri = $"https://ipfs.infura.io:5001/api/v0/get/{ipfsHash}";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Base64Encode(projectId + ":" + projectSecret));
            var response = client.GetAsync(ipfsUri).Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content;
            }
            return null;
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
