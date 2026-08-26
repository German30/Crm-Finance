using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("disaster_state")]
    public class DisasterState
    {
        [Key]
        [Column("disaster_state_id")]
        public int DisasterStateId { get; set; }

        [StringLength(50)]
        [Column("disaster_state_name")]
        public string? DisasterStateName { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public virtual ICollection<InsuranceClaim> InsuranceClaims { get; set; } = new List<InsuranceClaim>();
    }
}
