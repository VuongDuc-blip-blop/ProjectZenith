using Azure.Storage.Queues.Models;

namespace ProjectZenith.Contracts.Infrastructure.MessageQueue
{

    public class QueueMessageWrapper
    {
        public string MessageId { get; }
        public string PopReceipt { get; }
        public string MessageText { get; }
        internal object OriginalMessage { get; } // Keep original message for advanced scenarios

        public QueueMessageWrapper(QueueMessage originalMessage)
        {
            MessageId = originalMessage.MessageId;
            PopReceipt = originalMessage.PopReceipt;
            MessageText = originalMessage.Body.ToString();
            OriginalMessage = originalMessage;
        }
    }
}

