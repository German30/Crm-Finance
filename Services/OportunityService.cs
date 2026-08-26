using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.Models;
using CRMFinaciertoBackend.DTOs.Oportunity;

namespace CRMFinaciertoBackend.Services
{
    public class OportunityService : IOportunityService
    {
        private readonly ApplicationDbContext _context;

        public OportunityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OportunityResponseDto>> GetAllOportunitiesAsync(
            int? clientId, int? userId, int? stageId, int? areaId)
        {
            var query = _context.ComercialOportunity.AsQueryable();

            if (clientId.HasValue)
                query = query.Where(o => o.ClientId == clientId.Value);

            if (userId.HasValue)
                query = query.Where(o => o.UserId == userId.Value);

            if (stageId.HasValue)
                query = query.Where(o => o.StageId == stageId.Value);

            if (areaId.HasValue)
                query = query.Where(o => o.FinanceProduct!.AreaId == areaId.Value);

            return await query
                .OrderByDescending(o => o.OportunotyId)
                .Select(o => new OportunityResponseDto(
                    o.OportunotyId,
                    o.ClientId,
                    o.Client!.PhisicPerson != null
                        ? o.Client.PhisicPerson.Names + " " + o.Client.PhisicPerson.FatherLastName + " " + o.Client.PhisicPerson.MotherLastName
                        : (o.Client.MoralPerson != null ? o.Client.MoralPerson.SocialRazon : "Sin nombre"),
                    o.FinanceProduct!.ProductNamme,
                    o.FinanceProduct.Area!.AreaName,
                    o.User!.Name,
                    o.EstimatedMont,
                    o.Stage!.StageName!,
                    o.SuccesProbability,
                    o.DateEstimatedClose,
                    o.DateRegister
                ))
                .ToListAsync();
        }

        public async Task<OportunityResponseDto?> GetOportunityByIdAsync(int id)
        {
            return await _context.ComercialOportunity
                .Where(o => o.OportunotyId == id)
                .Select(o => new OportunityResponseDto(
                    o.OportunotyId,
                    o.ClientId,
                    o.Client!.PhisicPerson != null
                        ? o.Client.PhisicPerson.Names + " " + o.Client.PhisicPerson.FatherLastName + " " + o.Client.PhisicPerson.MotherLastName
                        : (o.Client.MoralPerson != null ? o.Client.MoralPerson.SocialRazon : "Sin nombre"),
                    o.FinanceProduct!.ProductNamme,
                    o.FinanceProduct.Area!.AreaName,
                    o.User!.Name,
                    o.EstimatedMont,
                    o.Stage!.StageName!,
                    o.SuccesProbability,
                    o.DateEstimatedClose,
                    o.DateRegister
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<OportunityResponseDto> CreateOportunityAsync(OportunityCreateDto dto)
        {
            await ValidateClientAsync(dto.ClientId);
            await ValidateProductAsync(dto.ProductId);
            await ValidateUserAsync(dto.UserId);
            await ValidateStageAsync(dto.StageId);
            ValidateProbability(dto.SuccessProbability);
            ValidateEstimatedMont(dto.EstimatedMont);

            var oportunity = new ComercialOportunity
            {
                ClientId = dto.ClientId,
                ProductId = dto.ProductId,
                UserId = dto.UserId,
                EstimatedMont = dto.EstimatedMont,
                StageId = dto.StageId,
                SuccesProbability = dto.SuccessProbability,
                DateEstimatedClose = dto.DateEstimatedClose,
                DateRegister = DateTime.UtcNow
            };

            _context.ComercialOportunity.Add(oportunity);
            await _context.SaveChangesAsync();

            return (await GetOportunityByIdAsync(oportunity.OportunotyId))!;
        }

        public async Task<OportunityResponseDto?> UpdateOportunityAsync(int id, OportunityUpdateDto dto)
        {
            var oportunity = await _context.ComercialOportunity.FindAsync(id);
            if (oportunity == null) return null;

            await ValidateUserAsync(dto.UserId);
            await ValidateStageAsync(dto.StageId);
            ValidateProbability(dto.SuccessProbability);
            ValidateEstimatedMont(dto.EstimatedMont);

            oportunity.UserId = dto.UserId;
            oportunity.EstimatedMont = dto.EstimatedMont;
            oportunity.StageId = dto.StageId;
            oportunity.SuccesProbability = dto.SuccessProbability;
            oportunity.DateEstimatedClose = dto.DateEstimatedClose;

            await _context.SaveChangesAsync();
            return await GetOportunityByIdAsync(id);
        }

        public async Task<OportunityResponseDto?> ChangeStageAsync(int id, OportunityStageUpdateDto dto)
        {
            var oportunity = await _context.ComercialOportunity.FindAsync(id);
            if (oportunity == null) return null;

            await ValidateStageAsync(dto.StageId);

            oportunity.StageId = dto.StageId;

            if (dto.SuccessProbability.HasValue)
            {
                ValidateProbability(dto.SuccessProbability.Value);
                oportunity.SuccesProbability = dto.SuccessProbability.Value;
            }

            await _context.SaveChangesAsync();
            return await GetOportunityByIdAsync(id);
        }

        public async Task<bool> DeleteOportunityAsync(int id)
        {
            var oportunity = await _context.ComercialOportunity.FindAsync(id);
            if (oportunity == null) return false;

            _context.ComercialOportunity.Remove(oportunity);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ValidateClientAsync(int clientId)
        {
            if (!await _context.Clients.AnyAsync(c => c.ClientId == clientId))
                throw new InvalidOperationException($"El cliente {clientId} no existe.");
        }

        private async Task ValidateProductAsync(int productId)
        {
            if (!await _context.FinanceProducts.AnyAsync(p => p.ProductId == productId))
                throw new InvalidOperationException($"El producto {productId} no existe.");
        }

        private async Task ValidateUserAsync(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId))
                throw new InvalidOperationException($"El usuario {userId} no existe.");
        }

        private async Task ValidateStageAsync(int stageId)
        {
            if (!await _context.Stage.AnyAsync(s => s.StageId == stageId))
                throw new InvalidOperationException($"La etapa {stageId} no existe.");
        }

        private static void ValidateProbability(int successProbability)
        {
            if (successProbability < 0 || successProbability > 100)
                throw new InvalidOperationException("La probabilidad de exito debe estar entre 0 y 100.");
        }

        private static void ValidateEstimatedMont(decimal? estimatedMont)
        {
            if (estimatedMont.HasValue && estimatedMont.Value < 0)
                throw new InvalidOperationException("El monto estimado no puede ser negativo.");
        }
    }
}
