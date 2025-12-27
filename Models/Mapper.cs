using System.ComponentModel.DataAnnotations;
using System;

namespace ApiProductsRest.Models
{
    public class Mapper
    {
            public int? Id { get; set; }

            [Required]
            public string? Nome { get; set; }

            [Required]
            public string? Descricao { get; set; }

            [Required]
            public decimal? Preco { get; set; }
    }
}
