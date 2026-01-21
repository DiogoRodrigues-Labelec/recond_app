// DtcRecord.cs
// Modelo "compatível" com o DtcWorkflowForm + DtcHtmlReport acima
// (e com o que descreveste: Fabricante, NumeroSerie, FirmwareOld/New, ConfigUploaded, AnalogOk, EmiPlcOk, ConformidadeFinal, etc.)

using System;

namespace Recondicionamento_DTC_Routers.Domain
{
    public sealed class DtcRecord
    {
        // Identificação
        public string Fabricante { get; set; } = "";
        public string NumeroSerie { get; set; } = "";   // usado como ID/Serial no report/form

        // Firmware
        public string FirmwareOld { get; set; } = "";
        public string FirmwareNew { get; set; } = "";

        // Resultados dos passos
        public bool ConfigUploaded { get; set; } = false;
        public bool AnalogOk { get; set; } = false;
        public bool EmiPlcOk { get; set; } = false;

        // Extras úteis (opcional)
        public string Comentario { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Resultado final
        public bool ConformidadeFinal { get; set; } = false;

        // Helper opcional: calcula conformidade a partir dos flags (se quiseres usar no runner)
        public void RecalculateConformidade()
        {
            ConformidadeFinal = ConfigUploaded && AnalogOk && EmiPlcOk;
        }
    }
}
