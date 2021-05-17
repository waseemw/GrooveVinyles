using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrooveVinyles.DataAccess
{
    [Table("Vinyl")]
    public class Vinyl
    {
        [Column("VinylID")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int VinylId { get; set; }

        [Column("VinylName")] [Required] public string VinylName { get; set; }
        [Column("GenreName")] [Required] public string Genre { get; set; }

        [Column("VinylStock")]
        [Required]
        [DefaultValue(0)]
        public int VinylStock { get; set; }

        [Column("Price")] [Required] public int Price { get; set; }

        public Artist Artist { get; set; }
        
        public ICollection<VinylPurchase> VinylPurchases { get; set; }
    }
}