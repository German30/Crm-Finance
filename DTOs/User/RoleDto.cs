namespace CRMFinaciertoBackend.DTOs.User
{
    public record RoleResponseDto(
            int RoleId,
            int AreaId,
            string RoleName,
            string Category,
            string? Description
    );
}
