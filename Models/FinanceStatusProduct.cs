using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("finace_status_product")]
    public class FinanceStatusProduct
    {
        [Key]
        [Column("finance_status_product_id")]
        public int FinanceStatusProductId { get; set; }

        [StringLength(50)]
        [Column("finance_status_product_name")]
        public string? FinanceStatusProductName { get; set; }

        [Column("register_date")]
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<FinanceProduct> FinanceProducts { get; set; } = new List<FinanceProduct>();
    }
}
