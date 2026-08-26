using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("finance_products")]
    public class FinanceProduct
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("area_id")]
        public int AreaId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("product_name")]
        public required string ProductNamme { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("tasa_interes_o_prima_base", TypeName = "decimal(5,2)")]
        public decimal TasaInteresOPrimaBase { get; set; } = 0.00m;

        [Column("finance_status_product_id")]
        public int? FinanceStatusProductId { get; set; } = 1;

        [Column("date_creation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AreaId))]
        public virtual Area? Area { get; set; }

        [ForeignKey(nameof(FinanceStatusProductId))]
        public virtual FinanceStatusProduct? FinanceStatusProduct { get; set; }

        public virtual ICollection<ProductsContract> ProductsContracts { get; set; } = new List<ProductsContract>();

        public virtual ICollection<ComercialOportunity> ComercialOportunities { get; set; } = new List<ComercialOportunity>();
    }
}
