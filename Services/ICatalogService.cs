using CRMFinaciertoBackend.DTOs.Catalog;
using CRMFinaciertoBackend.DTOs.User;

namespace CRMFinaciertoBackend.Services
{
    public interface ICatalogService
    {
        Task<IEnumerable<AreaResponseDto>> GetAreasAsync();
        Task<IEnumerable<CatalogItemDto>> GetTypePersonsAsync();
        Task<IEnumerable<CatalogItemDto>> GetGendersAsync();
        Task<IEnumerable<CatalogItemDto>> GetCivilStatesAsync();
        Task<IEnumerable<CatalogItemDto>> GetProductStatusesAsync();
        Task<IEnumerable<CatalogItemDto>> GetContractStatusesAsync();
        Task<IEnumerable<CatalogItemDto>> GetPayFormsAsync();
        Task<IEnumerable<CatalogItemDto>> GetStagesAsync();
        Task<IEnumerable<CatalogItemDto>> GetTransactionTypesAsync();
        Task<IEnumerable<CatalogItemDto>> GetDisasterStatesAsync();
    }
}
