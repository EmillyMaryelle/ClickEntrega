using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ApiProductsRest.Data;
using ApiProductsRest.Models;

namespace ApiProductsRest.Pages.Views
{
    public class EditModel : PageModel
    {
        private readonly ApiProductsRest.Data.ApiProductsRestContext _context;

        public EditModel(ApiProductsRest.Data.ApiProductsRestContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Mapper Mapper { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Mapper == null)
            {
                return NotFound();
            }

            var mapper =  await _context.Mapper.FirstOrDefaultAsync(m => m.Id == id);
            if (mapper == null)
            {
                return NotFound();
            }
            Mapper = mapper;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Mapper).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MapperExists(Mapper.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool MapperExists(int? id)
        {
          return (_context.Mapper?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
