using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("bank_transaction")]
    public class BankTransaction
    {
        [Key]
        [Column("transaction_id")]
        public int TransactionId { get; set; }

        [Column("contract_id")]
        public int ContractId { get; set; }

        [Column("transaction_type_id")]
        public int TransactionTypeId { get; set; }

        [Column("amount", TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        [Column("date_transaction")]
        public DateTime DateTransaction { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        [ForeignKey(nameof(ContractId))]
        public virtual BankContract? BankContract { get; set; }

        [ForeignKey(nameof(TransactionTypeId))]
        public virtual TransactionType? TransactionType { get; set; }
    }
}
