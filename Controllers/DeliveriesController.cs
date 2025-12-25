using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClickEntrega.Data;
using ClickEntrega.Models;

namespace ClickEntrega.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveriesController : ControllerBase
    {
        private readonly ClickEntregaContext _context;

        public DeliveriesController(ClickEntregaContext context)
        {
            _context = context;
        }

        // GET: api/Deliveries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Delivery>>> GetDelivery()
        {
            return await _context.Delivery.Include(d => d.Order).Include(d => d.Courier).ToListAsync();
        }

        // GET: api/Deliveries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Delivery>> GetDelivery(Guid id)
        {
            var delivery = await _context.Delivery
                .Include(d => d.Order)
                .Include(d => d.Courier)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
            {
                return NotFound();
            }

            return delivery;
        }

        // POST: api/Deliveries
        [HttpPost]
        public async Task<ActionResult<Delivery>> PostDelivery(Delivery delivery)
        {
            _context.Delivery.Add(delivery);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDelivery", new { id = delivery.Id }, delivery);
        }

        // PUT: api/Deliveries/5/Status
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateStatus(int id, DeliveryStatus status, string location)
        {
            var delivery = await _context.Delivery.FindAsync(id);
            if (delivery == null)
            {
                return NotFound();
            }

            delivery.Status = status;
            if(!string.IsNullOrEmpty(location))
            {
                delivery.TrackingLocation = location;
            }
            
            await _context.SaveChangesAsync();

            return Ok(delivery);
        }
        
        // PUT: api/Deliveries/5/AssignCourier
        [HttpPut("{id}/AssignCourier/{courierId}")]
        public async Task<IActionResult> AssignCourier(Guid id, Guid courierId)
        {
            var delivery = await _context.Delivery.FindAsync(id);
            if (delivery == null) return NotFound("Delivery not found");
            
            var courier = await _context.Courier.FindAsync(courierId);
            if (courier == null) return NotFound("Courier not found");
            
            delivery.CourierId = courierId;
            delivery.Status = DeliveryStatus.Pending; // Or PickedUp?
            
            await _context.SaveChangesAsync();
            return Ok(delivery);
        }

        // POST: api/Deliveries/Order/5/Assign/3
        [HttpPost("Order/{orderId}/Assign/{courierId}")]
        public async Task<ActionResult<Delivery>> AssignCourierToOrder(Guid orderId, Guid courierId)
        {
            var order = await _context.Order.FindAsync(orderId);
            if (order == null) return NotFound("Order not found");

            var courier = await _context.Courier.FindAsync(courierId);
            if (courier == null) return NotFound("Courier not found");

            var delivery = await _context.Delivery.FirstOrDefaultAsync(d => d.OrderId == orderId);
            
            if (delivery == null)
            {
                delivery = new Delivery
                {
                    OrderId = orderId,
                    CourierId = courierId,
                    Status = DeliveryStatus.Pending,
                    Address = "Address from Order (Need to implement)", 
                    TrackingCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                };
                
                // Try to get address from Client if available
                /*
                var client = await _context.Client.FindAsync(order.ClientId);
                if(client != null) delivery.Address = client.Address;
                */
                // For now, just placeholder or we need to fetch client.
                // Let's rely on the fact that we can update it later or client should provide it.
                // Actually, Order doesn't have Address field directly, it's on Client. 
                // Ideally Order should snapshot the address.
                
                 _context.Delivery.Add(delivery);
            }
            else
            {
                delivery.CourierId = courierId;
                // delivery.Status = DeliveryStatus.Pending; // Keep existing status or reset?
            }

            await _context.SaveChangesAsync();
            return Ok(delivery);
        }
    }
}

