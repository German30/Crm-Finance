using CRMFinaciertoBackend.DTOs.User;

namespace CRMFinaciertoBackend.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(UserCreateDto dto);
        Task<UserResponseDto?> UpdateUserAsync(int id, UserUpdateDto dto);
        Task<IEnumerable<RoleResponseDto>> GetRoles();
        Task<IEnumerable<UserStatusResponseDto>> GetStatuses();
        Task<bool> ToggleUserStatusAsync(int id);
        Task<bool> ResetPasswordAsync(int id, string newPassword);
        Task<IEnumerable<AreaResponseDto>> GetAreas();
    }
}
