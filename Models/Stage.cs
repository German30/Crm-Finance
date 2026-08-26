using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("stage")]
    public class Stage
    {
        [Key]
        [Column("stage_id")]
        public int StageId { get; set; }

        [StringLength(50)]
        [Column("stage_name")]
        public string? StageName { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ComercialOportunity> ComercialOportunities { get; set; } = new List<ComercialOportunity>();
    }
}
