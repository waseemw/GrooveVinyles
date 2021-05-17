using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrooveVinyles.DataAccess
{
    [Table("Artist")]
    public class Artist
    {
        [Column("ArtistID")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int ArtistId { get; set; }
        [Column("ArtistName")] [Required] public string ArtistName { get; set; }

        public ICollection<Vinyl> Vinyles { get; set; }
    }
}