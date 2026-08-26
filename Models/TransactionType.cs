using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("transaction_type")]
    public class TransactionType
    {
        [Key]
        [Column("transaction_type_id")]
        public int TransactionTypeId { get; set; }

        [StringLength(50)]
        [Column("transaction_type_name")]
        public string? TransactionTypeName { get; set; }

        [Column("data_created")]
        public DateTime DataCreated { get; set; } = DateTime.UtcNow;

        public virtual ICollection<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
    }
}
