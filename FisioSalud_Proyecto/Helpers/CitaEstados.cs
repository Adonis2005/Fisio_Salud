namespace FisioSalud_Proyecto.Helpers
{
    public static class CitaEstados
    {
        public const string Programada = "PROGRAMADA";
        public const string Atendida = "ATENDIDA";
        public const string Cancelada = "CANCELADA";

        public static string Etiqueta(string estado)
        {
            switch ((estado ?? string.Empty).ToUpperInvariant())
            {
                case Atendida: return "Completada";
                case Cancelada: return "Cancelada";
                default: return "Programada";
            }
        }

        public static string Css(string estado)
        {
            switch ((estado ?? string.Empty).ToUpperInvariant())
            {
                case Atendida: return "ok";
                case Cancelada: return "warn";
                default: return "info";
            }
        }
    }
}
