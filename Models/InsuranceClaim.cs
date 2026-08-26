using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("insurance_claims")]
    public class InsuranceClaim
    {
        [Key]
        [Column("insurance_id")]
        public int InsuranceId { get; set; }

        [Column("contract_id")]
        public int ContractId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("report_number")]
        public required string ReportNumber { get; set; }

        [Column("date_occurrence")]
        public DateTime DateOcurrence { get; set; }

        [Column("amount_claimed", TypeName = "decimal(15,2)")]
        public decimal AmountClaimed { get; set; }

        [Column("amount_paid", TypeName = "decimal(15,2)")]
        public decimal AmountPaid { get; set; } = 0.00m;

        [Column("disaster_state_id")]
        public int DisasterStateId { get; set; } = 1;

        [Column("report_details")]
        public string? ReportDetails { get; set; }

        [Column("date_register")]
        public DateTime DateRegister { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ContractId))]
        public virtual InsuranceContract? InsuranceContract { get; set; }

        [ForeignKey(nameof(DisasterStateId))]
        public virtual DisasterState? DisasterState { get; set; }
    }
}
