namespace CRMFinaciertoBackend.DTOs.Client
{
    public record ClientGridDto(
            int ClinetId,
            string TypePersonName,
            string? FiscalId,
            string ClientName,
            string? Email,
            string? Phone,
            string? AssignedUserName
        );

    public record PhisicClientDetailDto(
        int ClientId,
        string? FiscalId,
        string? Email,
        string? Phone,
        string? AddressFiscal,
        DateTime RegisterDate,
        string? AssignedUserName,
        string Names,
        string FatherLastName,
        string MotherLastName,
        DateTime BirthDate,
        string GenderName,
        string CivilStateName
    );

    public record MoralClientDetailDto(
        int ClientId,
        string? FiscalId,
        string? Email,
        string? Phone,
        string? AddressFiscal,
        DateTime RegisterDate,
        string? AssignedUserName,
        string SocialRazon,
        string? ComercialName,
        DateTime DateConstitucion,
        string? ComercialActivity,
        string RepresentativeLegalName,
        string? RepresentativeId
    );
}
