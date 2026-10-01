namespace KaliteWeb.UI.Models.ProductionLots
{
    public class CreateProductionLotViewModel
    {
        public int ProductionId { get; set; }

        public string? ProductionNo { get; set; } = "";

        public string MainLotNo { get; set; } = "";

        public bool ManualSubLotNo { get; set; }

        public string? SubLotNo { get; set; }

        public decimal Quantity { get; set; }

        public int? DepoId { get; set; }

        public string? Description { get; set; }

        public decimal? ProductTemperature { get; set; }

        // ==========================================
        // DEPO LİSTESİ
        // ==========================================

        public List<DepoSelectItemViewModel> Depolar { get; set; }
            = new();
    }

    public class DepoSelectItemViewModel
    {
        public int Id { get; set; }

        public string DepoAdi { get; set; } = "";
    }
}
