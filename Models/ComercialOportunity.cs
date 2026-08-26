using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("comercial_oportunities")]
    public class ComercialOportunity
    {
        [Key]
        [Column("oportunity_id")]
        public int OportunotyId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("estimated_mont", TypeName = "decimal(15,2)")]
        public decimal? EstimatedMont { get; set; }

        [Column("stage_id")]
        public int StageId { get; set; }

        [Column("success_probability")]
        public int SuccesProbability { get; set; } = 10;

        [Column("date_estimated_close")]
        public DateTime? DateEstimatedClose { get; set; }

        [Column("date_register")]
        public DateTime DateRegister { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual FinanceProduct? FinanceProduct { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(StageId))]
        public virtual Stage? Stage { get; set; }
    }
}
