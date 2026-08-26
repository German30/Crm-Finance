namespace CRMFinaciertoBackend.DTOs.Contract
{
    public record ContractGridDto(
        int ContractId,
        string ReferenceNumber,
        string ClientName,
        string ProductName,
        string AreaName,
        string ContractStatusName,
        DateTime DateOpeningIssue
    );

    public record BankContractDetailDto(
        int ContractId,
        string ReferenceNumber,
        string ClientName,
        string ProductName,
        string ContractStatusName,
        DateTime DateOpeningIssue,
        DateTime? DateEnd,
        string? InterbankCode,
        decimal BalanceActual,
        decimal LoanAmountGranted,
        decimal AgreedInterestRate,
        int MonthlyCotoffDay
    );

    public record InsuranceContractDetailDto(
        int ContractId,
        string ReferenceNumber,
        string ClientName,
        string ProductName,
        string ContractStatusName,
        DateTime DateOpeningIssue,
        DateTime? DateEnd,
        decimal InsuranceSumeTotal,
        decimal TotalAnnualPremium,
        string PayFromName,
        decimal PorcentDeductible,
        string? BeneficiaryName
    );
}
