using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PriceGenius.API.Services;

public interface IRabbitMqService : IAsyncDisposable
{
    Task InitializeAsync();
    Task PublishAsync(string queueName, object message);
    Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken) where T : class;
}

public class RabbitMqService : IRabbitMqService
{
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public const string MarketAnalysisQueue = "market_analysis_queue";
    public const string PriceUpdateQueue = "price_update_queue";

    public RabbitMqService(ILogger<RabbitMqService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "admin",
            Password = _configuration["RabbitMQ:Password"] ?? "admin123",
            ClientProvidedName = "PriceGenius.API",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        var retryCount = 0;
        const int maxRetries = 10;

        while (retryCount < maxRetries)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                _publishChannel = await _connection.CreateChannelAsync();

                // Declare queues
                await _publishChannel.QueueDeclareAsync(
                    queue: MarketAnalysisQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                await _publishChannel.QueueDeclareAsync(
                    queue: PriceUpdateQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _logger.LogInformation("✅ RabbitMQ connection established and queues declared.");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning("⏳ RabbitMQ connection attempt {Attempt}/{Max} failed: {Error}", retryCount, maxRetries, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(retryCount * 2, 30)));
            }
        }

        throw new Exception("Failed to connect to RabbitMQ after maximum retries.");
    }

    public async Task PublishAsync(string queueName, object message)
    {
        if (_publishChannel == null)
            throw new InvalidOperationException("RabbitMQ is not initialized.");

        await _publishLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json"
            };

            await _publishChannel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogInformation("📤 Published message to {Queue}: {Message}", queueName, json[..Math.Min(json.Length, 200)]);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    public async Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken) where T : class
    {
        if (_connection == null)
            throw new InvalidOperationException("RabbitMQ is not initialized.");

        var channel = await _connection.CreateChannelAsync();
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message != null)
                {
                    await handler(message);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("✅ Successfully processed message from {Queue}", queueName);
                }
                else
                {
                    _logger.LogWarning("⚠️ Failed to deserialize message from {Queue}", queueName);
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing message from {Queue}", queueName);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("👂 Subscribed to {Queue}", queueName);

        // Keep alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await channel.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_publishChannel != null)
        {
            await _publishChannel.CloseAsync();
            _publishChannel.Dispose();
        }
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}
