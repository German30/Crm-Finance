using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("clients")]
    public class Client
    {
        [Key]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("type_person_id")]
        public int TypePersonId { get; set; }

        [StringLength(50)]
        [Column("fiscal_id")]
        public string? FiscalId { get; set; }

        [StringLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Column("phone")]
        public string? Phone { get; set; }

        [Column("address_fiscal")]
        public string? AddressFiscal { get; set; }

        [Column("assigned_user_id")]
        public int? AssignedUserId { get; set; }

        [Column("register_date")]
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(TypePersonId))]
        public virtual TypePerson? TypePerson { get; set; }

        [ForeignKey(nameof(AssignedUserId))]
        public virtual User? AssignedUser { get; set; }

        public virtual PhisicPersonClient? PhisicPerson { get; set; }

        public virtual MoralPersonClient? MoralPerson { get; set; }

        public virtual ICollection<ProductsContract> ProductsContracts { get; set; } = new List<ProductsContract>();

        public virtual ICollection<ComercialOportunity> ComercialOportunities { get; set; } = new List<ComercialOportunity>();
    }
}
