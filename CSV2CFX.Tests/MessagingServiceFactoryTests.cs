using CSV2CFX.AppSettings;
using CSV2CFX.Interfaces;
using CSV2CFX.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CSV2CFX.Tests
{
    public class MessagingServiceFactoryTests
    {
        [Fact]
        public void CreateMessagingService_WhenProtocolIsAMQP_ReturnsRabbitMQService()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(Mock.Of<ILogger<MessagingServiceFactory>>());
            serviceCollection.AddSingleton(Mock.Of<ILogger<RabbitMQService>>());
            serviceCollection.AddSingleton(Mock.Of<IRabbitMQConnectionFactory>());
            serviceCollection.AddSingleton<RabbitMQService>();
            
            var mockProtocolOptions = new Mock<IOptionsMonitor<ProtocolSetting>>();
            mockProtocolOptions.Setup(x => x.CurrentValue).Returns(new ProtocolSetting { Type = ProtocolType.AMQP });
            serviceCollection.AddSingleton(mockProtocolOptions.Object);
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var factory = new MessagingServiceFactory(serviceProvider, mockProtocolOptions.Object);

            // Act
            var service = factory.CreateMessagingService();

            // Assert
            Assert.IsType<RabbitMQService>(service);
        }

        [Fact]
        public void CreateMessagingService_WhenProtocolIsMQTT_ReturnsMqttService()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(Mock.Of<ILogger<MessagingServiceFactory>>());
            serviceCollection.AddSingleton(Mock.Of<ILogger<MqttService>>());
            
            var mockMqttOptions = new Mock<IOptionsMonitor<MqttSetting>>();
            mockMqttOptions.Setup(x => x.CurrentValue).Returns(new MqttSetting());
            serviceCollection.AddSingleton(mockMqttOptions.Object);
            serviceCollection.AddTransient<MqttService>();
            
            var mockProtocolOptions = new Mock<IOptionsMonitor<ProtocolSetting>>();
            mockProtocolOptions.Setup(x => x.CurrentValue).Returns(new ProtocolSetting { Type = ProtocolType.MQTT });
            serviceCollection.AddSingleton(mockProtocolOptions.Object);
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var factory = new MessagingServiceFactory(serviceProvider, mockProtocolOptions.Object);

            // Act
            var service = factory.CreateMessagingService();

            // Assert
            Assert.IsType<MqttService>(service);
        }

        [Fact]
        public void CreateMessagingService_WhenProtocolIsDefault_ReturnsRabbitMQService()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(Mock.Of<ILogger<MessagingServiceFactory>>());
            serviceCollection.AddSingleton(Mock.Of<ILogger<RabbitMQService>>());
            serviceCollection.AddSingleton(Mock.Of<IRabbitMQConnectionFactory>());
            serviceCollection.AddSingleton<RabbitMQService>();
            
            var mockProtocolOptions = new Mock<IOptionsMonitor<ProtocolSetting>>();
            mockProtocolOptions.Setup(x => x.CurrentValue).Returns(new ProtocolSetting()); // Default is AMQP
            serviceCollection.AddSingleton(mockProtocolOptions.Object);
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var factory = new MessagingServiceFactory(serviceProvider, mockProtocolOptions.Object);

            // Act
            var service = factory.CreateMessagingService();

            // Assert
            Assert.IsType<RabbitMQService>(service);
        }
    }
}
