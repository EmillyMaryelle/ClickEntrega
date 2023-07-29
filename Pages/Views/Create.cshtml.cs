using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ApiProductsRest.Data;
using ApiProductsRest.Models;

namespace ApiProductsRest.Pages.Views
{
    public class CreateModel : PageModel
    {
        private readonly ApiProductsRest.Data.ApiProductsRestContext _context;

        public CreateModel(ApiProductsRest.Data.ApiProductsRestContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Mapper Mapper { get; set; } = default!;
        

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
          if (!ModelState.IsValid || _context.Mapper == null || Mapper == null)
            {
                return Page();
            }

            _context.Mapper.Add(Mapper);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
