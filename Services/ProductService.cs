using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.Models;
using CRMFinaciertoBackend.DTOs.Product;

namespace CRMFinaciertoBackend.Services
{
    public class ProductService : IProductService
    {
        // finace_status_product: 1 = activo (mismo valor por defecto que la entidad).
        private const int StatusActivoPorDefecto = 1;

        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FinanceProductResponseDto>> GetAllProductsAsync(int? areaId, int? statusId)
        {
            var query = _context.FinanceProducts.AsQueryable();

            if (areaId.HasValue)
                query = query.Where(p => p.AreaId == areaId.Value);

            if (statusId.HasValue)
                query = query.Where(p => p.FinanceStatusProductId == statusId.Value);

            return await query
                .OrderBy(p => p.ProductId)
                .Select(p => new FinanceProductResponseDto(
                    p.ProductId,
                    p.Area!.AreaName,
                    p.ProductNamme,
                    p.Description,
                    p.TasaInteresOPrimaBase,
                    p.FinanceStatusProduct != null
                        ? p.FinanceStatusProduct.FinanceStatusProductName!
                        : "Sin estado"
                ))
                .ToListAsync();
        }

        public async Task<FinanceProductResponseDto?> GetProductByIdAsync(int id)
        {
            return await _context.FinanceProducts
                .Where(p => p.ProductId == id)
                .Select(p => new FinanceProductResponseDto(
                    p.ProductId,
                    p.Area!.AreaName,
                    p.ProductNamme,
                    p.Description,
                    p.TasaInteresOPrimaBase,
                    p.FinanceStatusProduct != null
                        ? p.FinanceStatusProduct.FinanceStatusProductName!
                        : "Sin estado"
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<FinanceProductResponseDto> CreateProductAsync(FinanceProductCreateDto dto)
        {
            await ValidateAreaAsync(dto.AreaId);
            await ValidateStatusAsync(dto.FinanceStatusProductId ?? StatusActivoPorDefecto);
            await ValidateNameIsFreeAsync(dto.ProductName, dto.AreaId, null);

            var product = new FinanceProduct
            {
                AreaId = dto.AreaId,
                ProductNamme = dto.ProductName,
                Description = dto.Description,
                TasaInteresOPrimaBase = dto.TasaInteresOPrimaBase,
                FinanceStatusProductId = dto.FinanceStatusProductId ?? StatusActivoPorDefecto,
                DateCreation = DateTime.UtcNow
            };

            _context.FinanceProducts.Add(product);
            await _context.SaveChangesAsync();

            return (await GetProductByIdAsync(product.ProductId))!;
        }

        public async Task<FinanceProductResponseDto?> UpdateProductAsync(int id, FinanceProductUpdateDto dto)
        {
            var product = await _context.FinanceProducts.FindAsync(id);
            if (product == null) return null;

            await ValidateAreaAsync(dto.AreaId);
            await ValidateStatusAsync(dto.FinanceStatusProductId);
            await ValidateNameIsFreeAsync(dto.ProductName, dto.AreaId, id);

            product.AreaId = dto.AreaId;
            product.ProductNamme = dto.ProductName;
            product.Description = dto.Description;
            product.TasaInteresOPrimaBase = dto.TasaInteresOPrimaBase;
            product.FinanceStatusProductId = dto.FinanceStatusProductId;

            await _context.SaveChangesAsync();
            return await GetProductByIdAsync(id);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.FinanceProducts.FindAsync(id);
            if (product == null) return false;

            if (await _context.ProductContract.AnyAsync(c => c.ProductId == id))
                throw new InvalidOperationException("El producto tiene contratos asociados y no puede eliminarse.");

            if (await _context.ComercialOportunity.AnyAsync(o => o.ProductId == id))
                throw new InvalidOperationException("El producto tiene oportunidades comerciales asociadas y no puede eliminarse.");

            _context.FinanceProducts.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ValidateAreaAsync(int areaId)
        {
            if (!await _context.Areas.AnyAsync(a => a.AreaId == areaId))
                throw new InvalidOperationException($"El area {areaId} no existe.");
        }

        private async Task ValidateStatusAsync(int statusId)
        {
            if (!await _context.FinanceStatusProducts.AnyAsync(s => s.FinanceStatusProductId == statusId))
                throw new InvalidOperationException($"El estado de producto {statusId} no existe.");
        }

        private async Task ValidateNameIsFreeAsync(string productName, int areaId, int? excludeProductId)
        {
            var taken = await _context.FinanceProducts
                .AnyAsync(p => p.ProductNamme == productName
                            && p.AreaId == areaId
                            && (excludeProductId == null || p.ProductId != excludeProductId));

            if (taken)
                throw new InvalidOperationException($"Ya existe un producto llamado {productName} en esa area.");
        }
    }
}
