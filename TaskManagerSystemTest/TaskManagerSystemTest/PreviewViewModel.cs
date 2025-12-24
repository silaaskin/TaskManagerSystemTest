namespace TaskManagerSystem.Models
{
    // Dosya önizleme sayfası için veri taşıma modeli
    public class PreviewViewModel
    {
        // Dosyanın orijinal adı
        public string FileName { get; set; }
        
        // Dosya tipi açıklaması (örn: "Word Document", "Excel Spreadsheet")
        public string FileType { get; set; }
        
        // Dosya boyutu (byte cinsinden)
        public long FileSize { get; set; }
        
        // Kullanıcıya gösterilecek bilgi mesajı
        public string Message { get; set; }
        
        // İndirme URL'i
        public string DownloadUrl { get; set; }
    }
}
