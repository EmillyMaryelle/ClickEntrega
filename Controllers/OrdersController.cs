using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClickEntrega.Data;
using ClickEntrega.Models;
using ClickEntrega.Services;

namespace ClickEntrega.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ClickEntregaContext _context;
        private readonly IMessageBusService _messageBus;

        public OrdersController(ClickEntregaContext context, IMessageBusService messageBus)
        {
            _context = context;
            _messageBus = messageBus;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrder([FromQuery] Guid? companyId)
        {
            var query = _context.Order
                .Include(o => o.Client)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product) // Include Product details
                .Include(o => o.Delivery)
                    .ThenInclude(d => d.Courier)
                .AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(o => o.CompanyId == companyId);
            }

            return Ok(await query.ToListAsync());
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(Guid id)
        {
            var order = await _context.Order
                .Include(o => o.Client)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Delivery)
                    .ThenInclude(d => d.Courier)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        // GET: api/Orders/Client/5
        [HttpGet("Client/{clientId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByClientId(Guid clientId)
        {
            return Ok(await _context.Order
                .Where(o => o.ClientId == clientId)
                .Include(o => o.Client)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Delivery)
                    .ThenInclude(d => d.Courier)
                .Include(o => o.Review)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync());
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            // Simple logic: calculate total, check stock (simplified)
            order.OrderDate = DateTime.Now;
            order.Status = OrderStatus.Pending;
            
            // Assuming order items come with ProductId and Quantity
            decimal total = 0;
            Guid? companyId = null;

            foreach(var item in order.Items)
            {
                var product = await _context.Product.FindAsync(item.ProductId);
                if(product != null)
                {
                    if (companyId == null)
                    {
                        companyId = product.CompanyId;
                    }
                    else if (companyId != product.CompanyId)
                    {
                         return BadRequest("Cannot mix products from different companies in one order.");
                    }

                    if (product.StockQuantity < item.Quantity)
                    {
                        return BadRequest($"Estoque insuficiente para o produto {product.Name}. Restam apenas {product.StockQuantity}.");
                    }

                    item.UnitPrice = product.Price;
                    total += item.UnitPrice * item.Quantity;
                    
                    // Deduct stock
                    product.StockQuantity -= item.Quantity;
                }
            }

            if (companyId == null)
            {
                return BadRequest("Order must contain valid products.");
            }

            order.CompanyId = companyId.Value;
            order.TotalAmount = total;

            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            // Envia notificação via RabbitMQ quando pedido é criado
            _messageBus.PublishOrderNotification(
                order.Id,
                order.ClientId,
                $"Seu pedido #{order.Id} foi recebido e está aguardando confirmação!",
                "Pending"
            );

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        // PUT: api/Orders/5/Status
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] OrderStatusUpdateModel model)
        {
            var order = await _context.Order
                .Include(o => o.Client)
                .FirstOrDefaultAsync(o => o.Id == id);
                
            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status;
            order.Status = model.Status;
            
            if (model.Status == OrderStatus.Cancelled && !string.IsNullOrEmpty(model.RejectionReason))
            {
                order.RejectionReason = model.RejectionReason;
            }

            if (model.EstimatedDeliveryTime.HasValue)
            {
                order.EstimatedDeliveryTime = model.EstimatedDeliveryTime;
            }
            
            await _context.SaveChangesAsync();

            // Envia notificação via RabbitMQ quando status muda
            var statusMessages = new Dictionary<OrderStatus, string>
            {
                { OrderStatus.Confirmed, $"Seu pedido #{order.Id} foi confirmado!" },
                { OrderStatus.Preparation, $"Seu pedido #{order.Id} está sendo preparado!" },
                { OrderStatus.ReadyForDelivery, $"Seu pedido #{order.Id} está pronto para entrega!" },
                { OrderStatus.OutForDelivery, $"Seu pedido #{order.Id} saiu para entrega!" },
                { OrderStatus.Delivered, $"Seu pedido #{order.Id} foi entregue! Obrigado pela preferência!" },
                { OrderStatus.Cancelled, $"Seu pedido #{order.Id} foi cancelado. Motivo: {order.RejectionReason ?? "Não informado"}" },
                { OrderStatus.Problem, $"Houve um problema com seu pedido #{order.Id}. Entraremos em contato em breve." }
            };

            if (statusMessages.ContainsKey(model.Status))
            {
                _messageBus.PublishOrderNotification(
                    order.Id,
                    order.ClientId,
                    statusMessages[model.Status],
                    model.Status.ToString()
                );

                _messageBus.PublishOrderStatusChange(
                    order.Id,
                    order.ClientId,
                    model.Status.ToString(),
                    order.EstimatedDeliveryTime
                );
            }

            return Ok(order);
        }

        public class OrderStatusUpdateModel
        {
            public OrderStatus Status { get; set; }
            public string? RejectionReason { get; set; }
            public DateTime? EstimatedDeliveryTime { get; set; }
        }
        
        // POST: api/Orders/5/Payment
        [HttpPost("{id}/Payment")]
        public async Task<IActionResult> AddPayment(Guid id, [FromBody] Payment payment)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            
            payment.OrderId = id;
            payment.PaymentDate = DateTime.Now;
            _context.Payment.Add(payment);
            await _context.SaveChangesAsync();
            
            return Ok(payment);
        }
    }
}

