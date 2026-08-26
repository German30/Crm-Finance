using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.DTOs.Catalog;
using CRMFinaciertoBackend.DTOs.User;

namespace CRMFinaciertoBackend.Services
{
    /// <summary>
    /// Lectura de los catalogos que alimentan los combos del frontend. Todos son de solo
    /// lectura: el mantenimiento se hace directamente sobre la BD (no hay migraciones).
    /// <para>
    /// El frontend pide varios en cada formulario, asi que las respuestas se cachean en
    /// memoria. Como el mantenimiento ocurre fuera de la API no hay evento de invalidacion:
    /// se usa una caducidad corta (<see cref="CacheDuration"/>) y un alta hecha por SQL tarda
    /// como mucho ese tiempo en verse.
    /// </para>
    /// </summary>
    public class CatalogService : ICatalogService
    {
        public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CatalogService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public Task<IEnumerable<AreaResponseDto>> GetAreasAsync()
            => GetOrLoadAsync("catalog:areas", () => _context.Areas
                .OrderBy(a => a.AreaId)
                .Select(a => new AreaResponseDto(a.AreaId, a.AreaName, a.Description)));

        public Task<IEnumerable<CatalogItemDto>> GetTypePersonsAsync()
            => GetOrLoadAsync("catalog:type-persons", () => _context.TypePerson
                .OrderBy(t => t.TypePersonId)
                .Select(t => new CatalogItemDto(t.TypePersonId, t.TypeName)));

        public Task<IEnumerable<CatalogItemDto>> GetGendersAsync()
            => GetOrLoadAsync("catalog:genders", () => _context.Gender
                .OrderBy(g => g.GenderId)
                .Select(g => new CatalogItemDto(g.GenderId, g.GenderName!)));

        public Task<IEnumerable<CatalogItemDto>> GetCivilStatesAsync()
            => GetOrLoadAsync("catalog:civil-states", () => _context.CivilState
                .OrderBy(c => c.CivilStateId)
                .Select(c => new CatalogItemDto(c.CivilStateId, c.CivilStateName!)));

        public Task<IEnumerable<CatalogItemDto>> GetProductStatusesAsync()
            => GetOrLoadAsync("catalog:product-status", () => _context.FinanceStatusProducts
                .OrderBy(s => s.FinanceStatusProductId)
                .Select(s => new CatalogItemDto(s.FinanceStatusProductId, s.FinanceStatusProductName!)));

        public Task<IEnumerable<CatalogItemDto>> GetContractStatusesAsync()
            => GetOrLoadAsync("catalog:contract-status", () => _context.ContarctStatus
                .OrderBy(s => s.ContractSatusId)
                .Select(s => new CatalogItemDto(s.ContractSatusId, s.ContractStatusName!)));

        public Task<IEnumerable<CatalogItemDto>> GetPayFormsAsync()
            => GetOrLoadAsync("catalog:pay-forms", () => _context.PayForm
                .OrderBy(p => p.PayFormId)
                .Select(p => new CatalogItemDto(p.PayFormId, p.PayFormName!)));

        public Task<IEnumerable<CatalogItemDto>> GetStagesAsync()
            => GetOrLoadAsync("catalog:stages", () => _context.Stage
                .OrderBy(s => s.StageId)
                .Select(s => new CatalogItemDto(s.StageId, s.StageName!)));

        public Task<IEnumerable<CatalogItemDto>> GetTransactionTypesAsync()
            => GetOrLoadAsync("catalog:transaction-types", () => _context.TransactionType
                .OrderBy(t => t.TransactionTypeId)
                .Select(t => new CatalogItemDto(t.TransactionTypeId, t.TransactionTypeName!)));

        public Task<IEnumerable<CatalogItemDto>> GetDisasterStatesAsync()
            => GetOrLoadAsync("catalog:disaster-states", () => _context.DisaterState
                .OrderBy(d => d.DisasterStateId)
                .Select(d => new CatalogItemDto(d.DisasterStateId, d.DisasterStateName!)));

        private async Task<IEnumerable<T>> GetOrLoadAsync<T>(string key, Func<IQueryable<T>> query)
        {
            if (_cache.TryGetValue(key, out IReadOnlyList<T>? cached) && cached != null)
                return cached;

            var items = await query().ToListAsync();
            _cache.Set(key, (IReadOnlyList<T>)items, CacheDuration);
            return items;
        }
    }
}
