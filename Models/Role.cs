using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("role")]
    public class Role
    {
        [Key]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("area_id")]
        public int AreaId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("role_name")]
        public required string RoleName { get; set; }

        [Required]
        [StringLength(100)]
        [Column("category")]
        public required string Category { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AreaId))]
        public virtual Area? Area { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
