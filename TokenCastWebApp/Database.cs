using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using TokenCast.Controllers;
using TokenCast.Models;

namespace TokenCast
{
    public static class Database
    {
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
        private static Lazy<CloudTable> accountTable = new Lazy<CloudTable>(() =>
        {
            CloudTable table = tableClient.Value.GetTableReference(accountTableName);
            table.CreateIfNotExistsAsync().Wait();
            return table;
        });
        private static Lazy<CloudTable> deviceTable = new Lazy<CloudTable>(() =>
        {
            CloudTable table = tableClient.Value.GetTableReference(deviceTableName);
            table.CreateIfNotExistsAsync().Wait();
            return table;
        });
        private static Lazy<CloudTable> systemTable = new Lazy<CloudTable>(() =>
        {
            CloudTable table = tableClient.Value.GetTableReference(systemTableName);
            table.CreateIfNotExistsAsync().Wait();
            return table;
        });

        public static async Task CreateOrUpdateAccount(AccountModel account)
        {
            var databaseEntity = new DatabaseEntity<AccountModel>(account.address, account.address, account);
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(databaseEntity);
            TableResult result = await accountTable.Value.ExecuteAsync(insertOrMergeOperation);
            if (result.HttpStatusCode / 100 != 2)
            {
                throw new StorageException($"Unable to add account {account.address}");
            }
        }

        public static async Task<AccountModel> GetAccount(string address)
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

        public static async Task AddDevice(string address, string deviceId)
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
                await UpdateDeviceFrequency(deviceId, 5);
            }
        }

        public static async Task AddCanviaDevicesToAccount(string address)
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
                AddDevice(address, identifier).Wait();
                UpdateDevice(deviceModel).Wait();
                AddDeviceAlias(address, identifier, name).Wait();
            }
        }

        public static async Task AddDeviceAlias(string address, string deviceId, string alias)
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
        
        public static async Task UpdateDeviceFrequency(string deviceId, int frequency)
        {
            DeviceModel device = Database.GetDeviceContent(deviceId).Result;
            
            if (device == null)
            {
                throw new StorageException($"Unable to locate device {deviceId}");
            }
            
            device.frequencyOfRotation = frequency;

            await UpdateDevice(device);
        }

        public static async Task DeleteDevice(string address, string deviceId)
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

        public static Task ReorderCastedTokensOnDevice(DeviceModel device, IEnumerable<int> order)
        {
            var reorderedTokens = order.Select(t => device.castedTokens[t]).ToList();
            device.castedTokens = reorderedTokens;
            return UpdateDevice(device);
        }


        private static async Task UpdateDevice(DeviceModel device)
        {
            var databaseEntity = new DatabaseEntity<DeviceModel>(device.id, device.id, device);
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(databaseEntity);
            TableResult result = await deviceTable.Value.ExecuteAsync(insertOrMergeOperation);
            if (result.HttpStatusCode / 100 != 2)
            {
                throw new StorageException($"Unable to add content for device {device.id}");
            }
        }

        public static Task SetDeviceContent(DeviceModel device)
        {
            var prevDevice = Database.GetDeviceContent(device.id).Result;
            if (prevDevice != null && !string.IsNullOrEmpty(prevDevice.whiteLabeler))
            {
                device.whiteLabeler = prevDevice.whiteLabeler;
            }

            return UpdateDevice(device);
        }

        public static Task AddDeviceContent(DeviceModel device)
        {
            var prevDevice = Database.GetDeviceContent(device.id).Result;
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
            return UpdateDevice(prevDevice);
        }

        public static Task SetDeviceWhiteLabeler(string deviceId, string whitelabel)
        {
            var device = Database.GetDeviceContent(deviceId).Result;
            if (device != null && !string.IsNullOrEmpty(device.whiteLabeler))
            {
                return Task.FromResult(0);
            }
            if (device == null)
            {
                device = new DeviceModel();
            }

            device.id = deviceId;
            device.whiteLabeler = whitelabel;
            return UpdateDevice(device);
        }

        public static Task RemoveDeviceContent(string deviceId)
        {
            var prevDevice = Database.GetDeviceContent(deviceId).Result;
            if (prevDevice == null)
            {
                return Task.FromResult(0);
            }
            prevDevice.castedTokens = new List<Display>();
            prevDevice.currentDisplay = null;
            return UpdateDevice(prevDevice);
        }        
        
        public static Task RemoveATokenFromDevice(string deviceId, int index)
        {
            var prevDevice = Database.GetDeviceContent(deviceId).Result;
            if (prevDevice == null)
            {
                return Task.FromResult(0);
            }

            if (index == 0 && prevDevice.castedTokens.Count > 1)
            {
                prevDevice.currentDisplay = prevDevice.castedTokens[1];
            }
            
            prevDevice.castedTokens.RemoveAt(index);
            
            return UpdateDevice(prevDevice);
        }

        public static async Task<DeviceModel> GetDeviceContent(string deviceId)
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

        public static async Task<List<DeviceModel>> GetAllDevices()
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

        public static async Task<LastUpdateModel> GetLastUpdateTime()
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