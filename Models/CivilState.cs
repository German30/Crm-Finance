using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("civil_state")]
    public class CivilState
    {
        [Key]
        [Column("civil_state_id")]
        public int CivilStateId { get; set; }

        [StringLength(100)]
        [Column("civil_state_name")]
        public string? CivilStateName { get; set; }

        [Column("register_date")]
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PhisicPersonClient> PhisicPersonClients { get; set; } = new List<PhisicPersonClient>();
    }
}
