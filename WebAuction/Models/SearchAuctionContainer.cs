namespace WebAuction.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Web;
    using System.Linq;

    public partial class SearchAuctionContainer
    {
        public SearchAuction searchAuction { get; set; }
        public IEnumerable<Auction> auctionList { get; set; }
    }
}
