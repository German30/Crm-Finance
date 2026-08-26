using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.Models;
using CRMFinaciertoBackend.DTOs.Client;

namespace CRMFinaciertoBackend.Services
{
    public class ClientService : IClientService
    {
        // Ids del catalogo type_person. Se validan contra la BD antes de usarse porque el
        // esquema se administra fuera del proyecto (no hay migraciones).
        public const int TypePersonFisica = 1;
        public const int TypePersonMoral = 2;

        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClientGridDto>> GetAllClientsAsync(string? search, int? typePersonId, int? assignedUserId)
        {
            var query = _context.Clients.AsQueryable();

            if (typePersonId.HasValue)
                query = query.Where(c => c.TypePersonId == typePersonId.Value);

            if (assignedUserId.HasValue)
                query = query.Where(c => c.AssignedUserId == assignedUserId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c =>
                    (c.FiscalId != null && c.FiscalId.Contains(term)) ||
                    (c.Email != null && c.Email.Contains(term)) ||
                    (c.PhisicPerson != null && (
                        c.PhisicPerson.Names.Contains(term) ||
                        c.PhisicPerson.FatherLastName.Contains(term) ||
                        c.PhisicPerson.MotherLastName.Contains(term))) ||
                    (c.MoralPerson != null && (
                        c.MoralPerson.SocialRazon.Contains(term) ||
                        (c.MoralPerson.ComercialName != null && c.MoralPerson.ComercialName.Contains(term)))));
            }

            return await query
                .OrderBy(c => c.ClientId)
                .Select(c => new ClientGridDto(
                    c.ClientId,
                    c.TypePerson!.TypeName,
                    c.FiscalId,
                    c.PhisicPerson != null
                        ? c.PhisicPerson.Names + " " + c.PhisicPerson.FatherLastName + " " + c.PhisicPerson.MotherLastName
                        : (c.MoralPerson != null ? c.MoralPerson.SocialRazon : "Sin nombre"),
                    c.Email,
                    c.Phone,
                    c.AssignedUser != null ? c.AssignedUser.Name : null
                ))
                .ToListAsync();
        }

        public async Task<object?> GetClientDetailAsync(int id)
        {
            // Se prueba una proyeccion y luego la otra en vez de sondear con AnyAsync primero:
            // son 1-2 consultas en vez de las 2-4 que costaba comprobar-y-luego-leer.
            return await GetPhisicClientAsync(id)
                ?? (object?)await GetMoralClientAsync(id);
        }

        public async Task<PhisicClientDetailDto?> GetPhisicClientAsync(int id)
        {
            return await _context.PhisicPersonClient
                .Where(p => p.ClientId == id)
                .Select(p => new PhisicClientDetailDto(
                    p.ClientId,
                    p.Client!.FiscalId,
                    p.Client.Email,
                    p.Client.Phone,
                    p.Client.AddressFiscal,
                    p.Client.RegisterDate,
                    p.Client.AssignedUser != null ? p.Client.AssignedUser.Name : null,
                    p.Names,
                    p.FatherLastName,
                    p.MotherLastName,
                    p.BirthDate,
                    p.Gender!.GenderName!,
                    p.CiviState!.CivilStateName!
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<MoralClientDetailDto?> GetMoralClientAsync(int id)
        {
            return await _context.MoralPersonClient
                .Where(m => m.ClientId == id)
                .Select(m => new MoralClientDetailDto(
                    m.ClientId,
                    m.Client!.FiscalId,
                    m.Client.Email,
                    m.Client.Phone,
                    m.Client.AddressFiscal,
                    m.Client.RegisterDate,
                    m.Client.AssignedUser != null ? m.Client.AssignedUser.Name : null,
                    m.SocialRazon,
                    m.ComercialName,
                    m.DateConstitucion,
                    m.ComercialActivity,
                    m.RepreentativeLegalName,
                    m.RepresentativeId
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<PhisicClientDetailDto> CreatePhisicClientAsync(PhisicClientCreateDto dto)
        {
            await ValidateTypePersonAsync(TypePersonFisica);
            await ValidateFiscalIdIsFreeAsync(dto.FiscalId, null);
            await ValidateAssignedUserAsync(dto.AssignedUserId);
            await ValidateGenderAsync(dto.GenderId);
            await ValidateCivilStateAsync(dto.CivilStateId);

            var client = new Client
            {
                TypePersonId = TypePersonFisica,
                FiscalId = dto.FiscalId,
                Email = dto.Email,
                Phone = dto.Phone,
                AddressFiscal = dto.AddressFiscal,
                AssignedUserId = dto.AssignedUserId,
                RegisterDate = DateTime.UtcNow,
                PhisicPerson = new PhisicPersonClient
                {
                    Names = dto.Name,
                    FatherLastName = dto.FatherLastName,
                    MotherLastName = dto.MotherLastName,
                    BirthDate = dto.BirthDate,
                    GenderId = dto.GenderId,
                    CivilStateId = dto.CivilStateId
                }
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return (await GetPhisicClientAsync(client.ClientId))!;
        }

        public async Task<MoralClientDetailDto> CreateMoralClientAsync(MoralClientCreateDto dto)
        {
            await ValidateTypePersonAsync(TypePersonMoral);
            await ValidateFiscalIdIsFreeAsync(dto.FiscalId, null);
            await ValidateAssignedUserAsync(dto.AssignedUserId);

            var client = new Client
            {
                TypePersonId = TypePersonMoral,
                FiscalId = dto.FiscalId,
                Email = dto.Email,
                AddressFiscal = dto.AddressFiscal,
                AssignedUserId = dto.AssignedUserId,
                RegisterDate = DateTime.UtcNow,
                MoralPerson = new MoralPersonClient
                {
                    SocialRazon = dto.SocialRazon,
                    ComercialName = dto.ComercialName,
                    DateConstitucion = dto.DateConstitucion,
                    ComercialActivity = dto.ComercialActivity,
                    RepreentativeLegalName = dto.RepresentativeLegalName,
                    RepresentativeId = dto.RepresentativeId
                }
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return (await GetMoralClientAsync(client.ClientId))!;
        }

        public async Task<PhisicClientDetailDto?> UpdatePhisicClientAsync(int id, PhisicClientUpdateDto dto)
        {
            var person = await _context.PhisicPersonClient
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.ClientId == id);

            if (person?.Client == null) return null;

            await ValidateFiscalIdIsFreeAsync(dto.FiscalId, id);
            await ValidateAssignedUserAsync(dto.AssignedUserId);
            await ValidateGenderAsync(dto.GenderId);
            await ValidateCivilStateAsync(dto.CivilStateId);

            person.Client.FiscalId = dto.FiscalId;
            person.Client.Email = dto.Email;
            person.Client.Phone = dto.Phone;
            person.Client.AddressFiscal = dto.AddressFiscal;
            person.Client.AssignedUserId = dto.AssignedUserId;

            person.Names = dto.Name;
            person.FatherLastName = dto.FatherLastName;
            person.MotherLastName = dto.MotherLastName;
            person.BirthDate = dto.BirthDate;
            person.GenderId = dto.GenderId;
            person.CivilStateId = dto.CivilStateId;

            await _context.SaveChangesAsync();
            return await GetPhisicClientAsync(id);
        }

        public async Task<MoralClientDetailDto?> UpdateMoralClientAsync(int id, MoralClientUpdateDto dto)
        {
            var person = await _context.MoralPersonClient
                .Include(m => m.Client)
                .FirstOrDefaultAsync(m => m.ClientId == id);

            if (person?.Client == null) return null;

            await ValidateFiscalIdIsFreeAsync(dto.FiscalId, id);
            await ValidateAssignedUserAsync(dto.AssignedUserId);

            person.Client.FiscalId = dto.FiscalId;
            person.Client.Email = dto.Email;
            person.Client.Phone = dto.Phone;
            person.Client.AddressFiscal = dto.AddressFiscal;
            person.Client.AssignedUserId = dto.AssignedUserId;

            person.SocialRazon = dto.SocialRazon;
            person.ComercialName = dto.ComercialName;
            person.DateConstitucion = dto.DateConstitucion;
            person.ComercialActivity = dto.ComercialActivity;
            person.RepreentativeLegalName = dto.RepresentativeLegalName;
            person.RepresentativeId = dto.RepresentativeId;

            await _context.SaveChangesAsync();
            return await GetMoralClientAsync(id);
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return false;

            // Las FK horizontales son Restrict: hay que avisar en vez de dejar que MySQL
            // devuelva un error de clave foranea.
            if (await _context.ProductContract.AnyAsync(p => p.ClientId == id))
                throw new InvalidOperationException("El cliente tiene contratos asociados y no puede eliminarse.");

            if (await _context.ComercialOportunity.AnyAsync(o => o.ClientId == id))
                throw new InvalidOperationException("El cliente tiene oportunidades comerciales asociadas y no puede eliminarse.");

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ValidateTypePersonAsync(int typePersonId)
        {
            if (!await _context.TypePerson.AnyAsync(t => t.TypePersonId == typePersonId))
                throw new InvalidOperationException($"El tipo de persona {typePersonId} no existe en el catalogo.");
        }

        private async Task ValidateGenderAsync(int genderId)
        {
            if (!await _context.Gender.AnyAsync(g => g.GenderId == genderId))
                throw new InvalidOperationException($"El genero {genderId} no existe.");
        }

        private async Task ValidateCivilStateAsync(int civilStateId)
        {
            if (!await _context.CivilState.AnyAsync(c => c.CivilStateId == civilStateId))
                throw new InvalidOperationException($"El estado civil {civilStateId} no existe.");
        }

        private async Task ValidateAssignedUserAsync(int? assignedUserId)
        {
            if (assignedUserId == null) return;

            if (!await _context.Users.AnyAsync(u => u.UserId == assignedUserId))
                throw new InvalidOperationException($"El usuario asignado {assignedUserId} no existe.");
        }

        private async Task ValidateFiscalIdIsFreeAsync(string fiscalId, int? excludeClientId)
        {
            var taken = await _context.Clients
                .AnyAsync(c => c.FiscalId == fiscalId && (excludeClientId == null || c.ClientId != excludeClientId));

            if (taken)
                throw new InvalidOperationException($"El identificador fiscal {fiscalId} ya esta registrado.");
        }
    }
}
