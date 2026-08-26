using CRMFinaciertoBackend.DTOs.Contract;

namespace CRMFinaciertoBackend.Services
{
    public interface IContractService
    {
        Task<IEnumerable<ContractGridDto>> GetAllContractsAsync(int? areaId, int? clientId, int? userId, int? contractStatusId, string? search);
        Task<object?> GetContractDetailAsync(int id);
        Task<BankContractDetailDto?> GetBankContractAsync(int id);
        Task<InsuranceContractDetailDto?> GetInsuranceContractAsync(int id);
        Task<BankContractDetailDto> CreateBankContractAsync(BankContractCrateDto dto);
        Task<InsuranceContractDetailDto> CreateInsuranceContractAsync(InsuranceContranctCreateDto dto);
        Task<BankContractDetailDto?> UpdateBankContractAsync(int id, BankContractUpdateDto dto);
        Task<InsuranceContractDetailDto?> UpdateInsuranceContractAsync(int id, InsuranceContractUpdateDto dto);
        Task<bool> ChangeContractStatusAsync(int id, int contractStatusId);
        Task<bool> DeleteContractAsync(int id);
    }
}
