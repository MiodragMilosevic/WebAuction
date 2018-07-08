namespace WebAuction.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("SystemParameter")]
    public partial class SystemParameter
    {
        public int Id { get; set; }

        public int RecentAuction { get; set; }

        public int DefaultAuctionTime { get; set; }

        public int SilverPackage { get; set; }

        public int GoldPackage { get; set; }

        public int PlatinumPackage { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; }

        public decimal TokensPrice { get; set; }
    }
}
