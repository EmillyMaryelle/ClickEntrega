using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClickEntrega.Data;
using ClickEntrega.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClickEntrega.Services
{
    public class OrderTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderTimeoutService> _logger;

        public OrderTimeoutService(IServiceProvider serviceProvider, ILogger<OrderTimeoutService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Timeout Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ClickEntregaContext>();
                        
                        var cutoffTime = DateTime.Now.AddMinutes(-3);
                        
                        var staleOrders = await context.Order
                            .Where(o => o.Status == OrderStatus.Pending && o.OrderDate <= cutoffTime)
                            .ToListAsync(stoppingToken);

                        if (staleOrders.Any())
                        {
                            foreach (var order in staleOrders)
                            {
                                order.Status = OrderStatus.Cancelled;
                                order.RejectionReason = "Tempo de espera excedido";
                                _logger.LogInformation($"Order {order.Id} cancelled due to timeout.");
                            }

                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Order Timeout Service");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
