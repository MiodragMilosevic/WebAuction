namespace WebAuction.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Bid
    {
        public int Id { get; set; }

        public int Bidder { get; set; }

        public int Auction { get; set; }

        public DateTime BidOn { get; set; }

        public double? Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; }

        public virtual Auction Auction1 { get; set; }

        public virtual User User { get; set; }
    }
}
