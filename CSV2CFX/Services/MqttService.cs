using CSV2CFX.AppSettings;
using CSV2CFX.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Services
{
    public class MqttService : IMessagingService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IOptionsMonitor<MqttSetting> _mqttOptions;
        private IMqttClient? _mqttClient;
        private readonly HashSet<string> _createdTopics = new();
        private bool _disposed = false;

        public MqttService(ILogger<MqttService> logger, IOptionsMonitor<MqttSetting> mqttOptions)
        {
            _logger = logger;
            _mqttOptions = mqttOptions;
        }

        private async Task<IMqttClient> GetConnectedClientAsync()
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                var mqttSetting = _mqttOptions.CurrentValue;
                var factory = new MqttClientFactory();
                _mqttClient = factory.CreateMqttClient();

                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttSetting.BrokerHost, mqttSetting.BrokerPort);

                if (!string.IsNullOrEmpty(mqttSetting.Username))
                {
                    optionsBuilder.WithCredentials(mqttSetting.Username, mqttSetting.Password);
                }

                if (!string.IsNullOrEmpty(mqttSetting.ClientId))
                {
                    optionsBuilder.WithClientId(mqttSetting.ClientId);
                }
                else
                {
                    optionsBuilder.WithClientId($"CSV2CFX_{Environment.MachineName}_{Guid.NewGuid():N}");
                }

                var options = optionsBuilder
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(mqttSetting.KeepAlivePeriod))
                    .WithCleanSession(mqttSetting.CleanSession)
                    .WithTimeout(TimeSpan.FromSeconds(mqttSetting.ConnectionTimeout))
                    .Build();

                try
                {
                    await _mqttClient.ConnectAsync(options);
                    _logger.LogInformation("Connected to MQTT broker at {BrokerHost}:{BrokerPort}", 
                        mqttSetting.BrokerHost, mqttSetting.BrokerPort);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to MQTT broker at {BrokerHost}:{BrokerPort}", 
                        mqttSetting.BrokerHost, mqttSetting.BrokerPort);
                    throw;
                }
            }

            return _mqttClient;
        }

        /// <summary>
        /// Creates a topic for MQTT (topics are created implicitly in MQTT when first published to)
        /// </summary>
        public async Task CreateTopicAsync(string name)
        {
            // In MQTT, topics are created implicitly when first published to
            // We just track that we've "created" this topic for logging purposes
            _createdTopics.Add(name);
            _logger.LogInformation("Topic '{TopicName}' marked as created (MQTT topics are created implicitly)", name);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates an exchange (in MQTT, this is handled as a topic prefix)
        /// </summary>
        public async Task CreateExchangeAsync(string exchangeName, string exchangeType = "topic")
        {
            // In MQTT, we don't have exchanges, but we track the exchange name as a topic prefix
            _createdTopics.Add(exchangeName);
            _logger.LogInformation("Exchange '{ExchangeName}' of type '{ExchangeType}' marked as created (MQTT uses topic prefixes)", 
                exchangeName, exchangeType);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Binds a queue to an exchange (in MQTT, this is a no-op as topics are subscribed to directly)
        /// </summary>
        public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey = "")
        {
            // In MQTT, binding is handled through subscriptions, not explicit bindings
            // For compatibility with AMQP interface, we just log this operation
            _logger.LogInformation("Queue '{QueueName}' bound to exchange '{ExchangeName}' with routing key '{RoutingKey}' (MQTT binding is handled through subscriptions)", 
                queueName, exchangeName, routingKey);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Publishes a message to the specified topic
        /// </summary>
        public async Task PublishMessageAsync(string exchangeName, string routingKey, string message)
        {
            var client = await GetConnectedClientAsync();
            var mqttSetting = _mqttOptions.CurrentValue;
            
            // Build the topic name: prefix/exchange/routingKey
            var topicBuilder = new StringBuilder();
            
            if (!string.IsNullOrEmpty(mqttSetting.TopicPrefix))
            {
                topicBuilder.Append(mqttSetting.TopicPrefix).Append("/");
            }
            
            if (!string.IsNullOrEmpty(exchangeName))
            {
                topicBuilder.Append(exchangeName).Append("/");
            }
            
            if (!string.IsNullOrEmpty(routingKey))
            {
                topicBuilder.Append(routingKey);
            }

            var topic = topicBuilder.ToString().TrimEnd('/');
            
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            try
            {
                await client.PublishAsync(mqttMessage);
                _logger.LogInformation("Message published to MQTT topic '{Topic}'", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to MQTT topic '{Topic}'", topic);
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _mqttClient?.DisconnectAsync().Wait(5000);
                    _mqttClient?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error occurred while disposing MQTT client");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}