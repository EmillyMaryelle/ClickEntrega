using ClickEntrega.Services;
using Microsoft.Extensions.DependencyInjection;
using ClickEntrega.Data;
using ClickEntrega.Models;
using System;

namespace ClickEntrega.Services
{
    public class FakeMessageBusService : IMessageBusService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public FakeMessageBusService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void PublishOrderNotification(int orderId, int clientId, string message, string? orderStatus = null)
        {
            // In Fake mode, we write directly to DB
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ClickEntregaContext>();
                
                var notification = new Notification
                {
                    ClientId = clientId,
                    OrderId = orderId,
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                context.Notification.Add(notification);
                context.SaveChanges();
            }
        }

        public void PublishOrderStatusChange(int orderId, int clientId, string status, DateTime? estimatedDeliveryTime = null)
        {
             // Reuse the logic above or implement specific logic if needed
             // For now, let's just create a generic message if not covered by PublishOrderNotification
             // But usually PublishOrderNotification is called by the controller with the message already formatted.
             
             // If this method is called independently, we should handle it.
             // However, based on OrdersController, it calls PublishOrderNotification directly.
        }

        public void Publish(string queue, string message)
        {
            // Generic publish - hard to map to DB without structured data
        }
    }
}
