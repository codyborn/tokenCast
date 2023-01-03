
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TokenCast.Controllers;
using TokenCast.Models;
using TokenCastWebApp.Managers.Interfaces;

namespace TokenCast
{
    public interface IDatabase
    {
        Task CreateOrUpdateAccount(AccountModel account);

        Task<AccountModel> GetAccount(string address);

        Task AddDevice(string address, string deviceId, string deviceAlias);

        Task AddCanviaDevicesToAccount(string address);

        Task AddDeviceAlias(string address, string deviceId, string alias);

        Task UpdateDeviceFrequency(string deviceId, int frequency);

        Task DeleteDevice(string address, string deviceId);

        Task ReorderCastedTokensOnDevice(DeviceModel device, IEnumerable<int> order);

        Task SetDeviceContent(DeviceModel device);

        Task AddDeviceContent(DeviceModel device);

        Task SetDeviceWhiteLabeler(string deviceId, string whitelabel);

        Task RemoveDeviceContent(string deviceId);

        Task RemoveATokenFromDevice(string deviceId, int index);

        Task<DeviceModel> GetDeviceContent(string deviceId);

        Task<List<DeviceModel>> GetAllDevices();

        Task<LastUpdateModel> GetLastUpdateTime();
    }

    public class Database : IDatabase
    {
        private readonly IWebSocketConnectionManager _webSocketConnectionManager;

        public Database(IWebSocketConnectionManager webSocketConnectionManager)
        {
            _webSocketConnectionManager = webSocketConnectionManager;
        }


        private const string accountTableName = "accounts";
        private const string deviceTableName = "devices";
        private const string systemTableName = "system";
        private const string dateTimeKey = "datetime_key";

        private static Lazy<CloudStorageAccount> storageAccount = new Lazy<CloudStorageAccount>(() =>
        {
            string storageConnectionString = AppSettings.LoadAppSettings().StorageConnectionString;
            try
            {
                return CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the application.");
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.ReadLine();
                throw;
            }
        });

        private static Lazy<CloudTableClient> tableClient = new Lazy<CloudTableClient>(() =>
        {
            // Create a table client for interacting with the table service
            return storageAccount.Value.CreateCloudTableClient();
        });

        private Lazy<CloudTable> accountTable = new Lazy<CloudTable>(() =>
        {
            CloudTable table = tableClient.Value.GetTableReference(accountTableName);
            table.CreateIfNotExistsAsync().Wait();
            return table;
        });

        private Lazy<CloudTable> deviceTable = new Lazy<CloudTable>(() =>
        {
            CloudTable table = tableClient.Value.GetTableReference(deviceTableName);
            table.CreateIfNotExistsAsync().Wait();
            return table;
        });

        private Lazy<CloudTable> systemTable = new Lazy<CloudTable>(() =>
        {
            CloudTable table = tableClient.Value.GetTableReference(systemTableName);
            table.CreateIfNotExistsAsync().Wait();
            return table;
        });

        public async Task CreateOrUpdateAccount(AccountModel account)
        {
            var databaseEntity = new DatabaseEntity<AccountModel>(account.address, account.address, account);
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(databaseEntity);
            TableResult result = await accountTable.Value.ExecuteAsync(insertOrMergeOperation);
            if (result.HttpStatusCode / 100 != 2)
            {
                throw new StorageException($"Unable to add account {account.address}");
            }
        }

        public async Task<AccountModel> GetAccount(string address)
        {
            address = address.ToLowerInvariant();
            TableOperation retrieveOperation = TableOperation.Retrieve<DatabaseEntity<AccountModel>>(address, address);
            TableResult result = await accountTable.Value.ExecuteAsync(retrieveOperation);
            var databaseEntity = result.Result as DatabaseEntity<AccountModel>;

            if (databaseEntity == null)
            {
                return null;
            }

            return databaseEntity.getEntity();
        }

        public async Task AddDevice(string address, string deviceId, string deviceAlias)
        {
            address = address.ToLowerInvariant();
            AccountModel account = await GetAccount(address);
            if (account == null)
            {
                throw new StorageException($"Unable to locate account {address}");
            }

            if (account.devices == null)
            {
                account.devices = new List<string>();
            }

            if (!account.devices.Contains(deviceId))
            {
                account.devices.Add(deviceId);
                await CreateOrUpdateAccount(account);
                await UpdateDevice(new DeviceModel
                {
                    id = deviceId,
                    frequencyOfRotation = 5
                }); ;
                await AddDeviceAlias(address, deviceId, deviceAlias);
            }
        }

        public async Task AddCanviaDevicesToAccount(string address)
        {
            address = address.ToLowerInvariant();
            AccountModel account = await GetAccount(address);
            if (account == null)
            {
                throw new StorageException($"Unable to locate account {address}");
            }

            if (account.devices == null)
            {
                account.devices = new List<string>();

            }

            foreach (var (name, identifier) in account.canviaAccount.canviaDevices)
            {
                var deviceModel = new DeviceModel {id = identifier, isCanviaDevice = true};
                await AddDevice(address, identifier, "canvia");
                await UpdateDevice(deviceModel);
                await AddDeviceAlias(address, identifier, name);
            }
        }

        public async Task AddDeviceAlias(string address, string deviceId, string alias)
        {
            address = address.ToLowerInvariant();
            AccountModel account = await GetAccount(address);
            if (account == null)
            {
                throw new StorageException($"Unable to locate account {address}");
            }

            if (account.devices == null || !account.devices.Contains(deviceId))
            {
                throw new StorageException($"Unable to locate device {deviceId}");
            }

            if (account.deviceMapping == null)
            {
                account.deviceMapping = new Dictionary<string, string>();
            }

            account.deviceMapping[deviceId] = alias;
            await CreateOrUpdateAccount(account);
        }        
        
        public async Task UpdateDeviceFrequency(string deviceId, int frequency)
        {
            DeviceModel device = await GetDeviceContent(deviceId);
            
            if (device == null)
            {
                throw new StorageException($"Unable to locate device {deviceId}");
            }
            
            device.frequencyOfRotation = frequency;

            await UpdateDevice(device);
        }

        public async Task DeleteDevice(string address, string deviceId)
        {
            address = address.ToLowerInvariant();
            AccountModel account = await GetAccount(address);
            if (account == null)
            {
                throw new StorageException($"Unable to locate account {address}");
            }

            if (account.devices == null)
            {
                account.devices = new List<string>();
            }

            if (account.devices.Contains(deviceId))
            {
                account.devices.Remove(deviceId);
                await CreateOrUpdateAccount(account);
            }
        }

        public Task ReorderCastedTokensOnDevice(DeviceModel device, IEnumerable<int> order)
        {
            var reorderedTokens = order.Select(t => device.castedTokens[t]).ToList();
            device.castedTokens = reorderedTokens;
            return UpdateDevice(device);
        }


        private async Task UpdateDevice(DeviceModel device)
        {
            var databaseEntity = new DatabaseEntity<DeviceModel>(device.id, device.id, device);
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(databaseEntity);
            TableResult result = await deviceTable.Value.ExecuteAsync(insertOrMergeOperation);
            if (result.HttpStatusCode / 100 != 2)
            {
                throw new StorageException($"Unable to add content for device {device.id}");
            }

            _webSocketConnectionManager.SendMessage(device.id, new TokenCastWebApp.Models.ClientMessageResponse
            {
                Event = TokenCastWebApp.Models.EventType.NFTUpdated,
                Message = "Event raised!",
                Success = true
            });
        }

        public async Task SetDeviceContent(DeviceModel device)
        { 
            var prevDevice = await GetDeviceContent(device.id);
            if (prevDevice != null && !string.IsNullOrEmpty(prevDevice.whiteLabeler))
            {
                device.whiteLabeler = prevDevice.whiteLabeler;
            }

            await UpdateDevice(device);
        }

        public async Task AddDeviceContent(DeviceModel device)
        {
            var prevDevice = await GetDeviceContent(device.id);
            if (prevDevice == null)
            {
                prevDevice = new DeviceModel(device.id, device.currentDisplay);
            }
            if (prevDevice.castedTokens == null)
            {
                prevDevice.castedTokens = new List<Display>();
            }
            prevDevice.castedTokens.Add(device.currentDisplay);
            prevDevice.currentDisplay = device.currentDisplay;
            await UpdateDevice(prevDevice);
        }

        public async Task SetDeviceWhiteLabeler(string deviceId, string whitelabel)
        {
            var device = await GetDeviceContent(deviceId);
            if (device != null && !string.IsNullOrEmpty(device.whiteLabeler))
            {
                return;
            }
            if (device == null)
            {
                device = new DeviceModel();
            }

            device.id = deviceId;
            device.whiteLabeler = whitelabel;
            await UpdateDevice(device);
        }

        public async Task RemoveDeviceContent(string deviceId)
        {
            var prevDevice = await GetDeviceContent(deviceId);
            if (prevDevice == null)
            {
                return;
            }
            prevDevice.castedTokens = new List<Display>();
            prevDevice.currentDisplay = null;
            await UpdateDevice(prevDevice);
        }        
        
        public async Task RemoveATokenFromDevice(string deviceId, int index)
        {
            var prevDevice = await GetDeviceContent(deviceId);
            if (prevDevice == null)
            {
                return;
            }

            if (index == 0 && prevDevice.castedTokens.Count > 1)
            {
                prevDevice.currentDisplay = prevDevice.castedTokens[1];
            }
            
            prevDevice.castedTokens.RemoveAt(index);
            
            await UpdateDevice(prevDevice);
        }

        public async Task<DeviceModel> GetDeviceContent(string deviceId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<DatabaseEntity<DeviceModel>>(deviceId, deviceId);
            TableResult result = await deviceTable.Value.ExecuteAsync(retrieveOperation);
            var databaseEntity = result.Result as DatabaseEntity<DeviceModel>;

            if (databaseEntity == null)
            {
                return null;
            }

            return databaseEntity.getEntity();
        }

        public async Task<List<DeviceModel>> GetAllDevices()
        {
            TableContinuationToken token = null;
            var entities = new List<DeviceModel>();
            do
            {
                var queryResult = await deviceTable.Value.ExecuteQuerySegmentedAsync<DatabaseEntity<DeviceModel>>(new TableQuery<DatabaseEntity<DeviceModel>>(), token);
                entities.AddRange(queryResult.Results.Select(e => e.getEntity()));
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entities;
        }

        public async Task<LastUpdateModel> GetLastUpdateTime()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<DatabaseEntity<LastUpdateModel>>(dateTimeKey, dateTimeKey);
            TableResult result = await systemTable.Value.ExecuteAsync(retrieveOperation);
            var databaseEntity = result.Result as DatabaseEntity<LastUpdateModel>;

            if (databaseEntity == null)
            {
                return null;
            }

            return databaseEntity.getEntity();
        }
    }
}