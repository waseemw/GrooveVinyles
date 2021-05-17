using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrooveVinyles.DataAccess
{
    [Table("Order")]
    public class Order
    {
        [Column("OrderID")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int OrderId { get; set; }

        [Column("FullName")] [Required] public string FullName { get; set; }
        [Column("Address")] [Required] public string Address { get; set; }
        
        public ICollection<VinylPurchase> VinylPurchases { get; set; }
    }
}