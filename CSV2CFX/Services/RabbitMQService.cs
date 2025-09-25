using CSV2CFX.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Services
{
    public class RabbitMQService : IRabbitMQService, IMessagingService, IDisposable
    {
        private readonly IRabbitMQConnectionFactory _connectionFactory;
        private bool _disposed = false;
        private readonly ILogger<RabbitMQService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQService"/> class with the specified RabbitMQ connection
        /// and logger.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection to be used for interacting with the message broker. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="logger">The logger instance used for logging messages and errors. Cannot be <see langword="null"/>.</param>
        public RabbitMQService(IRabbitMQConnectionFactory connectionFactory, ILogger<RabbitMQService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// Creates a topic/queue for message routing (IMessagingService implementation)
        /// </summary>
        /// <param name="name">The name of the topic/queue to create</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task CreateTopicAsync(string name)
        {
            await CreateQueueAsync(name);
        }

        /// <summary>
        /// Creates a new message queue with the specified name.
        /// </summary>
        /// <remarks>The created queue is durable, non-exclusive, and will not be automatically deleted. 
        /// This method logs a message upon successful creation of the queue.</remarks>
        /// <param name="queueName">The name of the queue to create. Must not be <see langword="null"/> or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateQueueAsync(string queueName)
        {
            var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(
                                    queue: queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            _logger.LogInformation("Queue '{QueueName}' created successfully.", queueName);
        }

        /// <summary>
        /// Creates an exchange on the message broker with the specified name and type.
        /// </summary>
        /// <remarks>The created exchange is durable and will persist across broker restarts. It is not
        /// automatically deleted when no longer in use. Ensure that the <paramref name="exchangeName"/> is unique
        /// within the broker to avoid conflicts with existing exchanges.</remarks>
        /// <param name="exchangeName">The name of the exchange to create. This value cannot be null or empty.</param>
        /// <param name="exchangeType">The type of the exchange to create. Defaults to <see cref="ExchangeType.Direct"/> if not specified. Common
        /// values include <see cref="ExchangeType.Direct"/>, <see cref="ExchangeType.Fanout"/>, <see
        /// cref="ExchangeType.Topic"/>, and <see cref="ExchangeType.Headers"/>.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateExchangeAsync(string exchangeName, string exchangeType = ExchangeType.Topic)
        {
            var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(
                                    exchange: exchangeName,
                                    type: exchangeType,
                                    durable: true,
                                    autoDelete: false,
                                    arguments: null);
            _logger.LogInformation("Exchange '{ExchangeName}' of type '{ExchangeType}' created successfully.", exchangeName, exchangeType);
        }

        /// <summary>
        /// Binds a queue to an exchange with the specified routing key.
        /// </summary>
        /// <remarks>This method establishes a binding between the specified queue and exchange, allowing
        /// messages published to the exchange with a matching routing key to be routed to the queue.</remarks>
        /// <param name="queueName">The name of the queue to bind. Cannot be null or empty.</param>
        /// <param name="exchangeName">The name of the exchange to bind the queue to. Cannot be null or empty.</param>
        /// <param name="routingKey">The routing key to use for the binding. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey)
        {
            var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            await channel.QueueBindAsync(
                                    queue: queueName,
                                    exchange: exchangeName,
                                    routingKey: routingKey,
                                    arguments: null);
            _logger.LogInformation("Queue '{QueueName}' bound to exchange '{ExchangeName}' with routing key '{RoutingKey}'.", queueName, exchangeName, routingKey);
        }

        /// <summary>
        /// Publishes a message to the specified exchange with the given routing key.
        /// </summary>
        /// <remarks>This method ensures that the message is published with persistent delivery mode,
        /// which guarantees that the message will be saved to disk by the broker if necessary. The operation logs an
        /// informational message upon successful publication.</remarks>
        /// <param name="exchangeName">The name of the exchange to which the message will be published. Cannot be null or empty.</param>
        /// <param name="routingKey">The routing key used to route the message. Cannot be null or empty.</param>
        /// <param name="message">The message content to be published. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task PublishMessageAsync(string exchangeName, string routingKey, string message)
        {
            var connection = await _connectionFactory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            var properties = new BasicProperties
            {
                Persistent = true
            };
            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Message published to exchange '{ExchangeName}' with routing key '{RoutingKey}'.", exchangeName, routingKey);
        }

        /// <summary>
        /// Asynchronously releases the resources used by the current instance.
        /// </summary>
        /// <remarks>This method closes the underlying connection, if it is open, and disposes of it.  It
        /// should be called when the instance is no longer needed to ensure proper cleanup of resources.</remarks>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        public void Dispose()
        {
            if (!_disposed)
            {
                // 如果 IRabbitMQConnectionFactory 实现了 IDisposable，可以在这里处理
                // 但通常连接工厂的生命周期由容器管理
                _disposed = true;
            }
        }
    }
}
