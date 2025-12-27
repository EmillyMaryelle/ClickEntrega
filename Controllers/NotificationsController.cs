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
    public class NotificationsController : ControllerBase
    {
        private readonly ClickEntregaContext _context;
        private readonly IMessageBusService _messageBus;

        public NotificationsController(ClickEntregaContext context, IMessageBusService messageBus)
        {
            _context = context;
            _messageBus = messageBus;
        }

        // GET: api/Notifications/Client/5
        [HttpGet("Client/{clientId}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetClientNotifications(int clientId)
        {
            try
            {
                return await _context.Notification
                    .Where(n => n.ClientId == clientId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex}");
                return StatusCode(500, ex.Message);
            }
        }

        // POST: api/Notifications
        [HttpPost]
        public async Task<ActionResult<Notification>> PostNotification(Notification notification)
        {
            // Se a notificação já tem ID, significa que foi criada diretamente no banco
            // Caso contrário, publica via RabbitMQ para processamento assíncrono
            if (notification.Id == 0)
            {
                _messageBus.PublishOrderNotification(
                    notification.OrderId ?? 0,
                    notification.ClientId,
                    notification.Message
                );
                
                // Retorna sucesso, mas a notificação será processada pelo consumer
                return Ok(new { message = "Notificação enviada para processamento" });
            }
            else
            {
                _context.Notification.Add(notification);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetClientNotifications", new { clientId = notification.ClientId }, notification);
            }
        }

        // PUT: api/Notifications/5/Read
        [HttpPut("{id}/Read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notification.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

