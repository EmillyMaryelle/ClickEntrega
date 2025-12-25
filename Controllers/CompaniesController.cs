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
    public class CompaniesController : ControllerBase
    {
        private readonly ClickEntregaContext _context;

        public class LoginRequest
        {
            public string Name { get; set; }
            public string Password { get; set; }
        }

        public CompaniesController(ClickEntregaContext context)
        {
            _context = context;
        }

        // GET: api/Companies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompany()
        {
            return Ok(await _context.Company.ToListAsync());
        }

        // GET: api/Companies/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompany(Guid id)
        {
            var company = await _context.Company.FindAsync(id);

            if (company == null)
            {
                return NotFound();
            }

            return Ok(company);
        }

        // POST: api/Companies
        [HttpPost]
        public async Task<ActionResult<Company>> PostCompany(Company company)
        {
            if (await _context.Company.AnyAsync(c => c.Name == company.Name))
            {
                return BadRequest("Nome de empresa já cadastrado.");
            }

            _context.Company.Add(company);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCompany", new { id = company.Id }, company);
        }

        // POST: api/Companies/Login
        [HttpPost("Login")]
        public async Task<ActionResult<Company>> Login([FromBody] LoginRequest login)
        {
            var company = await _context.Company
                .FirstOrDefaultAsync(c => c.Name == login.Name && c.Password == login.Password);

            if (company == null)
            {
                return Unauthorized("Nome ou senha inválidos.");
            }

            return Ok(company);
        }

        // DELETE: api/Companies/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(Guid id)
        {
            var company = await _context.Company.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            _context.Company.Remove(company);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CompanyExists(Guid id)
        {
            return _context.Company.Any(e => e.Id == id);
        }
    }
}

