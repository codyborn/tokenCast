using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Web3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TokenCast.Models;

namespace TokenCast.Controllers
{
    public class AccountController : Controller
    {
        private const string Ethereum = "ETHEREUM";
        
        private class MessageHash
        {
            public string rawMessage;
            public string hash;
        }
        // "TokenCast - proof of ownership. Please sign this message to prove ownership over your Ethereum account."
        private Dictionary<string, MessageHash> ownershipProofMessages = new Dictionary<string, MessageHash>
        {
            { "default", new MessageHash {
                    rawMessage = "TokenCast - proof of ownership. Please sign this message to prove ownership over your Ethereum account.",
                    hash = "0x910d6ebf53e411666eb4658ae60b40ebd078e44b5dc66d353d7ceac05900a2b6"
                }
            },
            { "canvia", new MessageHash {
                    rawMessage = "Canvia NFT display- proof of ownership: please sign this message to prove ownership over your Ethereum account.",
                    hash = "0x35f3aaf63a693a0781023dc10da3d7b200b1304dce97f942dd521fd20ba345b6"
                }
            },
            { "divine", new MessageHash {
                    rawMessage = "Divine - proof of ownership. Please sign this message to prove ownership over your Ethereum account.",
                    hash = "0x25ee7774f6c184b3483ca5d5026d8ac0ded6cb099489a4f0a6ed3124504557b4"
                }
            },
            { "strangepainters", new MessageHash {
                    rawMessage = "NFTCaster - proof of ownership. Please sign this message to prove ownership over your Ethereum account.",
                    hash = "0x0b6b4e1ebf84920d15af7bba16de65a8a3ae78051c7ed63ea99c676eb60e53ba"
                }
            },
            { "blockframenft", new MessageHash {
                    rawMessage = "BlockFrameNFT - proof of ownership. Please sign this message to prove ownership over your Ethereum account.",
                    hash = "0xcaaf7ebd33b7a5ee87e75ac5412e6ef1eb074151118049fea881f398bcadf6da"
                }
            },
            { "nftframe", new MessageHash {
                    rawMessage = "NFT Frame - proof of ownership. Please sign this message to prove ownership over your Ethereum account.",
                    hash = "0x9b4f4b06a917c3232dedfcf9a09dc8757d529df6a33f1e0168bdd0c906faaa5f"
                }
            },
        };

        private const int tokensPerPage = 50;
        private const string communityWalletEthereum = "0x7652491068B53B5e1193b8DAA160b43C8a622eE9";
        private const string communityWalletTezos = "tz1Ue6z4cGtZTE8bXC5Ka8pTxnUd7q7Rpmi9";

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        // GET: Account/Details?address=0xEeA95EdFC25F15C0c44d4081BBd85026ba298Dc6&signature=...
        // https://developpaper.com/use-taifang-block-chain-to-ensure-asp-net-cores-api-security-part-2/
        public AccountModel Details(string address, string signature, string whitelabeler, string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
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
        public bool AddDevice(string address, string signature, string deviceId, string whitelabeler, string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return false;
            }

            Database.AddDevice(address, deviceId).Wait();
            return true;
        }
        
        [HttpPost]
        public bool AddCanviaDevices(string address, string signature, string code, string whitelabeler, string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return false;
            }

            var account = Database.GetAccount(address).Result;
            var currentCanviaDevices = account.canviaAccount?.canviaDevices;
            account.canviaAccount = new CanviaAccount { code = code };

            if (!account.canviaAccount.OAuth(false))
            {
                return false;
            }
            
            RemoveCanviaDevices(currentCanviaDevices, account, whitelabeler);
            
            if (!CanviaController.GetCanviaDevices(account.canviaAccount))
            {
                return false;
            }
            
            Database.CreateOrUpdateAccount(account).Wait();
            Database.AddCanviaDevicesToAccount(address).Wait();
            
            return true;
        }
        
        private static void RemoveCanviaDevices(Dictionary<string, string> canviaDevices, AccountModel account, string whitelabeler)
        {
            if (canviaDevices != null)
            {
                foreach (KeyValuePair<string, string> device in canviaDevices)
                {
                    if (account.devices.Contains(device.Value))
                    {
                        account.devices.Remove(device.Value);
                    }
                }
            }
        }

        public bool DeleteDevice(string address, string signature, string deviceId, string whitelabeler, string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return false;
            }

            Database.DeleteDevice(address, deviceId).Wait();
            return true;
        }

        public bool UpdateDevice(string address, string signature, string deviceId, string alias, int frequency, string whitelabeler, string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return false;
            }

            if (alias != null)
            {
                Database.AddDeviceAlias(address, deviceId, alias).Wait();
            }

            Database.UpdateDeviceFrequency(deviceId, frequency).Wait();

            return true;
        }        
        
        public string GetDeviceFrequency(string address, string signature, string deviceId, string whitelabeler, string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return string.Empty;
            }

            var device = Database.GetDeviceContent(deviceId).Result;

            return JsonConvert.SerializeObject(device.frequencyOfRotation);
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
            string whitelabeler,
            [FromForm] DeviceModel deviceDisplay,
            string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return false;
            }

            DeviceModel prevDevice = Database.GetDeviceContent(deviceDisplay.id).Result;
            if (prevDevice == null)
            {
                return false;
            }

            if (prevDevice.isCanviaDevice)
            {
                var account = Database.GetAccount(address).Result;
                var responseCode = CanviaController.CastToCanviaDevice(account.canviaAccount, deviceDisplay);
                
                if (responseCode != HttpStatusCode.Unauthorized)
                {
                    return responseCode == HttpStatusCode.OK;
                }

                if (account.canviaAccount.OAuth(true))
                {
                    return CanviaController.CastToCanviaDevice(account.canviaAccount, deviceDisplay) == HttpStatusCode.OK;
                }

                return false;
            }

            Database.AddDeviceContent(deviceDisplay).Wait();
            return true;
        }


        [HttpPost]
        public bool RemoveDeviceContent(string address,
            string signature,
            string deviceId,
            string whitelabeler,
            string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return false;
            }

            Database.RemoveDeviceContent(deviceId).Wait();
            return true;
        }        
        
        [HttpPost]
        public string RemoveIndexFromQueue(string address,
            string signature,
            string deviceId,
            int index,
            string whitelabeler,
            string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return string.Empty;
            }

            var deviceModel = Database.GetDeviceContent(deviceId).Result;

            if (index > deviceModel.castedTokens.Count)
            {
                return string.Empty;
            }

            Database.RemoveATokenFromDevice(deviceId, index).Wait();
            var updatedDevice = Database.GetDeviceContent(deviceId).Result;
            
            return JsonConvert.SerializeObject(updatedDevice.castedTokens);
        }
        
        [HttpPost]
        public string GetCastedTokensForDevice(string address,
            string signature,
            string deviceId,
            string whitelabeler,
            string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return string.Empty;
            }

            DeviceModel deviceModel = Database.GetDeviceContent(deviceId).Result;

            return JsonConvert.SerializeObject(deviceModel.castedTokens);
        }

        public bool ReorderQueuedCastedTokens(string address,
            string signature,
            string deviceId,
            string whitelabeler,
            int[] order,
            string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return false;
            }
            
            DeviceModel device = Database.GetDeviceContent(deviceId).Result;
            
            if (device?.castedTokens != null)
            {
                Database.ReorderCastedTokensOnDevice(device, order).Wait();
                return true;
            }

            return false;
        }

        public string Tokens(string address, string signature, string whitelabeler, string network = Ethereum)
        {
            if (!AuthCheck(address, signature, network, whitelabeler))
            {
                return string.Empty;
            }

            TokenList tokenList = new TokenList();
            tokenList.assets = new List<Token>();
            if (network.Equals("TEZOS", StringComparison.OrdinalIgnoreCase))
            {
                addTezosTokensForUser(tokenList, address);
            }
            else
            {
                addEthereumTokensForUser(tokenList, address);
                addPolygonTokensForUser(tokenList, address);

                // Remove spam
                tokenList.assets.RemoveAll((t) => t.asset_contract != null && t.asset_contract.address == "0xc20cf2cda05d2355e218cb59f119e3948da65dfa");
            }

            return JsonConvert.SerializeObject(tokenList);
        }

        public string CommunityTokens()
        {
            TokenList tokenList = new TokenList();
            tokenList.assets = new List<Token>();
            addEthereumTokensForUser(tokenList, communityWalletEthereum);
            addTezosTokensForUser(tokenList, communityWalletTezos);
            foreach (Token token in tokenList.assets)
            {
                token.description += " - Owned by TokenCast Community";
            }
            return JsonConvert.SerializeObject(tokenList);
        }

        private void addEthereumTokensForUser(TokenList tokenList, string address)
        {
            addAlchemyTokensForUser(tokenList, address, Network.Ethereum);
        }
        private void addPolygonTokensForUser(TokenList tokenList, string address)
        {
            addAlchemyTokensForUser(tokenList, address, Network.Polygon);
        }

        private enum Network
        {
            Ethereum,
            Polygon
        }

        private void addAlchemyTokensForUser(TokenList tokenList, string address, Network network) 
        {
            string hostname = string.Empty;
            if (network == Network.Ethereum)
            {
                hostname = "https://eth-mainnet.alchemyapi.io";
            }
            else if (network == Network.Polygon)
            {
                hostname = "https://polygon-mainnet.g.alchemyapi.io";
            }
            else
            {
                throw new Exception(String.Concat("Unexpected network", network));
            }

            var alchemySecret = AppSettings.LoadAppSettings().AlchemySecret;
            AlchemyResponse nextSet = new AlchemyResponse();
            int currPage = 0;
            string pageKey = String.Empty;
            do
            {
                int offset = currPage++ * tokensPerPage;
                string alchemyGetTokensUri = $"{hostname}/v2/{alchemySecret}/getNFTs/?owner={address}";
                if (!string.IsNullOrEmpty(pageKey))
                {
                    alchemyGetTokensUri += $"&pageKey={pageKey}";
                }
                Uri alchemyGetTokens = new Uri(alchemyGetTokensUri);
                HttpClient client = new HttpClient();
                var response = client.GetAsync(alchemyGetTokens).Result;
                if (response.IsSuccessStatusCode)
                {
                    string jsonList = response.Content.ReadAsStringAsync().Result;
                    nextSet = JsonConvert.DeserializeObject<AlchemyResponse>(jsonList);
                    pageKey = nextSet.pageKey;
                    nextSet.ownedNfts.ForEach(nft =>
                    {
                        Uri alchemyGetMetadata = new Uri($"{hostname}/v2/{alchemySecret}/getNFTMetadata?contractAddress={nft.contract.address}&tokenId={nft.id.tokenId}&tokenType=erc721");
                        var tokenMetadataResponse = client.GetAsync(alchemyGetMetadata).Result;
                        if (tokenMetadataResponse.IsSuccessStatusCode)
                        {
                            string jsonMetadata = tokenMetadataResponse.Content.ReadAsStringAsync().Result;
                            var tokenMetadata = JsonConvert.DeserializeObject<AlchemyToken>(jsonMetadata);
                            if (tokenMetadata.media.Count > 0 &&
                                tokenMetadata.media.First().raw != null)
                            {
                                tokenList.assets.Add(new Token
                                {
                                    image_url = ipfsToHttp(tokenMetadata.media.First().raw),
                                    image_original_url = ipfsToHttp(tokenMetadata.media.First().raw),
                                    description = tokenMetadata.description,
                                    name = tokenMetadata.title,
                                    asset_contract = new AssetContract { address = nft.contract.address },
                                    permalink = Uri.EscapeUriString(tokenMetadata.metadata.external_url ?? string.Empty)
                                });
                            }
                        }
                        // Add retries in case of throttling
                    });
                }
            }
            while (nextSet.pageKey != null && nextSet.ownedNfts.Count > 0);

            // Add prices
            //if (lookupPrices)
            //{
            //    Dictionary<long, string> priceMap = getTokenPrices(address);
            //    // Consolidate into a single object
            //    foreach (Token token in tokenList.assets)
            //    {
            //        if (priceMap.ContainsKey(token.id))
            //        {
            //            token.current_price = priceMap[token.id];
            //        }
            //    }
            //}
        }

        private void addTezosTokensForUser(TokenList tokenList, string address)
        {
            Uri hicetnuncAPI = new Uri("https://hdapi.teztools.io/v1/graphql");
            var queryBody = "{\"query\":\"\\nquery collectorGallery($address: String!) {\\n  hic_et_nunc_token_holder(where: {holder_id: {_eq: $address}, token: {creator: {address: {_neq: $address}}}, quantity: {_gt: \\\"0\\\"}}, order_by: {token_id: desc}) {\\n    token {\\n      id\\n      artifact_uri\\n      display_uri\\n      thumbnail_uri\\n      timestamp\\n      mime\\n      title\\n      description\\n      supply\\n      royalties\\n      creator {\\n        address\\n        name\\n      }\\n    }\\n  }\\n}\\n\",\"variables\":{\"address\":\"" + address + "\"},\"operationName\":\"collectorGallery\"}";
            var requestBody = new StringContent(queryBody, Encoding.UTF8, "application/json");
            HttpClient client = new HttpClient();
            var response = client.PostAsync(hicetnuncAPI, requestBody).Result;
            if (response.IsSuccessStatusCode)
            {
                string jsonList = response.Content.ReadAsStringAsync().Result;
                TezosQueryResponse tezosTokens = JsonConvert.DeserializeObject<TezosQueryResponse>(jsonList);
                if (tezosTokens.data == null)
                {
                    return;
                }
                foreach(TezosToken tezosToken in tezosTokens.data.hic_et_nunc_token_holder)
                {
                    tezosToken.clean();
                    Token transformedToken = new Token();
                    // transformedToken.current_price = tezosToken.price.ToString();
                    transformedToken.id = tezosToken.token.id;
                    transformedToken.name = tezosToken.token.title;
                    transformedToken.description = tezosToken.token.description;
                    string imageUrl = ipfsToHttp(tezosToken.token.display_uri);
                    transformedToken.image_original_url = imageUrl;
                    transformedToken.image_url = imageUrl;
                    if (tezosToken.token.mime == "video/mp4")
                    {
                        // Add "mp4" to the end to make it easy to determine the content type
                        string url = string.Concat(ipfsToHttp(tezosToken.token.display_uri), "?type=mp4");
                        transformedToken.animation_url = url;
                        transformedToken.image_url = url;
                        transformedToken.image_original_url = url;
                    }
                    else
                    {
                        string url = ipfsToHttp(tezosToken.token.display_uri);
                        transformedToken.image_original_url = url;
                        transformedToken.image_url = url;
                    }
                    tokenList.assets.Add(transformedToken);
                }
            }
        }

        private string ipfsToHttp(string endpoint)
        {
            if (String.IsNullOrWhiteSpace(endpoint) || 
                endpoint.StartsWith("https://") || 
                endpoint.StartsWith("http://") ||
                endpoint.StartsWith("data:image/svg"))
            {
                return endpoint;
            }
            if (!endpoint.StartsWith("ipfs://"))
            {
                throw new Exception($"Expected {endpoint} to be an IPFS endpoint");
            }
            string id = endpoint.Substring("ipfs://".Length);
            id = id.Replace("ipfs/", string.Empty);
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
            while (nextSet.orders != null && nextSet.orders.Count > 0);

            Dictionary<long, string> priceMap = new Dictionary<long, string>();
            foreach (Order order in orderList.orders)
            {
                priceMap[order.asset.id] = order.current_price;
            }
            return priceMap;
        }

        public class AlchemyResponse
        {
            public List<AlchemyNft> ownedNfts { get; set; }
            public string pageKey { get; set; }
        }

        public class AlchemyNft
        {
            public AlchemyContract contract { get; set; }
            public AlchemyId id { get; set; }
        }

        public class AlchemyContract
        {
            public string address { get; set; }
        }

        public class AlchemyId
        {
            public string tokenId { get; set; }
        }

        public class AlchemyToken
        {
            public string title { get; set; }
            public string description { get; set; }
            public List<AlchemyMediaUri> media { get; set; }
            public AlchemyMetadata metadata { get; set; }
        }

        public class AlchemyMetadata
        {
            public string external_url { get; set; }
        }

        public class AlchemyMediaUri
        {
            public string raw { get; set; }
            public string gateway { get; set; }
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

            // Ensure properties are properly formatted
            public void clean()
            {
                if (!String.IsNullOrWhiteSpace(this.image_url))
                {
                    this.image_url = Uri.EscapeUriString(this.image_url);
                }
                if (!String.IsNullOrWhiteSpace(this.image_original_url))
                {
                    this.image_original_url = Uri.EscapeUriString(this.image_original_url);
                }
                if (!String.IsNullOrWhiteSpace(this.animation_url))
                {
                    this.animation_url = Uri.EscapeUriString(this.animation_url);
                }
                if (!String.IsNullOrWhiteSpace(this.external_link))
                {
                    this.external_link = Uri.EscapeUriString(this.external_link);
                }
                if (!String.IsNullOrWhiteSpace(this.permalink))
                {
                    this.permalink = Uri.EscapeUriString(this.permalink);
                }
            }
        }
        public class TezosQueryResponse
        {
            public TezosTokenList data { get; set; }
        }

        public class TezosTokenList
        {
            public List<TezosToken> hic_et_nunc_token_holder { get; set; }
        }


        public class TezosToken
        {
            public TokenInfo token { get; set; }

            // Ensure properties are properly formatted
            public void clean()
            {
                if (!String.IsNullOrWhiteSpace(this.token.artifact_uri))
                {
                    this.token.artifact_uri = Uri.EscapeUriString(this.token.artifact_uri);
                }
                if (!String.IsNullOrWhiteSpace(this.token.display_uri))
                {
                    this.token.display_uri = Uri.EscapeUriString(this.token.display_uri);
                }
                if (!String.IsNullOrWhiteSpace(this.token.thumbnail_uri))
                {
                    this.token.thumbnail_uri = Uri.EscapeUriString(this.token.thumbnail_uri);
                }
            }
        }

        public class TokenInfo
        {
            public long id { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public string artifact_uri { get; set; }
            public string display_uri { get; set; }
            public string thumbnail_uri { get; set; }
            public string mime { get; set; }
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

        private bool AuthCheck(string address, string signature, string network, string whitelabeler)
        {
            if (network.Equals("TEZOS", StringComparison.OrdinalIgnoreCase))
            {
                //replace with authentication
                return true;
            }
            if (signature == null || signature.Equals("null"))
            {
                return false;
            }
            if (string.IsNullOrEmpty(whitelabeler) || !ownershipProofMessages.ContainsKey(whitelabeler))
            {
                whitelabeler = "default";
            }
            string ownershipProofMessage = ownershipProofMessages[whitelabeler].hash;
            string rawMessage = ownershipProofMessages[whitelabeler].rawMessage;
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