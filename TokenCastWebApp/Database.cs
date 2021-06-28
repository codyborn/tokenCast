using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
                throw new StorageException($"Unable to locate account {account.address}");
            }

            if (account.devices == null)
            {
                account.devices = new List<string>();
            }

            if (!account.devices.Contains(deviceId))
            {
                account.devices.Add(deviceId);
                await CreateOrUpdateAccount(account);
            }
        }

        public static async Task AddDeviceAlias(string address, string deviceId, string alias)
        {
            address = address.ToLowerInvariant();
            AccountModel account = await GetAccount(address);
            if (account == null)
            {
                throw new StorageException($"Unable to locate account {account.address}");
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

        public static async Task DeleteDevice(string address, string deviceId)
        {
            address = address.ToLowerInvariant();
            AccountModel account = await GetAccount(address);
            if (account == null)
            {
                throw new StorageException($"Unable to locate account {account.address}");
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

        public static async Task SetDeviceContent(DeviceModel device)
        {
            var databaseEntity = new DatabaseEntity<DeviceModel>(device.id, device.id, device);
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(databaseEntity);
            TableResult result = await deviceTable.Value.ExecuteAsync(insertOrMergeOperation);
            if (result.HttpStatusCode / 100 != 2)
            {
                throw new StorageException($"Unable to add content for device {device.id}");
            }
        }

        public static async Task RemoveDeviceContent(string deviceId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<DatabaseEntity<DeviceModel>>(deviceId, deviceId);
            TableResult result = await deviceTable.Value.ExecuteAsync(retrieveOperation);
            var databaseEntity = result.Result as DatabaseEntity<DeviceModel>;

            if (databaseEntity == null)
            {
                return;
            }

            databaseEntity.ETag = "*";
            TableOperation deleteOperation = TableOperation.Delete(databaseEntity);
            result = await deviceTable.Value.ExecuteAsync(deleteOperation);
            if (result.HttpStatusCode / 100 != 2)
            {
                throw new StorageException($"Unable to remove content for device {deviceId}");
            }
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
