using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRMFinaciertoBackend.DTOs.Product;
using CRMFinaciertoBackend.Services;

namespace CRMFinaciertoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FinanceProductResponseDto>>> GetProducts(
            [FromQuery] int? areaId,
            [FromQuery] int? statusId)
        {
            var products = await _productService.GetAllProductsAsync(areaId, statusId);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FinanceProductResponseDto>> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound(new { Message = "Producto no encontrado" });
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<ActionResult<FinanceProductResponseDto>> CreateProduct([FromBody] FinanceProductCreateDto dto)
        {
            try
            {
                var created = await _productService.CreateProductAsync(dto);
                return CreatedAtAction(nameof(GetProduct), new { id = created.ProductId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<ActionResult<FinanceProductResponseDto>> UpdateProduct(int id, [FromBody] FinanceProductUpdateDto dto)
        {
            try
            {
                var updated = await _productService.UpdateProductAsync(id, dto);
                if (updated == null) return NotFound(new { Message = "Producto no encontrado" });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequiereAdministrador")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var deleted = await _productService.DeleteProductAsync(id);
                if (!deleted) return NotFound(new { Message = "Producto no encontrado" });
                return Ok(new { Message = "Producto eliminado correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
