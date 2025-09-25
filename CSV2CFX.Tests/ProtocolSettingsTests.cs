using CSV2CFX.AppSettings;
using Xunit;

namespace CSV2CFX.Tests
{
    public class ProtocolSettingsTests
    {
        [Fact]
        public void ProtocolSetting_DefaultType_IsAMQP()
        {
            // Arrange & Act
            var setting = new ProtocolSetting();

            // Assert
            Assert.Equal(ProtocolType.AMQP, setting.Type);
        }

        [Theory]
        [InlineData(ProtocolType.AMQP)]
        [InlineData(ProtocolType.MQTT)]
        public void ProtocolSetting_CanSetType(ProtocolType protocolType)
        {
            // Arrange
            var setting = new ProtocolSetting();

            // Act
            setting.Type = protocolType;

            // Assert
            Assert.Equal(protocolType, setting.Type);
        }

        [Fact]
        public void MqttSetting_HasCorrectDefaults()
        {
            // Arrange & Act
            var setting = new MqttSetting();

            // Assert
            Assert.Equal("localhost", setting.BrokerHost);
            Assert.Equal(1883, setting.BrokerPort);
            Assert.Equal(60, setting.KeepAlivePeriod);
            Assert.True(setting.CleanSession);
            Assert.Equal(30, setting.ConnectionTimeout);
            Assert.Equal("cfx", setting.TopicPrefix);
            Assert.False(setting.UseTls);
        }
    }
}
