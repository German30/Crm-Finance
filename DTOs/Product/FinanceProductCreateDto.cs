using System.ComponentModel.DataAnnotations;
using CRMFinaciertoBackend.Validation;

namespace CRMFinaciertoBackend.DTOs.Product
{
    public record FinanceProductCreateDto(
        [Required] int AreaId,
        [Required, StringLength(100)] string ProductName,
        string? Description,
        [Range(0, DecimalPrecision.Rate)] decimal TasaInteresOPrimaBase,
        int? FinanceStatusProductId
    );

    public record FinanceProductUpdateDto(
        [Required] int AreaId,
        [Required, StringLength(100)] string ProductName,
        string? Description,
        [Range(0, DecimalPrecision.Rate)] decimal TasaInteresOPrimaBase,
        [Required] int FinanceStatusProductId
    );
}
