using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMFinaciertoBackend.Models
{
    [Table("phisic_person_client")]
    public class PhisicPersonClient
    {
        [Key]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("names")]
        public required string Names { get; set; }

        [Required]
        [StringLength(50)]
        [Column("father_lastname")]
        public required string FatherLastName { get; set; }

        [Required]
        [StringLength(50)]
        [Column("mother_lastname")]
        public required string MotherLastName { get; set; }

        [Column("birth_date")]
        public DateTime BirthDate { get; set; }

        [Column("gender_id")]
        public int GenderId { get; set; }

        [Column("civil_state_id")]
        public int CivilStateId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        [ForeignKey(nameof(GenderId))]
        public virtual Gender? Gender { get; set; }

        [ForeignKey(nameof(CivilStateId))]
        public virtual CivilState? CiviState { get; set; }
    }
}
