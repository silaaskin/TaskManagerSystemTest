using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Linq;
using TaskManagerSystem.Controllers;
using TaskManagerSystem.Models;
using Xunit;

namespace TaskManagerSystem.Tests
{
    // İstatistik endpoint'lerinin doğru hesaplama yaptığını test eden sınıf
    public class TasksStatisticsTests : TestBase
    {
        // GET /api/tasks/stats endpoint'inin toplam görev sayısını doğru hesaplamasını test eder
        [Fact]
        public void GetStats_ShouldReturnTotalTaskCount()
        {
            // 5 görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            for (int i = 0; i < 5; i++)
            {
                context.Tasks.Add(new UserTask { Title = $"Task {i}", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            }
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // Toplam 5 görev olmalı
            Assert.NotNull(stats);
            Assert.Equal(5, stats.TotalTasks);
        }

        // Tamamlanan görev sayısının doğru hesaplanmasını test eder
        [Fact]
        public void GetStats_ShouldCalculateCompletedTasksCorrectly()
        {
            // 3 tamamlanmış, 2 tamamlanmamış görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Completed 1", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Completed 2", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Completed 3", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Pending 1", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Pending 2", UserId = 1, Category = 1, Status = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // 3 tamamlanmış görev olmalı
            Assert.Equal(3, stats.CompletedTasks);
        }

        // Bekleyen görev sayısının doğru hesaplanmasını test eder
        [Fact]
        public void GetStats_ShouldCalculatePendingTasksCorrectly()
        {
            // 2 tamamlanmış, 4 bekleyen görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Completed", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Completed 2", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Not Started", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "In Progress 1", UserId = 1, Category = 1, Status = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "In Progress 2", UserId = 1, Category = 1, Status = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Not Started 2", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // 4 bekleyen görev olmalı (Status != 2)
            Assert.Equal(4, stats.PendingTasks);
        }

        // Gecikmiş görev sayısının doğru hesaplanmasını test eder
        [Fact]
        public void GetStats_ShouldCalculateOverdueTasksCorrectly()
        {
            // Gecikmiş ve zamanında görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            // 3 gecikmiş görev (geçmiş tarihli ve tamamlanmamış)
            context.Tasks.Add(new UserTask { Title = "Overdue 1", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(-5), DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Overdue 2", UserId = 1, Category = 1, Status = 1, DueDate = DateTime.Now.AddDays(-3), DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Overdue 3", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(-1), DueTime = TimeSpan.Zero });
            // Gecikmiş ama tamamlanmış (sayılmamalı)
            context.Tasks.Add(new UserTask { Title = "Completed Overdue", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now.AddDays(-2), DueTime = TimeSpan.Zero });
            // Gelecek tarihli
            context.Tasks.Add(new UserTask { Title = "Future", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(5), DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // 3 gecikmiş görev olmalı (tamamlanmış olanlar hariç)
            Assert.Equal(3, stats.OverdueTasks);
        }

        // Kategoriye göre görev dağılımının doğru hesaplanmasını test eder
        [Fact]
        public void GetStats_ShouldCalculateCategoryDistributionCorrectly()
        {
            // Farklı kategorilerde görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            // 3 Work (Category 1)
            context.Tasks.Add(new UserTask { Title = "Work 1", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Work 2", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Work 3", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            // 2 Personal (Category 2)
            context.Tasks.Add(new UserTask { Title = "Personal 1", UserId = 1, Category = 2, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Personal 2", UserId = 1, Category = 2, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            // 1 Other (Category 3)
            context.Tasks.Add(new UserTask { Title = "Other 1", UserId = 1, Category = 3, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // Kategori dağılımını kontrol et
            Assert.NotNull(stats.Categories);
            Assert.NotNull(stats.CategoryCounts);
            Assert.Contains("Work", stats.Categories);
            Assert.Contains("Personal", stats.Categories);
            Assert.Contains("Other", stats.Categories);
            Assert.Contains(3, stats.CategoryCounts); // Work: 3
            Assert.Contains(2, stats.CategoryCounts); // Personal: 2
            Assert.Contains(1, stats.CategoryCounts); // Other: 1
        }

        // Admin'in tüm kullanıcıların istatistiklerini görebilmesini test eder
        [Fact]
        public void GetStats_AsAdmin_ShouldReturnAllUsersStatistics()
        {
            // Farklı kullanıcılara ait görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "Admin", Email = "admin@test.com", Password = "123", Role = "Admin" });
            context.Users.Add(new User { Id = 2, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            // Admin'in görevi
            context.Tasks.Add(new UserTask { Title = "Admin Task", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            // User'ın görevi
            context.Tasks.Add(new UserTask { Title = "User Task", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "Admin");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // Admin tüm görevlerin istatistiklerini görebilmeli
            Assert.Equal(2, stats.TotalTasks);
        }

        // Normal User'ın sadece kendi istatistiklerini görebilmesini test eder
        [Fact]
        public void GetStats_AsUser_ShouldReturnOnlyOwnStatistics()
        {
            // Farklı kullanıcılara ait görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 2, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            // User1'in 3 görevi
            context.Tasks.Add(new UserTask { Title = "User1 Task 1", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User1 Task 2", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User1 Task 3", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            // User2'nin 5 görevi
            context.Tasks.Add(new UserTask { Title = "User2 Task 1", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User2 Task 2", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User2 Task 3", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User2 Task 4", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User2 Task 5", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // User1 sadece kendi 3 görevinin istatistiklerini görebilmeli
            Assert.Equal(3, stats.TotalTasks);
            Assert.Equal(1, stats.CompletedTasks);
            Assert.Equal(2, stats.PendingTasks);
        }

        // Giriş yapmadan istatistik çağrısı yapıldığında Unauthorized döndürmesini test eder
        [Fact]
        public void GetStats_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Session'da UserId olmadan istatistik çağrısı yap
            var context = GetDatabase();
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            // UserId session'a set edilmedi

            // Unauthorized dönmeli
            var result = controller.GetStats() as UnauthorizedResult;

            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        // Görev olmayan kullanıcının istatistiklerinin sıfır olmasını test eder
        [Fact]
        public void GetStats_WithNoTasks_ShouldReturnZeroStatistics()
        {
            // Hiç görev olmayan kullanıcı oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // Tüm istatistikler sıfır olmalı
            Assert.Equal(0, stats.TotalTasks);
            Assert.Equal(0, stats.CompletedTasks);
            Assert.Equal(0, stats.PendingTasks);
            Assert.Equal(0, stats.OverdueTasks);
        }

        // İstatistik modelinin tüm alanlarının dolu olmasını test eder
        [Fact]
        public void GetStats_ShouldReturnCompleteStatisticsModel()
        {
            // Örnek görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Task 1", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Task 2", UserId = 1, Category = 2, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // Model tüm gerekli alanları içermeli
            Assert.NotNull(stats);
            Assert.True(stats.TotalTasks >= 0);
            Assert.True(stats.CompletedTasks >= 0);
            Assert.True(stats.PendingTasks >= 0);
            Assert.True(stats.OverdueTasks >= 0);
            Assert.NotNull(stats.Categories);
            Assert.NotNull(stats.CategoryCounts);
        }

        // Tamamlama oranı hesaplamasının doğru olmasını test eder
        [Fact]
        public void GetStats_ShouldCalculateCompletionRateCorrectly()
        {
            // 10 görevden 7'si tamamlanmış
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            
            // 7 tamamlanmış
            for (int i = 0; i < 7; i++)
            {
                context.Tasks.Add(new UserTask { Title = $"Completed {i}", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            }
            // 3 tamamlanmamış
            for (int i = 0; i < 3; i++)
            {
                context.Tasks.Add(new UserTask { Title = $"Pending {i}", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            }
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // 10 toplam, 7 tamamlanmış, 3 bekleyen
            Assert.Equal(10, stats.TotalTasks);
            Assert.Equal(7, stats.CompletedTasks);
            Assert.Equal(3, stats.PendingTasks);
        }

        // Karışık durumlardaki görevlerin doğru kategorize edilmesini test eder
        [Fact]
        public void GetStats_WithMixedTaskStates_ShouldCategorizeCorrectly()
        {
            // Farklı durumlarda ve tarihlerde görevler
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            
            // Gecikmiş ve devam eden
            context.Tasks.Add(new UserTask { Title = "Overdue In Progress", UserId = 1, Category = 1, Status = 1, DueDate = DateTime.Now.AddDays(-3), DueTime = TimeSpan.Zero });
            // Gecikmiş ve başlamamış
            context.Tasks.Add(new UserTask { Title = "Overdue Not Started", UserId = 1, Category = 2, Status = 0, DueDate = DateTime.Now.AddDays(-1), DueTime = TimeSpan.Zero });
            // Zamanında tamamlanmış
            context.Tasks.Add(new UserTask { Title = "Completed On Time", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now.AddDays(5), DueTime = TimeSpan.Zero });
            // Geç tamamlanmış
            context.Tasks.Add(new UserTask { Title = "Completed Late", UserId = 1, Category = 3, Status = 2, DueDate = DateTime.Now.AddDays(-2), DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // İstatistikleri getir
            var result = controller.GetStats() as OkObjectResult;
            var stats = result.Value as TaskStatsViewModel;

            // Doğru kategorize edilmiş olmalı
            Assert.Equal(4, stats.TotalTasks);
            Assert.Equal(2, stats.CompletedTasks); // Tamamlanmış olanlar
            Assert.Equal(2, stats.PendingTasks); // Devam eden ve başlamamış
            Assert.Equal(2, stats.OverdueTasks); // Gecikmiş ve tamamlanmamış
        }
    }
}
