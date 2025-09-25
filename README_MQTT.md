# MQTT Protocol Support in CSV2CFX

This document describes the MQTT protocol implementation added to CSV2CFX alongside the existing AMQP/RabbitMQ support.

## Overview

CSV2CFX now supports both AMQP (RabbitMQ) and MQTT protocols for message publishing. Users can select their preferred protocol through the configuration UI, and the system will automatically use the appropriate messaging service.

## Architecture

### Protocol Abstraction
- `IMessagingService`: Common interface for both protocols
- `IMessagingServiceFactory`: Factory pattern for protocol selection
- `MessagingServiceFactory`: Concrete factory implementation

### MQTT Implementation
- `MqttService`: MQTT protocol implementation using MQTTnet library
- `MqttSetting`: Configuration class for MQTT broker settings
- `ProtocolType`: Enum for protocol selection (AMQP = 0, MQTT = 1)

## Configuration

### Protocol Selection
```json
{
  "Protocol": {
    "Type": 1  // 0 = AMQP, 1 = MQTT
  }
}
```

### MQTT Settings
```json
{
  "MQTT": {
    "BrokerHost": "localhost",
    "BrokerPort": 1883,
    "Username": "",
    "Password": "",
    "ClientId": "",
    "UseTls": false,
    "KeepAlivePeriod": 60,
    "CleanSession": true,
    "ConnectionTimeout": 30,
    "TopicPrefix": "cfx"
  }
}
```

## Topic Structure

MQTT topics follow this structure:
```
{TopicPrefix}/{ExchangeName}/{RoutingKey}
```

Examples:
- `cfx/FLEX-SMT.exchange/FLEX-SMT.heartbeat.routing-key`
- `cfx/FLEX-SMT.exchange/FLEX-SMT.workstarted.routing-key`
- `cfx/FLEX-SMT.exchange/FLEX-SMT.unitsprocessed.routing-key`

## Message Types Supported

Both protocols support the same CFX message types:
- Heartbeat
- WorkStarted
- WorkCompleted
- UnitsProcessed
- StationStateChanged
- FaultOccurred
- FaultCleared

## Dependencies

### New Package Added
- **MQTTnet v4.3.8.1034**: High-performance .NET MQTT client library

### Existing Dependencies (Unchanged)
- RabbitMQ.Client v7.1.2 (for AMQP support)
- Microsoft.Extensions.* packages for dependency injection and configuration

## Backward Compatibility

- **Default Protocol**: AMQP (for existing installations)
- **Existing Configurations**: All RabbitMQ settings remain unchanged
- **Migration**: No migration required - simply update and optionally switch to MQTT

## Usage Examples

### Programmatic Configuration
```csharp
// Configure for MQTT
services.Configure<ProtocolSetting>(options => 
{
    options.Type = ProtocolType.MQTT;
});

services.Configure<MqttSetting>(options => 
{
    options.BrokerHost = "mqtt.example.com";
    options.BrokerPort = 1883;
    options.Username = "user";
    options.Password = "pass";
});
```

### Publishing Messages
```csharp
// The messaging service is selected automatically based on configuration
using var messagingService = _messagingServiceFactory.CreateMessagingService();
await messagingService.PublishMessageAsync(exchangeName, routingKey, message);
```

## Testing

### Unit Tests
- `MessagingServiceFactoryTests`: Tests protocol selection logic
- `ProtocolSettingsTests`: Tests configuration classes

### Connection Testing
The configuration UI includes connection testing for both protocols:
- AMQP: Tests RabbitMQ connection parameters
- MQTT: Tests MQTT broker connection parameters

## Security Considerations

### MQTT Security
- **Authentication**: Username/password authentication supported
- **TLS/SSL**: Optional TLS encryption (UseTls setting)
- **Client ID**: Configurable client identification
- **Clean Session**: Configurable session persistence

### AMQP Security (Unchanged)
- Existing RabbitMQ security features remain available

## Performance Notes

- **MQTT**: Lightweight protocol, ideal for IoT scenarios
- **AMQP**: Enterprise messaging with advanced features
- **Memory Usage**: Minimal additional memory footprint
- **Connection Pooling**: Services use dependency injection lifecycle management

## Troubleshooting

### Common MQTT Issues
1. **Connection Failed**: Check broker host/port and network connectivity
2. **Authentication Failed**: Verify username/password if authentication is enabled
3. **Topic Publishing Failed**: Check topic prefix and permissions

### Logs
Both protocols provide detailed logging through the standard logging infrastructure:
```csharp
_logger.LogInformation("Connected to MQTT broker at {BrokerHost}:{BrokerPort}", 
    mqttSetting.BrokerHost, mqttSetting.BrokerPort);
```

## Future Enhancements

Potential future improvements:
- MQTT subscriber implementation for bidirectional communication
- Advanced MQTT features (retained messages, QoS levels)
- Certificate-based authentication for MQTT
- Protocol-specific configuration validation
- Performance monitoring and metrics