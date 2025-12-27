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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Delivery>>> GetDelivery()
        {
            return await _context.Delivery.Include(d => d.Order).Include(d => d.Courier).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Delivery>> GetDelivery(int id)
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

        [HttpPost]
        public async Task<ActionResult<Delivery>> PostDelivery(Delivery delivery)
        {
            _context.Delivery.Add(delivery);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDelivery", new { id = delivery.Id }, delivery);
        }

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
        
        [HttpPut("{id}/AssignCourier/{courierId}")]
        public async Task<IActionResult> AssignCourier(int id, int courierId)
        {
            var delivery = await _context.Delivery.FindAsync(id);
            if (delivery == null) return NotFound("Delivery not found");
            
            var courier = await _context.Courier.FindAsync(courierId);
            if (courier == null) return NotFound("Courier not found");
            
            delivery.CourierId = courierId;
            delivery.Status = DeliveryStatus.Pending;
            
            await _context.SaveChangesAsync();
            return Ok(delivery);
        }

        [HttpPost("Order/{orderId}/Assign/{courierId}")]
        public async Task<ActionResult<Delivery>> AssignCourierToOrder(int orderId, int courierId)
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
                
                 _context.Delivery.Add(delivery);
            }
            else
            {
                delivery.CourierId = courierId;
            }

            await _context.SaveChangesAsync();
            return Ok(delivery);
        }
    }
}
