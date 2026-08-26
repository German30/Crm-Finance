using System.ComponentModel.DataAnnotations;

namespace CRMFinaciertoBackend.DTOs.User
{
    public record UserChangePasswordDto(
        [Required] string CurrentPassword,
        [Required, StringLength(100, MinimumLength = 8)] string NewPassword
    );

    public record UserResetPasswordDto(
        [Required, StringLength(100, MinimumLength = 8)] string NewPassword
    );

    public record CurrentUserDto(
        int UserId,
        string Name,
        string Email,
        string RoleName,
        string AreaName
    );
}
