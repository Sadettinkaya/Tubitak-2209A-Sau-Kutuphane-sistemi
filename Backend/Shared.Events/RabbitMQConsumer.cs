using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Shared.Events;

public class RabbitMQConsumer : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;
    private readonly string _queueName;

    public RabbitMQConsumer(string hostName, string userName, string password, string queueName, string exchangeName = "library_events")
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
        _queueName = queueName;

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );

        // Declare queue
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );
    }

    public void Subscribe<T>(string routingKey, Action<T> handler) where T : class
    {
        // Bind queue to exchange with routing key
        _channel.QueueBind(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: routingKey
        );

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                if (message != null)
                {
                    handler(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer
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
