using CRMFinaciertoBackend.DTOs.Operation;

namespace CRMFinaciertoBackend.Services
{
    public interface IOperationService
    {
        // Banca
        Task<IEnumerable<TransactionResponseDto>> GetTransactionsByContractAsync(int contractId);
        Task<TransactionResponseDto?> GetTransactionByIdAsync(int transactionId);
        Task<TransactionResponseDto> CreateTransactionAsync(TransactionCreateDto dto);
        Task<bool> DeleteTransactionAsync(int transactionId);

        // Seguros
        Task<IEnumerable<InsuranceClaiResponseDto>> GetClaimsByContractAsync(int contractId);
        Task<InsuranceClaiResponseDto?> GetClaimByIdAsync(int insuranceId);
        Task<InsuranceClaiResponseDto> CreateClaimAsync(InsuranceClaimCreateDto dto);
        Task<InsuranceClaiResponseDto?> UpdateClaimAsync(int insuranceId, InsuranceClaimUpdateDto dto);
        Task<bool> DeleteClaimAsync(int insuranceId);
    }
}
