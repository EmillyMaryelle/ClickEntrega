using ClickEntrega.Services;

namespace ClickEntrega.Services
{
    public class FakeMessageBusService : IMessageBusService
    {
        public void PublishOrderNotification(Guid orderId, Guid clientId, string message, string? orderStatus = null)
        {
            // Do nothing - Fake implementation for environment without RabbitMQ
        }

        public void PublishOrderStatusChange(Guid orderId, Guid clientId, string status, DateTime? estimatedDeliveryTime = null)
        {
            // Do nothing - Fake implementation for environment without RabbitMQ
        }
    }
}
