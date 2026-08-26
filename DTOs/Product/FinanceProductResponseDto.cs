namespace CRMFinaciertoBackend.DTOs.Product
{
    public record FinanceProductResponseDto(
        int ProductId,
        string AreaName,
        string ProductName,
        string? Description,
        decimal TasaInteresOPrimaBase,
        string StatusName
    );
}
