using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Operation
{
    public record TransactionCreateDto(
        [Required] int ContractId,
        [Required] int TransactionTypeId,
        [Required, Range(0.01, DecimalPrecision.Money)] decimal Amount,
        [StringLength(255)] string? Description
    );

    public record TransactionResponseDto(
        int TransactionId,
        string ReferenceNumber,
        string TransactionTypeName,
        decimal Amount,
        DateTime DateTransaction,
        string? Decription
    );
}
