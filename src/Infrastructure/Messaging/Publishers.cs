using System.Text;
using Application.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public class LogMessagePublisher(ILogger<LogMessagePublisher> logger) : IMessagePublisher
{
    public Task PublishAsync(string messageType, string payload, CancellationToken ct)
    {
        logger.LogInformation("Published outbox event {Type} {Payload}", messageType, payload);
        return Task.CompletedTask;
    }
}

public sealed class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqMessagePublisher(string hostName, string exchange)
    {
        var factory = new ConnectionFactory { HostName = hostName };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);
        Exchange = exchange;
    }

    public string Exchange { get; }

    public Task PublishAsync(string messageType, string payload, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(payload);
        var props = _channel.CreateBasicProperties();
        props.Type = messageType;
        props.Persistent = true;
        _channel.BasicPublish(exchange: Exchange, routingKey: string.Empty, basicProperties: props, body: body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
