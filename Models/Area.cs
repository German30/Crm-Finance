using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("area")]
    public class Area
    {
        [Key]
        [Column("area_id")]
        public int AreaId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("area_name")]
        public required string AreaName { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Role> Role { get; set; } = new List<Role>();

        public virtual ICollection<FinanceProduct> FinanceProduct { get; set; } = new List<FinanceProduct>();
    }
}
