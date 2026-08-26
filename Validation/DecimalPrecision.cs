namespace CRMFinaciertoBackend.Validation
{
    /// <summary>
    /// Topes que impone la precision real de las columnas de la BD. Sin ellos un importe
    /// fuera de rango no falla en la validacion del modelo sino al ejecutar el INSERT, y el
    /// cliente recibe un 500 en vez de un 400 (verificado contra <c>crm_finance</c>).
    /// </summary>
    public static class DecimalPrecision
    {
        /// <summary>Importes: <c>decimal(15,2)</c>.</summary>
        public const double Money = 9_999_999_999_999.99;

        /// <summary>Tasas de interes y primas base: <c>decimal(5,2)</c>.</summary>
        public const double Rate = 999.99;

        /// <summary>Deducibles expresados en porcentaje: <c>decimal(4,2)</c>.</summary>
        public const double Percent = 99.99;
    }
}
