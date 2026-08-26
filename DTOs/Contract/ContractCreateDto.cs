using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Contract
{
    public record BankContractCrateDto(
        [Required] int ClientId,
        [Required] int ProductId,
        [Required] int UserId,
        [Required, StringLength(50)] string ReferenceNumber,
        [Required, RequiredDate] DateTime DateOpeningIssue,
        [RequiredDate] DateTime? DateEnd,
        [Required] int ContractStatusId,
        [StringLength(18)] string? InterbankCode,
        [Range(0, DecimalPrecision.Money)] decimal BalanceActual,
        [Range(0, DecimalPrecision.Money)] decimal LoanAmountGranted,
        [Required, Range(0, DecimalPrecision.Rate)] decimal AgreedInterestRate,
        [Range(1, 28)] int MonthlyCutoffDay = 1
    );

    public record InsuranceContranctCreateDto(
        [Required] int ClientId,
        [Required] int ProductId,
        [Required] int UserId,
        [Required, StringLength(50)] string ReferenceNumber,
        [Required, RequiredDate] DateTime DateOpeningIssue,
        [RequiredDate] DateTime? DateEnd,
        [Required] int ContractStatusId,
        [Required, Range(0, DecimalPrecision.Money)] decimal InsuranceAmountGranted,
        [Required, Range(0, DecimalPrecision.Money)] decimal TotalAnnualPremium,
        [Required] int PayFormId,
        [Range(0, DecimalPrecision.Percent)] decimal PorcentDeductible,
        string? BeneficiaryName
    );
}
