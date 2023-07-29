using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ApiProductsRest.Data;
using ApiProductsRest.Models;

namespace ApiProductsRest.Pages.Views
{
    public class DeleteModel : PageModel
    {
        private readonly ApiProductsRest.Data.ApiProductsRestContext _context;

        public DeleteModel(ApiProductsRest.Data.ApiProductsRestContext context)
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

            var mapper = await _context.Mapper.FirstOrDefaultAsync(m => m.Id == id);

            if (mapper == null)
            {
                return NotFound();
            }
            else 
            {
                Mapper = mapper;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null || _context.Mapper == null)
            {
                return NotFound();
            }
            var mapper = await _context.Mapper.FindAsync(id);

            if (mapper != null)
            {
                Mapper = mapper;
                _context.Mapper.Remove(Mapper);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
