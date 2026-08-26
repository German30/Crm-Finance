using CRMFinaciertoBackend.DTOs.Client;

namespace CRMFinaciertoBackend.Services
{
    public interface IClientService
    {
        Task<IEnumerable<ClientGridDto>> GetAllClientsAsync(string? search, int? typePersonId, int? assignedUserId);
        Task<object?> GetClientDetailAsync(int id);
        Task<PhisicClientDetailDto?> GetPhisicClientAsync(int id);
        Task<MoralClientDetailDto?> GetMoralClientAsync(int id);
        Task<PhisicClientDetailDto> CreatePhisicClientAsync(PhisicClientCreateDto dto);
        Task<MoralClientDetailDto> CreateMoralClientAsync(MoralClientCreateDto dto);
        Task<PhisicClientDetailDto?> UpdatePhisicClientAsync(int id, PhisicClientUpdateDto dto);
        Task<MoralClientDetailDto?> UpdateMoralClientAsync(int id, MoralClientUpdateDto dto);
        Task<bool> DeleteClientAsync(int id);
    }
}
