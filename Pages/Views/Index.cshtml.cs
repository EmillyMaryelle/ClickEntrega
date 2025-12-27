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
    public class IndexModel : PageModel
    {
        private readonly ApiProductsRest.Data.ApiProductsRestContext _context;

        public IndexModel(ApiProductsRest.Data.ApiProductsRestContext context)
        {
            _context = context;
        }

        public IList<Mapper> Mapper { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Mapper != null)
            {
                Mapper = await _context.Mapper.ToListAsync();
            }
        }
    }
}
