using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TokenCast.Models
{
    public class AccountModel
    {
        public AccountModel(string address)
        {
            this.address = address.ToLowerInvariant();
            this.devices = new List<string>();
        }

        public AccountModel()
        { 
        }

        public string address { get; set; }
        public List<string> devices { get; set; }
        public Dictionary<string, string> deviceMapping { get; set; }
    }
}
