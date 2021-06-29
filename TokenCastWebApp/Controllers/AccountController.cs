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
using Nethereum.Web3;
using Newtonsoft.Json;
using TokenCast.Models;

namespace TokenCast.Controllers
{
    public class AccountController : Controller
    {
        // "TokenCast - proof of ownership. Please sign this message to prove ownership over your Ethereum account."
        private const string ownershipProofMessage = "0x910d6ebf53e411666eb4658ae60b40ebd078e44b5dc66d353d7ceac05900a2b6";
        private const string rawMessage = "TokenCast - proof of ownership. Please sign this message to prove ownership over your Ethereum account.";
        private const int tokensPerPage = 50;
        private const string communityWallet = "0x7652491068B53B5e1193b8DAA160b43C8a622eE9";

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        // GET: Account/Details?address=0xEeA95EdFC25F15C0c44d4081BBd85026ba298Dc6&signature=...
        // https://developpaper.com/use-taifang-block-chain-to-ensure-asp-net-cores-api-security-part-2/
        public AccountModel Details(string address, string signature, string network)
        {
            if (!AuthCheck(address, signature, network))
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
        public bool AddDevice(string address, string signature, string network, string deviceId)
        {
            if (!AuthCheck(address, signature, network))
            {
                return false;
            }

            Database.AddDevice(address, deviceId).Wait();
            return true;
        }

        public bool DeleteDevice(string address, string signature, string network, string deviceId)
        {
            if (!AuthCheck(address, signature, network))
            {
                return false;
            }

            Database.DeleteDevice(address, deviceId).Wait();
            return true;
        }

        public bool AddDeviceAlias(string address, string signature, string network, string deviceId, string alias)
        {
            if (!AuthCheck(address, signature, network))
            {
                return false;
            }

            Database.AddDeviceAlias(address, deviceId, alias).Wait();
            return true;
        }

        // POST Account/Details?address=0xEeA95EdFC25F15C0c44d4081BBd85026ba298Dc6&signature=...&deviceId=doge...
        /// <summary>
        /// Sets content for the device to display
        /// </summary>
        /// <param name="address">user address</param>
        /// <param name="signature">user signature</param>
        /// <param name="deviceDisplay">details for displaying content</param>
        /// <param name="network">Ethereum or Tezos address indication</param>
        /// <returns>success status</returns>
        [HttpPost]
        public bool SetDeviceContent(string address,
            string signature,
            string network,
            string whitelabeler,
            [FromForm] DeviceModel deviceDisplay)
        {
            if (!AuthCheck(address, signature, network))
            {
                return false;
            }

            Database.SetDeviceContent(deviceDisplay).Wait();
            return true;
        }


        [HttpPost]
        public bool RemoveDeviceContent(string address,
            string signature,
            string deviceId,
            string network)
        {
            if (!AuthCheck(address, signature, network))
            {
                return false;
            }

            Database.RemoveDeviceContent(deviceId).Wait();
            return true;
        }

        public string Tokens(string address, string signature, string network)
        {
            if (!AuthCheck(address, signature, network))
            {
                return string.Empty;
            }

            TokenList tokenList = new TokenList();
            tokenList.assets = new List<Token>();
            if (network.Equals("ETHEREUM", StringComparison.OrdinalIgnoreCase))
            {
                addEthereumTokensForUser(tokenList, address);
            }
            else
            {
                // addTezosTokensForUser(tokenList, "tz1NDaRsyv7BkRTMxipcWc4mtbxii62XsxZE");
            }

            // Remove spam
            tokenList.assets.RemoveAll((t) => t.asset_contract.address == "0xc20cf2cda05d2355e218cb59f119e3948da65dfa");
            addTezosTokensForUser(tokenList, address);

            return JsonConvert.SerializeObject(tokenList);
        }

        public string CommunityTokens()
        {
            TokenList tokenList = new TokenList();
            tokenList.assets = new List<Token>();
            addEthereumTokensForUser(tokenList, communityWallet, lookupPrices:true);
            foreach(Token token in tokenList.assets)
            {
                token.description += " - Owned by TokenCast Community";
            }
            return JsonConvert.SerializeObject(tokenList);
        }

        private void addEthereumTokensForUser(TokenList tokenList, string address, bool lookupPrices = false)
        {
            TokenList nextSet = new TokenList();
            int currPage = 0;
            do
            {
                int offset = currPage++ * tokensPerPage;
                Uri openSeaAPI = new Uri($"https://api.opensea.io/api/v1/assets/?owner={address}&limit={tokensPerPage}&offset={offset}");
                HttpClient client = new HttpClient();
                var response = client.GetAsync(openSeaAPI).Result;
                if (response.IsSuccessStatusCode)
                {
                    string jsonList = response.Content.ReadAsStringAsync().Result;
                    nextSet = JsonConvert.DeserializeObject<TokenList>(jsonList);
                    tokenList.assets.AddRange(nextSet.assets);
                }
            }
            while (nextSet.assets != null && nextSet.assets.Count > 0);

            // Add prices
            if (lookupPrices)
            {
                Dictionary<long, string> priceMap = getTokenPrices(address);
                // Consolidate into a single object
                foreach (Token token in tokenList.assets)
                {
                    if (priceMap.ContainsKey(token.id))
                    {
                        token.current_price = priceMap[token.id];
                    }
                }
            }
        }

        private void addTezosTokensForUser(TokenList tokenList, string address)
        {
            Uri hicetnuncAPI = new Uri($"https://51rknuvw76.execute-api.us-east-1.amazonaws.com/dev/tz?tz={address}");
            HttpClient client = new HttpClient();
            var response = client.GetAsync(hicetnuncAPI).Result;
            if (response.IsSuccessStatusCode)
            {
                string jsonList = response.Content.ReadAsStringAsync().Result;
                TezosTokenList tezosTokens = JsonConvert.DeserializeObject<TezosTokenList>(jsonList);
                foreach(TezosToken tezosToken in tezosTokens.result)
                {
                    Token transformedToken = new Token();
                    // transformedToken.current_price = tezosToken.price.ToString();
                    transformedToken.id = tezosToken.piece;
                    transformedToken.name = tezosToken.token_info.name;
                    transformedToken.description = tezosToken.token_info.description;
                    string imageUrl = ipfsToHttp(tezosToken.token_info.displayUri);
                    transformedToken.image_original_url = imageUrl;
                    transformedToken.image_url = imageUrl;
                    if (tezosToken.token_info.formats.Count > 0)
                    {
                        MediaFormat format = tezosToken.token_info.formats.First();
                        if (format.mimeType == "video/mp4")
                        {
                            // Add "mp4" to the end to make it easy to determine the content type
                            string url = string.Concat(ipfsToHttp(format.uri), "?type=mp4");
                            transformedToken.animation_url = url;
                            transformedToken.image_url = url;
                        }
                        else
                        {
                            string url = ipfsToHttp(format.uri);
                            transformedToken.image_original_url = url;
                            transformedToken.image_url = url;
                        }
                    }
                    tokenList.assets.Add(transformedToken);
                }
            }
        }

        private string ipfsToHttp(string endpoint)
        {
            if (String.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith("https://") || endpoint.StartsWith("http://"))
            {
                return endpoint;
            }
            if (!endpoint.StartsWith("ipfs://"))
            {
                throw new Exception($"Expected {endpoint} to be an IPFS endpoint");
            }
            string id = endpoint.Substring("ipfs://".Length);
            return $"https://ipfs.io/ipfs/{id}";
        }

        private Dictionary<long, string> getTokenPrices(string address)
        {
            OrderList orderList = new OrderList();
            orderList.orders = new List<Order>();
            OrderList nextSet = new OrderList();
            int currPage = 0;
            do
            {
                int offset = currPage++ * tokensPerPage;
                Uri openSeaAPI = new Uri($"https://api.opensea.io/wyvern/v1/orders?side=1&owner={address}&limit={tokensPerPage}&offset={offset}");
                HttpClient client = new HttpClient();
                var response = client.GetAsync(openSeaAPI).Result;
                if (response.IsSuccessStatusCode)
                {
                    string jsonList = response.Content.ReadAsStringAsync().Result;
                    nextSet = JsonConvert.DeserializeObject<OrderList>(jsonList);
                    // Filter out orders not originating from owner
                    orderList.orders.AddRange(
                        nextSet.orders.Where((o)=> o != null && 
                                                o.maker != null && 
                                                o.maker.address != null && 
                                                o.maker.address.Equals(address, StringComparison.OrdinalIgnoreCase))
                    );
                }
            }
            while (nextSet.orders.Count > 0);

            Dictionary<long, string> priceMap = new Dictionary<long, string>();
            foreach (Order order in orderList.orders)
            {
                priceMap[order.asset.id] = order.current_price;
            }
            return priceMap;
        }

        public class TokenList
        {
            public List<Token> assets { get; set; }
        }

        public class Token
        {
            public long id { get; set; }
            public string token_id { get; set; }
            public string background_color { get; set; }
            public string image_url { get; set; }
            public string image_original_url { get; set; }
            public string animation_url { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string external_link { get; set; }
            public string permalink { get; set; }
            public Owner owner { get; set; }
            public AssetContract asset_contract { get; set; }
            public string current_price { get; set; }
        }
        public class TezosTokenList
        {
            public List<TezosToken> result { get; set; }
        }


        public class TezosToken
        {
            public long price { get; set; }
            public long piece { get; set; }
            public TokenInfo token_info { get; set; }
        }

        public class TokenInfo
        {
            public string name { get; set; }
            public string description { get; set; }
            public string artifactUri { get; set; }
            public string displayUri { get; set; }
            public string thumbnailUri { get; set; }
            public List<MediaFormat> formats { get;set;}
        }

        public class MediaFormat
        {
            public string uri { get; set; }
            public string mimeType { get; set; }
        }

        public class Owner
        {
            public string address { get; set; }
        }

        public class AssetContract
        {
            public string address { get; set; }
        }

        public class OrderList
        {
            public List<Order> orders { get; set; }
        }

        public class Order
        {
            public string current_price { get; set; }
            public Maker maker { get; set; }
            public Asset asset { get; set; }
        }

        public class Maker
        {
            public string address { get; set; }
        }

        public class Asset
        {
            public long id { get; set; }
        }

        private bool AuthCheck(string address, string signature, string network)
        {
            //replace with authentication (below) 
            if (network.Equals("TEZOS", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (signature == null || signature.Equals("null"))
            {
                return false;
            }
            MessageSigner signer = new MessageSigner();
            string signerAddress = signer.EcRecover(ownershipProofMessage.HexToByteArray(), signature);
            if (signerAddress.Equals(address, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return EIP1271AuthCheck(address, rawMessage, signature);
        }

        private bool EIP1271AuthCheck(string address, string rawMessage, string signature)
        {

            var abi = @"[{'constant':true,'inputs':[{'name':'_messageHash','type':'bytes'},{'name':'_signature','type':'bytes'}],'name':'isValidSignature','outputs':[{'name':'magicValue','type':'bytes4'}],'payable':false,'stateMutability':'view','type':'function'}]";

            var web3 = new Web3("https://mainnet.infura.io/v3/9d5e849c49914b7092581cc71e3c2580");
            var contract = web3.Eth.GetContract(abi, address);
            var magicValue = "20c13b0b";

            var messageInBytes = Encoding.ASCII.GetBytes(rawMessage);
            var isValidSignatureFunction = contract.GetFunction("isValidSignature");
            var result = isValidSignatureFunction.CallAsync<byte[]>(messageInBytes, signature.HexToByteArray()).Result;
            if (result == null)
            {
                return false;
            }
            string hexResult = result.ToHex();
            return hexResult.Equals(magicValue, StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}