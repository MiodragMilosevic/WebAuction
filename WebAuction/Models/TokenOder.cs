namespace WebAuction.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class TokenOder
    {
        public int Id { get; set; }

        public int Buyer { get; set; }

        public int TokensAmount { get; set; }

        public decimal Price { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; }

        [Required]
        [StringLength(10)]
        public string Status { get; set; }

        public virtual User User { get; set; }
    }
}
