using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;

namespace TokenCastWebApp.Processors
{
    /// <summary>
    /// Handles the queue of incoming items for processing, using Thread from ThreadPool.
    /// </summary>
    /// <typeparam name="T">The type of queue items.</typeparam>
    public sealed class QueueProcessor<T>
    {

        #region Private members

        private readonly Queue<T> _queue = new Queue<T>();

        private readonly Func<T, Task> _processQueueItem;
        private readonly ILogger _logger;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="processQueueItem">The callback function for processing incoming items.</param>
        /// <param name="logger">The instance of ILogger.</param>
        public QueueProcessor(Func<T, Task> processQueueItem, ILogger logger)
        {
            _processQueueItem = processQueueItem ?? throw new ArgumentNullException(nameof(processQueueItem));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles incoming items. Adds new incoming item to the queue and start processing.
        /// </summary>
        /// <param name="queueItem"></param>
        public void OnQueueItemReceived(T queueItem)
        {
            bool startThread;

            lock (_queue)
            {
                startThread = _queue.Count == 0;
                _queue.Enqueue(queueItem);
            }

            if (startThread)
                ThreadPool.QueueUserWorkItem(ProcessQueue);
        }

        /// <summary>
        /// Processes queue in the separated thread from the ThreadPool.
        /// </summary>
        /// <param name="state">Parameter of the WaitCallback.</param>
        private void ProcessQueue(object state)
        {
            while (true)
            {
                T queueItem;
                lock (_queue)
                {
                    if (_queue.Count == 0)
                        return;

                    queueItem = _queue.Peek();
                }

                try
                {
                    if (queueItem != null)
                    {
                        _processQueueItem(queueItem).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                lock (_queue)
                {
                    _queue.Dequeue();
                    if (_queue.Count == 0)
                        return;
                }
            }
        }
    }
}
