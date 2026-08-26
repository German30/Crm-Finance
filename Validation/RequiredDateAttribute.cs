using System.ComponentModel.DataAnnotations;

namespace CRMFinaciertoBackend.Validation
{
    /// <summary>
    /// Exige una fecha real en una propiedad <see cref="DateTime"/>.
    /// <para>
    /// Hace falta porque <see cref="RequiredAttribute"/> NO detecta un campo ausente cuando el
    /// tipo es un <c>DateTime</c> no anulable: el binder deja <c>default(DateTime)</c>
    /// (0001-01-01), <c>Required</c> lo da por valido y la fila termina guardada con esa fecha
    /// basura (las columnas de negocio son <c>date</c>, que si admite el año 1).
    /// </para>
    /// <para>
    /// El minimo por defecto es 1753 para que la misma validacion sirva tanto en columnas
    /// <c>date</c> como <c>datetime</c> (esta ultima no admite fechas anteriores).
    /// </para>
    /// Los valores nulos se aceptan: en un <c>DateTime?</c> opcional la ausencia es legitima,
    /// y para exigir presencia se combina con <see cref="RequiredAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class RequiredDateAttribute : ValidationAttribute
    {
        public int MinYear { get; set; } = 1753;

        public int MaxYear { get; set; } = 9999;

        public override bool IsValid(object? value)
        {
            if (value == null) return true;

            if (value is not DateTime date) return false;

            return date.Year >= MinYear && date.Year <= MaxYear;
        }

        public override string FormatErrorMessage(string name)
        {
            return ErrorMessage
                ?? $"El campo {name} necesita una fecha valida entre {MinYear} y {MaxYear}.";
        }
    }
}
