using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Org.BouncyCastle.Crypto.Tls;
using TokenCast.Controllers;

namespace TokenCast.Models
{
    public class AccountModel
    {
        public AccountModel(string address)
        {
            this.address = address.ToLowerInvariant();
            devices = new List<string>();
            canviaAccount = new CanviaAccount();
        }

        // needed for deserialization 
        public AccountModel()
        {
        }

        public CanviaAccount canviaAccount { get; set; }
        public string address { get; set; }
        public List<string> devices { get; set; }
        public Dictionary<string, string> deviceMapping { get; set; }
        public Dictionary<string, bool> devicesOnline { get; set; }


    }
    
    public class CanviaAccount
    {
        public string email { get; set; }
        
        public string jwt { get; set; }
        
        public string accessToken { get; set; }
        
        public string refreshToken { get; set; }
        
        public string code { get; set; }
        
        public Dictionary<string, string> canviaDevices { get; set; }

        
        public bool OAuth(bool justRefresh)
        {
            if (CanviaController.SetCanviaAccessAndRefreshTokens(this, justRefresh))
            {
                return CanviaController.SetCanviaJWTAndUserId(this);
            }

            return false;
        }
    }
}
