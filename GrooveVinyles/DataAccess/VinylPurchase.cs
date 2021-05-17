using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrooveVinyles.DataAccess
{
    [Table("VinylPurchase")]
    public class VinylPurchase
    {
        [Column("VinylPurchaseID")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int VinylPurchaseId { get; set; }

        [Column("Amount")] [Required] public int Amount { get; set; }
        public Vinyl Vinyl { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}