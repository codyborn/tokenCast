using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using TokenCast.Models;

namespace TokenCast.Controllers
{
    public class AccountController : Controller
    {
        // "TokenCast - proof of ownership. Please sign this message to prove ownership over your Ethereum account."
        private const string ownershipProofMessage = "0x910d6ebf53e411666eb4658ae60b40ebd078e44b5dc66d353d7ceac05900a2b6";

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        // GET: Account/Details?address=0xEeA95EdFC25F15C0c44d4081BBd85026ba298Dc6&signature=...
        // https://developpaper.com/use-taifang-block-chain-to-ensure-asp-net-cores-api-security-part-2/
        public AccountModel Details(string address, string signature)
        {
            if (!AuthCheck(address, signature))
            {
                return null;
            }

            AccountModel account = Database.GetAccount(address).Result;
            if (account == null)
            {
                account = new AccountModel(address);
                Database.CreateOrUpdateAccount(account).Wait();
            }

            return account;
        }

        // POST Account/Details?address=0xEeA95EdFC25F15C0c44d4081BBd85026ba298Dc6&signature=...&deviceId=doge...
        [HttpPost]
        public bool AddDevice(string address, string signature, string deviceId)
        {
            if (!AuthCheck(address, signature))
            {
                return false;
            }

            Database.AddDevice(address, deviceId).Wait();
            return true;
        }

        // POST Account/Details?address=0xEeA95EdFC25F15C0c44d4081BBd85026ba298Dc6&signature=...&deviceId=doge...
        /// <summary>
        /// Sets content for the device to display
        /// </summary>
        /// <param name="address">user address</param>
        /// <param name="signature">user signature</param>
        /// <param name="deviceDisplay">details for displaying content</param>
        /// <returns>success status</returns>
        [HttpPost]
        public bool SetDeviceContent(string address,
            string signature,
            [FromForm] DeviceModel deviceDisplay)
        {
            if (!AuthCheck(address, signature))
            {
                return false;
            }

            Database.SetDeviceContent(deviceDisplay).Wait();
            return true;
        }


        [HttpPost]
        public bool RemoveDeviceContent(string address,
            string signature,
            string deviceId)
        {
            if (!AuthCheck(address, signature))
            {
                return false;
            }

            Database.RemoveDeviceContent(deviceId).Wait();
            return true;
        }

        public string Tokens(string address, string signature)
        {
            if (!AuthCheck(address, signature))
            {
                return string.Empty;
            }

            Uri openSeaAPI = new Uri("https://api.opensea.io/api/v1/assets/?owner=" + address);
            HttpClient client = new HttpClient();
            var response = client.GetAsync(openSeaAPI).Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }

            return string.Empty;
        }

        private bool AuthCheck(string address, string signature)
        {
            MessageSigner signer = new MessageSigner();
            string signerAddress = signer.EcRecover(ownershipProofMessage.HexToByteArray(), signature);
            return signerAddress.Equals(address, StringComparison.OrdinalIgnoreCase);
        }
    }
}