using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ClickEntrega.Services
{
    public interface IMessageBusService
    {
        void PublishOrderNotification(int orderId, int clientId, string message, string? orderStatus = null);
        void PublishOrderStatusChange(int orderId, int clientId, string status, DateTime? estimatedDeliveryTime = null);
        void Publish(string queue, string message);
    }

    public class MessageBusService : IMessageBusService, IDisposable
    {
        private readonly IConnection? _connection;
        private readonly IModel? _channel;
        private readonly ILogger<MessageBusService> _logger;
        private const string NOTIFICATIONS_QUEUE = "notifications";
        private const string ORDER_STATUS_QUEUE = "order_status";

        public MessageBusService(ILogger<MessageBusService> logger, IConfiguration configuration)
        {
            _logger = logger;
            
            try
            {
                var rabbitConfig = configuration.GetSection("RabbitMQ");
                
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

                // Declara filas
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declara filas
                _channel.QueueDeclare(queue: NOTIFICATIONS_QUEUE,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                _channel.QueueDeclare(queue: ORDER_STATUS_QUEUE,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                _logger.LogInformation("RabbitMQ connection established");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Notifications will not be sent. The application will continue to work normally.");
                // Em caso de erro, não quebra a aplicação
                _connection = null;
                _channel = null;
            }
        }

        public void PublishOrderNotification(int orderId, int clientId, string message, string? orderStatus = null)
        {
            try
            {
                if (_channel == null || !_channel.IsOpen) return;

                var notification = new
                {
                    OrderId = orderId,
                    ClientId = clientId,
                    Message = message,
                    OrderStatus = orderStatus,
                    Timestamp = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Mensagem persiste mesmo se RabbitMQ reiniciar

                _channel.BasicPublish(exchange: "",
                                     routingKey: NOTIFICATIONS_QUEUE,
                                     basicProperties: properties,
                                     body: body);

                _logger.LogInformation("Notification published for Order {OrderId}, Client {ClientId}", orderId, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing notification for Order {OrderId}", orderId);
            }
        }

        public void PublishOrderStatusChange(int orderId, int clientId, string status, DateTime? estimatedDeliveryTime = null)
        {
            try
            {
                if (_channel == null || !_channel.IsOpen) return;

                var statusChange = new
                {
                    OrderId = orderId,
                    ClientId = clientId,
                    Status = status,
                    EstimatedDeliveryTime = estimatedDeliveryTime,
                    Timestamp = DateTime.UtcNow
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(statusChange));

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(exchange: "",
                                     routingKey: ORDER_STATUS_QUEUE,
                                     basicProperties: properties,
                                     body: body);

                _logger.LogInformation("Order status change published for Order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing order status change for Order {OrderId}", orderId);
            }
        }

        public void Publish(string queue, string message)
        {
            try
            {
                if (_channel == null || !_channel.IsOpen) return;

                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.QueueDeclare(queue: queue,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                _channel.BasicPublish(exchange: "",
                                     routingKey: queue,
                                     basicProperties: properties,
                                     body: body);

                _logger.LogInformation("Message published to {Queue}", queue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to {Queue}", queue);
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}


