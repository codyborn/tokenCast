using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using System;

namespace TokenCastWebApp.Models
{
    public sealed class ClientMessageRequest
    {
        /// <summary>
        /// Subscribe/Unsubscribe action.
        /// </summary>
        public SubscribeAction Action { get; set; }

        /// <summary>
        /// Optional parameter for specify identifier.
        /// </summary>
        public long? Id { get; set; }
   
    }
}
