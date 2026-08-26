using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Oportunity
{
    public record OportunityCreateDto(
        [Required] int ClientId,
        [Required] int ProductId,
        [Required] int UserId,
        [Range(0, DecimalPrecision.Money)] decimal? EstimatedMont,
        [Required] int StageId,
        [RequiredDate] DateTime? DateEstimatedClose,
        [Range(0, 100)] int SuccessProbability = 10
    );

    public record OportunityResponseDto(
        int OportunityId,
        int ClientId,
        string ClientName,
        string PrdoductName,
        string AreaName,
        string AssignedUserName,
        [Range(0, DecimalPrecision.Money)] decimal? EstimatedMont,
        string StageName,
        int SuccesProbability,
        [RequiredDate] DateTime? DateEstimatedClose,
        DateTime DateRegister
    );
}
