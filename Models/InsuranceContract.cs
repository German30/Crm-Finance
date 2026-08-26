using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("insurance_contranct")]
    public class InsuranceContract
    {
        [Key]
        [Column("contract_id")]
        public int ContractId { get; set; }

        [Column("insurance_sume_total", TypeName = "decimal(15,2)")]
        public decimal InsuranceSumeTotal { get; set; }

        [Column("total_annual_premium", TypeName = "decimal(15,2)")]
        public decimal TotalAnnualPremiu { get; set; }

        [Column("pay_form_id")]
        public int PayFormId { get; set; } = 4;

        [Column("porcent_deductible", TypeName = "decimal(4,2)")]
        public decimal PorcentDeductible { get; set; } = 0.00m;

        [Column("beneficiary_name")]
        public string? BeneficiaryName { get; set; }

        [ForeignKey(nameof(ContractId))]
        public virtual ProductsContract? ProductsContract { get; set; }

        [ForeignKey(nameof(PayFormId))]
        public virtual PayForm? PayForm { get; set; }

        public virtual ICollection<InsuranceClaim> InsuranceClaims { get; set; } = new List<InsuranceClaim>();
    }
}
