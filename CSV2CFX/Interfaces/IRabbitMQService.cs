using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Interfaces
{
    public interface IRabbitMQService
    {
        /// <summary>
        /// Publishes a message to the specified exchange with the given routing key.
        /// </summary>
        /// <remarks>This method is asynchronous and does not block the calling thread. Ensure that the
        /// exchange and routing key are properly configured in the messaging system to route the message as
        /// intended.</remarks>
        /// <param name="message">The message to be published. Cannot be null or empty.</param>
        /// <param name="routingKey">The routing key used to route the message. Cannot be null or empty.</param>
        /// <param name="exchange">The name of the exchange to publish the message to. If not specified, the default exchange is used.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishMessageAsync(string exchangeName, string routingKey, string message);

        /// <summary>
        /// Creates a new queue with the specified name asynchronously.
        /// </summary>
        /// <remarks>The queue name must adhere to the naming conventions required by the underlying queue
        /// system. If a queue with the specified name already exists, the behavior of this method depends on the
        /// implementation.</remarks>
        /// <param name="queueName">The name of the queue to create. Must not be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateQueueAsync(string queueName);

        /// <summary>
        /// Creates an exchange with the specified name and type asynchronously.
        /// </summary>
        /// <param name="exchangeName">The name of the exchange to create. Cannot be null or empty.</param>
        /// <param name="exchangeType">The type of the exchange to create. Defaults to <see cref="ExchangeType.Direct"/> if not specified.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CreateExchangeAsync(string exchangeName, string exchangeType = ExchangeType.Direct);

        /// <summary>
        /// Binds a queue to an exchange with the specified routing key.
        /// </summary>
        /// <remarks>This method establishes a binding between the specified queue and exchange, allowing
        /// messages published to the exchange with a matching routing key to be routed to the queue.</remarks>
        /// <param name="queueName">The name of the queue to bind. Cannot be null or empty.</param>
        /// <param name="exchangeName">The name of the exchange to bind the queue to. Cannot be null or empty.</param>
        /// <param name="routingKey">The routing key to use for the binding. Defaults to an empty string, which matches all messages.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task BindQueueAsync(string queueName, string exchangeName, string routingKey = "");
    }
}
