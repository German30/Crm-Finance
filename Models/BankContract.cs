using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("bank_contract")]
    public class BankContract
    {
        [Key]
        [Column("contract_id")]
        public int ContractId { get; set; }

        [StringLength(18)]
        [Column("interbank_code")]
        public string? InterbankCode { get; set; }

        [Column("balance_actual", TypeName = "decimal(15,2)")]
        public decimal BalanceActual { get; set; } = 0.00m;

        [Column("loan_amount_granted", TypeName = "decimal(15,2)")]
        public decimal LoanAmountGranted { get; set; } = 0.00m;

        [Column("agreed_interest_rate", TypeName = "decimal(5,2)")]
        public decimal AgreeInterestRate { get; set; }

        [Column("monthly_cutoff_day")]
        public int MonthlyCutoffDay { get; set; } = 1;

        [ForeignKey(nameof(ContractId))]
        public virtual ProductsContract? ProductsContract { get; set; }

        public virtual ICollection<BankTransaction> BankTransactions { get; set; } = new List<BankTransaction>();
    }
}
