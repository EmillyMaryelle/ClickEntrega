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
        public async Task<ActionResult<IEnumerable<Order>>> GetOrder([FromQuery] int? companyId)
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
        public async Task<ActionResult<Order>> GetOrder(int id)
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
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByClientId(int clientId)
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
            int? companyId = null;

            foreach(var item in order.Items)
            {
                var product = await _context.Product.FindAsync(item.ProductId);
                if(product != null)
                {
                    if(product.StockQuantity < item.Quantity)
                    {
                        return BadRequest($"Estoque insuficiente para o produto {product.Name}. Restam apenas {product.StockQuantity}.");
                    }

                    // Deduct stock
                    product.StockQuantity -= item.Quantity;

                    // Set UnitPrice from current product price if not set
                    if(item.UnitPrice == 0) item.UnitPrice = product.Price;
                    
                    total += item.UnitPrice * item.Quantity;
                    
                    if(companyId == null) companyId = product.CompanyId;
                    else if(companyId != product.CompanyId)
                    {
                         // Basic check: all items should be from same company
                         // return BadRequest("Pedidos devem conter itens de uma única empresa.");
                    }
                }
            }

            if(order.TotalAmount == 0) order.TotalAmount = total;
            if(order.CompanyId == 0 && companyId.HasValue) order.CompanyId = companyId.Value;

            // Create delivery record if needed
            if(order.Delivery == null)
            {
                 order.Delivery = new Delivery 
                 { 
                     Status = DeliveryStatus.Pending,
                     Address = "Endereço do Cliente" // Should come from request
                 };
            }

             // Create payment record if needed
            if(order.Payment == null)
            {
                 order.Payment = new Payment 
                 { 
                     Status = PaymentStatus.Pending,
                     Method = PaymentMethod.CreditCard,
                     Amount = total
                 };
            }


            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            // Publish message to RabbitMQ (or Fake)
            _messageBus.PublishOrderNotification(order.Id, order.ClientId, $"Novo pedido criado: {order.Id} - Valor: {order.TotalAmount}", order.Status.ToString());

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Orders/5/Status
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ClickEntrega.Models.DTOs.UpdateOrderStatusDto dto)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = dto.Status;
            
            if (!string.IsNullOrEmpty(dto.RejectionReason))
            {
                order.RejectionReason = dto.RejectionReason;
            }

            if (dto.EstimatedDeliveryTime.HasValue)
            {
                order.EstimatedDeliveryTime = dto.EstimatedDeliveryTime;
            }

            await _context.SaveChangesAsync();
            
            // Notify client (optional/mock)
            string friendlyStatus = GetFriendlyStatusName(dto.Status);
            _messageBus.PublishOrderNotification(order.Id, order.ClientId, $"Status do pedido: {friendlyStatus}", dto.Status.ToString());

            return NoContent();
        }

        private string GetFriendlyStatusName(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Pendente",
                OrderStatus.Confirmed => "Confirmado",
                OrderStatus.Preparation => "Em Preparo",
                OrderStatus.ReadyForDelivery => "Pronto para Entrega",
                OrderStatus.OutForDelivery => "Saiu para Entrega",
                OrderStatus.Delivered => "Entregue",
                OrderStatus.Cancelled => "Cancelado",
                OrderStatus.Problem => "Problema",
                _ => status.ToString()
            };
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.Id == id);
        }
    }
}
