using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("pay_form")]
    public class PayForm
    {
        [Key]
        [Column("pay_form_id")]
        public int PayFormId { get; set; }

        [StringLength(50)]
        [Column("pay_form_name")]
        public string? PayFormName { get; set; }

        [Column("date_created")]
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public virtual ICollection<InsuranceContract> InsuranceContracts { get; set; } = new List<InsuranceContract>();
    }
}
