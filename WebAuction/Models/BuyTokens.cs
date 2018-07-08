namespace WebAuction.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class BuyTokens
    {

        public string PackageType { get; set; }

        public string PhoneNumber { get; set; }

        public int UserID { get; set; }

    }
}
