using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TokenCast.Models
{
    public class DatabaseEntity<T> : TableEntity
    {
        public string entity { get; set; }
        public DatabaseEntity(string rowKey, string partitionKey, T entity)
        {
            this.RowKey = rowKey;
            this.PartitionKey = partitionKey;
            this.entity = JsonConvert.SerializeObject(entity);
        }

        public DatabaseEntity()
        {
        }

        public T getEntity()
        {
            return JsonConvert.DeserializeObject<T>(entity);
        }
    }
}
