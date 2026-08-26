using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("user_status")]
    public class UserStatus
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Required]
        [StringLength(30)]
        [Column("status_name")]
        public required string StatusName { get; set; }

        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
