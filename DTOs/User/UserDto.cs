using System.ComponentModel.DataAnnotations;

namespace CRMFinaciertoBackend.DTOs.User
{
    public record UserCreateDto(
            [Required] int RoleId,
            [Required, StringLength(100)] string Name,
            [Required, EmailAddress, StringLength(100)] string Email,
            [Required, StringLength(100, MinimumLength = 8)] string Password
    );

    public record UserUpdateDto(
        [Required, StringLength(100)] string Name,
        [Required, EmailAddress, StringLength(100)] string Email,
        [Required] int StatusId,
        [Required] int RoleId
    );

    public record UserResponseDto(
        int UserId,
        string Name,
        string Email,
        int RoleId,
        string RoleName,
        string AreaName,
        string StatusName,
        DateTime CreationDate
    );
}
