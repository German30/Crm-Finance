using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Operation
{
    public record InsuranceClaimUpdateDto(
        [Required] int DisasterStateId,
        [Range(0, DecimalPrecision.Money)] decimal AmountPaid,
        string? ReportDetails
    );
}
