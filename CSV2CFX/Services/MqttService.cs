using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;

namespace CSV2CFX.Services
{
    public class MqttService : IMqttService, IDisposable
    {
        private readonly ILogger<MqttService> _logger;
        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttOptions;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttService"/> class with the specified logger.
        /// </summary>
        /// <param name="logger">The logger instance used for logging messages and errors.</param>
        public MqttService(ILogger<MqttService> logger)
        {
            _logger = logger;

            // Configure MQTT client options
            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("MqttServiceClient")
                .WithTcpServer("localhost", 1883) // Update with your MQTT broker address and port
                .WithCleanSession()
                .Build();

            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        }

        /// <summary>
        /// Handles the Connected event.
        /// </summary>
        private Task OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            _logger.LogInformation("Connected to MQTT broker.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the Disconnected event.
        /// </summary>
        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("Disconnected from MQTT broker.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles incoming messages.
        /// </summary>
        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
            _logger.LogInformation("Message received on topic '{Topic}': {Payload}", args.ApplicationMessage.Topic, payload);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Connects to the MQTT broker asynchronously.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (!_mqttClient.IsConnected)
            {
                await _mqttClient.ConnectAsync(_mqttOptions, CancellationToken.None);
                _logger.LogInformation("Successfully connected to MQTT broker.");
            }
        }

        /// <summary>
        /// Publishes a message to the specified topic.
        /// </summary>
        /// <param name="topic">The topic to publish the message to.</param>
        /// <param name="message">The message content to publish.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task PublishMessageAsync(string topic, string message)
        {
            if (!_mqttClient.IsConnected)
            {
                await ConnectAsync();
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(message))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag()
                .Build();

            var result = await _mqttClient.PublishAsync(mqttMessage, CancellationToken.None);
            if (result.ReasonCode == MqttClientPublishReasonCode.Success)
            {
                _logger.LogInformation("Message published to topic '{Topic}'.", topic);
            }
            else
            {
                _logger.LogWarning("Failed to publish message to topic '{Topic}'. Reason: {ReasonCode}", topic, result.ReasonCode);
            }
        }

        /// <summary>
        /// Subscribes to a specified topic and handles incoming messages.
        /// </summary>
        /// <param name="topic">The topic to subscribe to.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SubscribeAsync(string topic)
        {
            if (!_mqttClient.IsConnected)
            {
                await ConnectAsync();
            }

            var result = await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build(), CancellationToken.None);
            if (result.Items.First().ResultCode == MqttClientSubscribeResultCode.GrantedQoS1)
            {
                _logger.LogInformation("Subscribed to topic '{Topic}'.", topic);
            }
            else
            {
                _logger.LogWarning("Failed to subscribe to topic '{Topic}'. Reason: {ReasonCode}", topic, result.Items.First().ResultCode);
            }
        }

        /// <summary>
        /// Disconnects from the MQTT broker asynchronously.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                _logger.LogInformation("Disconnected from MQTT broker.");
            }
        }

        /// <summary>
        /// Releases resources used by the MqttService.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                DisconnectAsync().GetAwaiter().GetResult();
                _mqttClient?.Dispose();
                _disposed = true;
            }
        }
    }
}