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
    public class ClientsController : ControllerBase
    {
        private readonly ClickEntregaContext _context;

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public ClientsController(ClickEntregaContext context)
        {
            _context = context;
        }

        // GET: api/Clients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClient()
        {
            return await _context.Client.ToListAsync();
        }

        // GET: api/Clients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(Guid id)
        {
            var client = await _context.Client.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            return client;
        }

        // PUT: api/Clients/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClient(Guid id, Client client)
        {
            if (id != client.Id)
            {
                return BadRequest();
            }

            _context.Entry(client).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Clients
        [HttpPost]
        public async Task<ActionResult<Client>> PostClient(Client client)
        {
            if (string.IsNullOrEmpty(client.Password) || client.Password.Length < 6)
            {
                return BadRequest("A senha deve ter pelo menos 6 caracteres.");
            }

            if (await _context.Client.AnyAsync(c => c.Email == client.Email))
            {
                return BadRequest("Email já cadastrado.");
            }

            _context.Client.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetClient", new { id = client.Id }, client);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<Client>> Login([FromBody] LoginRequest login)
        {
            var client = await _context.Client
                .FirstOrDefaultAsync(c => c.Email == login.Email && c.Password == login.Password);

            if (client == null)
            {
                return Unauthorized("Email ou senha inválidos.");
            }

            return client;
        }

        // DELETE: api/Clients/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(Guid id)
        {
            var client = await _context.Client.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            _context.Client.Remove(client);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Clients/5/Orders
        [HttpGet("{id}/Orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetClientOrders(Guid id)
        {
             var orders = await _context.Order
                .Where(o => o.ClientId == id)
                .Include(o => o.Items)
                .Include(o => o.Delivery)
                .Include(o => o.Payment)
                .ToListAsync();
            return orders;
        }

        private bool ClientExists(Guid id)
        {
            return _context.Client.Any(e => e.Id == id);
        }
    }
}

