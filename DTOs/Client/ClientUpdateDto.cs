using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Client
{
    public record PhisicClientUpdateDto(
        [Required, StringLength(50)] string FiscalId,
        [Required, EmailAddress, StringLength(100)] string Email,
        [StringLength(20)] string? Phone,
        string? AddressFiscal,
        int? AssignedUserId,
        [Required, StringLength(100)] string Name,
        [Required, StringLength(50)] string FatherLastName,
        [Required, StringLength(50)] string MotherLastName,
        [Required, RequiredDate] DateTime BirthDate,
        [Required] int GenderId,
        [Required] int CivilStateId
    );

    public record MoralClientUpdateDto(
        [Required, StringLength(50)] string FiscalId,
        [Required, EmailAddress, StringLength(100)] string Email,
        [StringLength(20)] string? Phone,
        string? AddressFiscal,
        int? AssignedUserId,
        [Required, StringLength(200)] string SocialRazon,
        [StringLength(150)] string? ComercialName,
        [Required, RequiredDate] DateTime DateConstitucion,
        [StringLength(150)] string? ComercialActivity,
        [Required, StringLength(150)] string RepresentativeLegalName,
        [StringLength(50)] string? RepresentativeId
    );
}
