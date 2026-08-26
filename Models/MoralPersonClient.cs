using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("moral_person_client")]
    public class MoralPersonClient
    {
        [Key]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Required]
        [StringLength(200)]
        [Column("social_razon")]
        public required string SocialRazon { get; set; }

        [StringLength(150)]
        [Column("comercial_name")]
        public string? ComercialName { get; set; }

        [Column("date_constitucion")]
        public DateTime DateConstitucion { get; set; }

        [StringLength(150)]
        [Column("comercial_activity")]
        public string? ComercialActivity { get; set; }

        [Required]
        [StringLength(150)]
        [Column("representative_legal_name")]
        public required string RepreentativeLegalName { get; set; }

        [StringLength(50)]
        [Column("representative_id")]
        public string? RepresentativeId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }
    }
}
