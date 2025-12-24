using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerSystem.Models
{
    // Görevlere eklenen dosya eklerini temsil eden model
    public class TaskAttachment
    {
        // Dosya eki ID'si
        public int Id { get; set; }
        
        // Hangi göreve ait olduğu
        public int TaskId { get; set; }
        
        // İlişkili görev nesnesi
        [ForeignKey("TaskId")]
        public UserTask Task { get; set; }
        
        // Dosyayı yükleyen kullanıcı ID'si
        public int UploadedByUserId { get; set; }
        
        // Dosyanın orijinal adı
        [Required]
        public string OriginalFileName { get; set; }
        
        // Sunucuda kaydedilen dosya adı (benzersiz)
        [Required]
        public string StoragePath { get; set; }
        
        // Dosya içerik tipi (MIME type)
        [Required]
        public string ContentType { get; set; }
        
        // Dosya boyutu (byte cinsinden)
        public long FileSize { get; set; }
        
        // Yüklenme tarihi
        public DateTime UploadDate { get; set; }
    }
}
