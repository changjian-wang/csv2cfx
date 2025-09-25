using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Interfaces
{
    public interface IMessagingService : IDisposable
    {
        /// <summary>
        /// Creates a topic/queue for message routing
        /// </summary>
        /// <param name="name">The name of the topic/queue to create</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task CreateTopicAsync(string name);

        /// <summary>
        /// Creates an exchange/topic prefix for message routing
        /// </summary>
        /// <param name="exchangeName">The name of the exchange/topic prefix to create</param>
        /// <param name="exchangeType">The type of the exchange (for AMQP compatibility)</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task CreateExchangeAsync(string exchangeName, string exchangeType = "topic");

        /// <summary>
        /// Binds a queue to an exchange with the specified routing key
        /// </summary>
        /// <param name="queueName">The name of the queue/topic to bind</param>
        /// <param name="exchangeName">The name of the exchange/topic prefix to bind to</param>
        /// <param name="routingKey">The routing key to use for the binding</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task BindQueueAsync(string queueName, string exchangeName, string routingKey = "");

        /// <summary>
        /// Publishes a message to the specified exchange with the given routing key
        /// </summary>
        /// <param name="exchangeName">The name of the exchange/topic prefix</param>
        /// <param name="routingKey">The routing key used to route the message</param>
        /// <param name="message">The message content to be published</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        Task PublishMessageAsync(string exchangeName, string routingKey, string message);
    }
}