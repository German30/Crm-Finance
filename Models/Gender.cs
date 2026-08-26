using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("gender")]
    public class Gender
    {
        [Key]
        [Column("gender_id")]
        public int GenderId { get; set; }

        [StringLength(50)]
        [Column("gender_name")]
        public string? GenderName { get; set; }

        [Column("register_date")]
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PhisicPersonClient> PhisicPersonClients { get; set; } = new List<PhisicPersonClient>();
    }
}
