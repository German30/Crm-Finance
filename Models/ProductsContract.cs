using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("products_contract")]
    public class ProductsContract
    {
        [Key]
        [Column("contract_id")]
        public int ContractId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("reference_number")]
        public required string ReferenceNumber { get; set; }

        [Column("date_opening_issue")]
        public DateTime DateOpeningIssue { get; set; }

        [Column("date_end")]
        public DateTime? DateEnd { get; set; }

        [Column("contract_status_id")]
        public int ContractStatusId { get; set; }

        [Column("date_register")]
        public DateTime DateRegister { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual FinanceProduct? FinanceProduct { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(ContractStatusId))]
        public virtual ContractStatus? ContractStatus { get; set; }

        public virtual BankContract? BankContract { get; set; }

        public virtual InsuranceContract? InsuranceContract { get; set; }
    }
}
