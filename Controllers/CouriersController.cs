using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClickEntrega.Data;
using ClickEntrega.Models;
using ClickEntrega.Models.DTOs;

namespace ClickEntrega.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouriersController : ControllerBase
    {
        private readonly ClickEntregaContext _context;

        public CouriersController(ClickEntregaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Courier>>> GetCouriers([FromQuery] Guid? companyId)
        {
            var query = _context.Courier.AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(c => c.CompanyId == companyId);
            }

            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Courier>> GetCourier(Guid id)
        {
            var courier = await _context.Courier.FindAsync(id);

            if (courier == null)
            {
                return NotFound();
            }

            return courier;
        }

        [HttpPost]
        public async Task<ActionResult<Courier>> PostCourier(CreateCourierDto courierDto)
        {
            var courier = new Courier
            {
                Name = courierDto.Name,
                VehicleInfo = courierDto.VehicleInfo,
                Phone = courierDto.Phone,
                CompanyId = courierDto.CompanyId
            };

            _context.Courier.Add(courier);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetCourier", new { id = courier.Id }, courier);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourier(Guid id, Courier courier)
        {
            if (id != courier.Id)
            {
                return BadRequest();
            }

            _context.Entry(courier).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourierExists(id))
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourier(Guid id)
        {
            var courier = await _context.Courier.FindAsync(id);
            if (courier == null)
            {
                return NotFound();
            }

            _context.Courier.Remove(courier);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CourierExists(Guid id)
        {
            return _context.Courier.Any(e => e.Id == id);
        }
    }
}

