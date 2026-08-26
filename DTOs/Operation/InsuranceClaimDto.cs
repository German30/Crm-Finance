using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Operation
{
    public record InsuranceClaimCreateDto(
        [Required] int ContractId,
        [Required, StringLength(50)] string ReportNumber,
        [Required, RequiredDate] DateTime DateOccurrence,
        [Required, Range(0.01, DecimalPrecision.Money)] decimal AmountClaimed,
        [Range(0, DecimalPrecision.Money)] decimal AmountPaid,
        [Required] int DisasterStateId,
        string? ReportDetails
    );

    public record InsuranceClaiResponseDto(
        int InsuranceId,
        string ReferenceNumber,
        string ReportNumber,
        DateTime DateOccurrence,
        decimal AmountClaimed,
        [Range(0, DecimalPrecision.Money)] decimal AmountPaid,
        string DisasterStateName,
        string? ReportDetails,
        DateTime DateRegister
    );
}
