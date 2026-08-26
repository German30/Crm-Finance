using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("status_id")]
        public int StatusId { get; set; } = 1;

        [Required]
        [StringLength(100)]
        [Column("name")]
        public required string Name { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        [Column("email")]
        public required string Email { get; set; }

        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public required string PasswordHash { get; set; }

        [Column("creation_date")]
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(RoleId))]
        public virtual Role? Role { get; set; }

        [ForeignKey(nameof(StatusId))]
        public virtual UserStatus? Status { get; set; }

        public virtual ICollection<Client> AssignedClients { get; set; } = new List<Client>();

        public virtual ICollection<ProductsContract> ProductsContracts { get; set; } = new List<ProductsContract>();

        public virtual ICollection<ComercialOportunity> ComercialOportunities { get; set; } = new List<ComercialOportunity>();
    }
}
