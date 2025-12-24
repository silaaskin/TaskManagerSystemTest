namespace TaskManagerSystem.Models
{
    // API istatistik endpoint'i için veri taşıma modeli
    public class TaskStatsViewModel
    {
        // Toplam görev sayısı
        public int TotalTasks { get; set; }
        
        // Tamamlanan görev sayısı (Status = 2)
        public int CompletedTasks { get; set; }
        
        // Bekleyen görev sayısı (Status != 2)
        public int PendingTasks { get; set; }
        
        // Gecikmiş görev sayısı (tamamlanmamış ve tarihi geçmiş)
        public int OverdueTasks { get; set; }
        
        // Kategori isimleri dizisi
        public string[] Categories { get; set; }
        
        // Her kategorideki görev sayısı dizisi
        public int[] CategoryCounts { get; set; }
    }
}
