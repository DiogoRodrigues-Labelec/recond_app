namespace Recondicionamento_DTC_Routers.Domain
{
    public sealed class RouterRecord
    {
        public string Fabricante { get; set; } = "";
        public string NumeroSerie { get; set; } = "";
        public string FirmwareOld { get; set; } = "";
        public string FirmwareNew { get; set; } = "";

        public bool InspecaoVisual { get; set; }
        public bool Liga230V { get; set; }

        public bool ConfigUploaded { get; set; }

        public int Rs232Score { get; set; } // ex.: 0..2
        public bool Rs485Ok { get; set; }

        public int NumeroPortasEth { get; set; }
        public bool[] EthOk { get; set; } = new bool[8];

        public bool Antena { get; set; }
        public bool CaboAlimentacao { get; set; }
        public bool CaboRS232 { get; set; }
        public bool CaboRS485 { get; set; }

        public string Comentario { get; set; } = "";

        public bool ConformidadeFinal { get; set; }
    }


}
