using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("type_person")]
    public class TypePerson
    {
        [Key]
        [Column("type_person_id")]
        public int TypePersonId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("type_name")]
        public required string TypeName { get; set; }

        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
    }
}
