namespace FisioSalud_Proyecto.Helpers
{
    public static class CitaEstados
    {
        public const string Solicitada = "SOLICITADA";
        public const string PendientePago = "PENDIENTE_PAGO";
        public const string Confirmada = "CONFIRMADA";
        public const string EnAtencion = "EN_ATENCION";
        public const string Atendida = "ATENDIDA";
        public const string Cancelada = "CANCELADA";
        public const string NoAsistio = "NO_ASISTIO";

        // Compatibilidad con código anterior
        public const string Programada = "CONFIRMADA";

        public static string Etiqueta(string estado)
        {
            switch ((estado ?? string.Empty).ToUpperInvariant())
            {
                case Solicitada: return "Solicitada";
                case PendientePago: return "Pendiente de Pago";
                case Confirmada: return "Confirmada";
                case EnAtencion: return "En Atención";
                case Atendida: return "Atendida";
                case Cancelada: return "Cancelada";
                case NoAsistio: return "No Asistió";
                default: return estado ?? "Solicitada";
            }
        }

        public static string Css(string estado)
        {
            switch ((estado ?? string.Empty).ToUpperInvariant())
            {
                case Solicitada: return "warning";
                case PendientePago: return "warning";
                case Confirmada: return "info";
                case EnAtencion: return "primary";
                case Atendida: return "success";
                case Cancelada: return "danger";
                case NoAsistio: return "secondary";
                default: return "info";
            }
        }
    }
}

