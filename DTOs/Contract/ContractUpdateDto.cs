using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Contract
{
    // BalanceActual no se actualiza aqui: lo determinan las transacciones bancarias
    // registradas contra el contrato (ver OperationService).
    public record BankContractUpdateDto(
        [Required] int UserId,
        [Required, RequiredDate] DateTime DateOpeningIssue,
        [RequiredDate] DateTime? DateEnd,
        [Required] int ContractStatusId,
        [StringLength(18)] string? InterbankCode,
        [Range(0, DecimalPrecision.Money)] decimal LoanAmountGranted,
        [Required, Range(0, DecimalPrecision.Rate)] decimal AgreedInterestRate,
        [Range(1, 28)] int MonthlyCutoffDay = 1
    );

    public record InsuranceContractUpdateDto(
        [Required] int UserId,
        [Required, RequiredDate] DateTime DateOpeningIssue,
        [RequiredDate] DateTime? DateEnd,
        [Required] int ContractStatusId,
        [Required, Range(0, DecimalPrecision.Money)] decimal InsuranceSumeTotal,
        [Required, Range(0, DecimalPrecision.Money)] decimal TotalAnnualPremium,
        [Required] int PayFormId,
        [Range(0, DecimalPrecision.Percent)] decimal PorcentDeductible,
        string? BeneficiaryName
    );

    public record ContractStatusUpdateDto(
        [Required] int ContractStatusId
    );
}
