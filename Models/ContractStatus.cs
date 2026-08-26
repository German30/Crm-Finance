using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("contract_status")]
    public class ContractStatus
    {
        [Key]
        [Column("contract_status_id")]
        public int ContractSatusId { get; set; }

        [StringLength(50)]
        [Column("contract_status_name")]
        public string? ContractStatusName { get; set; }

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ProductsContract> ProductsContracts { get; set; } = new List<ProductsContract>();
    }
}
