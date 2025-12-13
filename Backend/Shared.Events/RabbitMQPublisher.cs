using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Shared.Events;

public class RabbitMQPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;

    public RabbitMQPublisher(string hostName, string userName, string password, string exchangeName = "library_events")
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _exchangeName = exchangeName;

        // Declare exchange (topic type for routing)
        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );
    }

    public void Publish<T>(T message, string routingKey) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body
        );
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
