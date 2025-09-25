using System;
using System.Threading.Tasks;

namespace CSV2CFX.Services
{
    /// <summary>
    /// Interface for MQTT service functionalities.
    /// </summary>
    public interface IMqttService : IDisposable
    {
        /// <summary>
        /// Connects to the MQTT broker asynchronously.
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Publishes a message to the specified topic.
        /// </summary>
        /// <param name="topic">The topic to publish the message to.</param>
        /// <param name="message">The message content to publish.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishMessageAsync(string topic, string message);

        /// <summary>
        /// Subscribes to a specified topic and handles incoming messages.
        /// </summary>
        /// <param name="topic">The topic to subscribe to.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SubscribeAsync(string topic);

        /// <summary>
        /// Disconnects from the MQTT broker asynchronously.
        /// </summary>
        Task DisconnectAsync();
    }
}