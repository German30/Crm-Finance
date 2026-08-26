using CRMFinaciertoBackend.DTOs.Oportunity;

namespace CRMFinaciertoBackend.Services
{
    public interface IOportunityService
    {
        Task<IEnumerable<OportunityResponseDto>> GetAllOportunitiesAsync(int? clientId, int? userId, int? stageId, int? areaId);
        Task<OportunityResponseDto?> GetOportunityByIdAsync(int id);
        Task<OportunityResponseDto> CreateOportunityAsync(OportunityCreateDto dto);
        Task<OportunityResponseDto?> UpdateOportunityAsync(int id, OportunityUpdateDto dto);
        Task<OportunityResponseDto?> ChangeStageAsync(int id, OportunityStageUpdateDto dto);
        Task<bool> DeleteOportunityAsync(int id);
    }
}
