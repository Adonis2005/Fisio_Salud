namespace FisioSalud_Proyecto.Helpers
{
    public static class PagoEstados
    {
        public const string Pendiente = "PENDIENTE";
        public const string Pagado = "PAGADO";
        public const string Cancelado = "CANCELADO";
        public const string Reembolsado = "REEMBOLSADO";

        public static string Etiqueta(string estado)
        {
            switch ((estado ?? string.Empty).ToUpperInvariant())
            {
                case Pendiente: return "Pendiente";
                case Pagado: return "Pagado";
                case Cancelado: return "Cancelado";
                case Reembolsado: return "Reembolsado";
                default: return estado ?? "Pendiente";
            }
        }

        public static string Css(string estado)
        {
            switch ((estado ?? string.Empty).ToUpperInvariant())
            {
                case Pendiente: return "warning";
                case Pagado: return "success";
                case Cancelado: return "danger";
                case Reembolsado: return "info";
                default: return "secondary";
            }
        }
    }
}
