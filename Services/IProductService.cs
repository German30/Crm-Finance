using CRMFinaciertoBackend.DTOs.Product;

namespace CRMFinaciertoBackend.Services
{
    public interface IProductService
    {
        Task<IEnumerable<FinanceProductResponseDto>> GetAllProductsAsync(int? areaId, int? statusId);
        Task<FinanceProductResponseDto?> GetProductByIdAsync(int id);
        Task<FinanceProductResponseDto> CreateProductAsync(FinanceProductCreateDto dto);
        Task<FinanceProductResponseDto?> UpdateProductAsync(int id, FinanceProductUpdateDto dto);
        Task<bool> DeleteProductAsync(int id);
    }
}
