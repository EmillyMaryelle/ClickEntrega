using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiProductsRest.Models;

namespace ApiProductsRest.Data
{
    public class ApiProductsRestContext : DbContext
    {
        public ApiProductsRestContext (DbContextOptions<ApiProductsRestContext> options)
            : base(options)
        {
        }

        public DbSet<ApiProductsRest.Models.Mapper> Mapper { get; set; } = default!;
    }
}
