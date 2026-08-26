using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Oportunity
{
    public record OportunityUpdateDto(
        [Required] int UserId,
        [Range(0, DecimalPrecision.Money)] decimal? EstimatedMont,
        [Required] int StageId,
        [Range(0, 100)] int SuccessProbability,
        [RequiredDate] DateTime? DateEstimatedClose
    );

    public record OportunityStageUpdateDto(
        [Required] int StageId,
        [Range(0, 100)] int? SuccessProbability
    );
}
