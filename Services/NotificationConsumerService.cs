using Microsoft.EntityFrameworkCore;
using ClickEntrega.Data;
using ClickEntrega.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ClickEntrega.Services
{
    public class NotificationConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IModel? _channel;
        private const string NOTIFICATIONS_QUEUE = "notifications";

        public NotificationConsumerService(
            IServiceProvider serviceProvider,
            ILogger<NotificationConsumerService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var rabbitConfig = _configuration.GetSection("RabbitMQ");
                
                var factory = new ConnectionFactory()
                {
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") 
                        ?? rabbitConfig["HostName"] 
                        ?? "localhost",
                    UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") 
                        ?? rabbitConfig["UserName"] 
                        ?? "guest",
                    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") 
                        ?? rabbitConfig["Password"] 
                        ?? "guest",
                    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") 
                        ?? rabbitConfig["Port"] 
                        ?? "5672")
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: NOTIFICATIONS_QUEUE,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    try
                    {
                        var notification = JsonSerializer.Deserialize<NotificationMessage>(message);
                        if (notification != null)
                        {
                            await ProcessNotification(notification);
                        }

                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing notification: {Message}", message);
                        
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                _channel.BasicConsume(queue: NOTIFICATIONS_QUEUE,
                                    autoAck: false,
                                    consumer: consumer);

                _logger.LogInformation("Notification consumer started");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to start Notification Consumer. RabbitMQ might be unreachable.");
            }
        }

        private async Task ProcessNotification(NotificationMessage notification)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ClickEntregaContext>();

            try
            {
                var dbNotification = new Notification
                {
                    ClientId = notification.ClientId,
                    OrderId = notification.OrderId,
                    Message = notification.Message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                context.Notification.Add(dbNotification);
                await context.SaveChangesAsync();

                _logger.LogInformation("Notification saved for Client {ClientId}, Order {OrderId}", 
                    notification.ClientId, notification.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notification to database");
                throw;
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            base.Dispose();
        }

        private class NotificationMessage
        {
            public int OrderId { get; set; }
            public int ClientId { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? OrderStatus { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
