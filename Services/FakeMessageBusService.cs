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
        }

        public void Publish(string queue, string message)
        {
        }
    }
}
