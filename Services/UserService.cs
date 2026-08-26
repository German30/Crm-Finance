using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.Models;
using CRMFinaciertoBackend.DTOs.User;

namespace CRMFinaciertoBackend.Services
{
    public class UserService : IUserService
    {
        private const int StatusActivo = 1;
        private const int StatusInactivo = 2;

        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            return await _context.Users
                .Select(u => new UserResponseDto(
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.RoleId,
                    u.Role!.RoleName,
                    u.Role.Area!.AreaName,
                    u.Status!.StatusName,
                    u.CreationDate
                ))
                .ToListAsync();
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new UserResponseDto(
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.RoleId,
                    u.Role!.RoleName,
                    u.Role.Area!.AreaName,
                    u.Status!.StatusName,
                    u.CreationDate
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<UserResponseDto> CreateUserAsync(UserCreateDto dto)
        {
            await ValidateRoleAsync(dto.RoleId);
            await ValidateEmailIsFreeAsync(dto.Email, null);

            var newUser = new User
            {
                RoleId = dto.RoleId,
                StatusId = StatusActivo,
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = _passwordHasher.Hash(dto.Password),
                CreationDate = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return (await GetUserByIdAsync(newUser.UserId))!;
        }

        public async Task<UserResponseDto?> UpdateUserAsync(int id, UserUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            await ValidateRoleAsync(dto.RoleId);
            await ValidateStatusAsync(dto.StatusId);
            await ValidateEmailIsFreeAsync(dto.Email, id);

            user.Name = dto.Name;
            user.Email = dto.Email;
            user.StatusId = dto.StatusId;
            user.RoleId = dto.RoleId;

            await _context.SaveChangesAsync();
            return await GetUserByIdAsync(id);
        }

        public async Task<IEnumerable<RoleResponseDto>> GetRoles()
        {
            return await _context.Roles
                .Select(r => new RoleResponseDto
                (
                     r.RoleId,
                     r.AreaId,
                     r.RoleName,
                     r.Category,
                     r.Description
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<AreaResponseDto>> GetAreas()
        {
            return await _context.Areas
                .Select(a => new AreaResponseDto(
                    a.AreaId,
                    a.AreaName,
                    a.Description
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<UserStatusResponseDto>> GetStatuses()
        {
            return await _context.UserStatuses
                .Select(s => new UserStatusResponseDto(
                    s.StatusId,
                    s.StatusName
                ))
                .ToListAsync();
        }

        public async Task<bool> ToggleUserStatusAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.StatusId = user.StatusId == StatusActivo ? StatusInactivo : StatusActivo;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            user.PasswordHash = _passwordHasher.Hash(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ValidateRoleAsync(int roleId)
        {
            if (!await _context.Roles.AnyAsync(r => r.RoleId == roleId))
                throw new InvalidOperationException($"El rol {roleId} no existe.");
        }

        private async Task ValidateStatusAsync(int statusId)
        {
            if (!await _context.UserStatuses.AnyAsync(s => s.StatusId == statusId))
                throw new InvalidOperationException($"El estado {statusId} no existe.");
        }

        private async Task ValidateEmailIsFreeAsync(string email, int? excludeUserId)
        {
            var taken = await _context.Users
                .AnyAsync(u => u.Email == email && (excludeUserId == null || u.UserId != excludeUserId));

            if (taken)
                throw new InvalidOperationException($"El correo {email} ya esta registrado.");
        }
    }
}
