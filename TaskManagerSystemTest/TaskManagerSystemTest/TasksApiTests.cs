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
    // API endpoint'lerinin doğru çalışıp çalışmadığını test eden sınıf
    public class TasksApiTests : TestBase
    {
        // GET /api/tasks endpoint'inin tüm görevleri döndürmesini test eder
        [Fact]
        public void GetTasks_ShouldReturnAllTasksAsJson()
        {
            // Test verileri oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "Task 1", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Id = 2, Title = "Task 2", UserId = 1, Category = 2, Status = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            // Controller oluştur ve session ayarla
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // API endpoint'i çağır
            var result = controller.GetTasks(null, null, null, null, null) as OkObjectResult;

            // Sonuç kontrolü
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            
            // JSON içeriğini kontrol et
            dynamic data = result.Value;
            Assert.True(data.success);
            Assert.Equal(2, data.count);
        }

        // Kategoriye göre filtreleme yapıldığında doğru sonuç döndürmesini test eder
        [Fact]
        public void GetTasks_WithCategoryFilter_ShouldReturnFilteredTasks()
        {
            // Farklı kategorilerde görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Work Task", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Personal Task", UserId = 1, Category = 2, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // Category=1 (Work) ile filtrele
            var result = controller.GetTasks(1, null, null, null, null) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(1, data.count); // Sadece Work kategorisindeki görev dönmeli
        }

        // Duruma göre filtreleme yapıldığında doğru sonuç döndürmesini test eder
        [Fact]
        public void GetTasks_WithStatusFilter_ShouldReturnFilteredTasks()
        {
            // Farklı durumlarda görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Not Started", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "In Progress", UserId = 1, Category = 1, Status = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Completed", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // Status=2 (Completed) ile filtrele
            var result = controller.GetTasks(null, 2, null, null, null) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(1, data.count); // Sadece tamamlanmış görev dönmeli
        }

        // Tamamlanmış görevlerin filtrelenmesini test eder
        [Fact]
        public void GetTasks_WithCompletedFilter_ShouldReturnCompletedTasks()
        {
            // Tamamlanmış ve tamamlanmamış görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Task 1", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Task 2", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Task 3", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // completed=true ile filtrele
            var result = controller.GetTasks(null, null, true, null, null) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(2, data.count); // 2 tamamlanmış görev dönmeli
        }

        // Gecikmiş görevlerin tespit edilmesini test eder
        [Fact]
        public void GetTasks_WithOverdueFilter_ShouldReturnOverdueTasks()
        {
            // Geçmiş ve gelecek tarihli görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Overdue", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(-5), DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Future", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(5), DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // overdue=true ile filtrele
            var result = controller.GetTasks(null, null, null, true, null) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(1, data.count); // Sadece gecikmiş görev dönmeli
        }

        // Yaklaşan görevlerin filtrelenmesini test eder
        [Fact]
        public void GetTasks_WithUpcomingFilter_ShouldReturnUpcomingTasks()
        {
            // Farklı tarihlerde görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Tomorrow", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(1), DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "Next Week", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(7), DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // upcoming=3 (3 gün içindeki görevler)
            var result = controller.GetTasks(null, null, null, null, 3) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(1, data.count); // Sadece 1 günlük görev 3 gün içinde
        }

        // Giriş yapmadan API çağrısı yapıldığında Unauthorized döndürmesini test eder
        [Fact]
        public void GetTasks_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Session'da UserId olmadan API çağrısı yap
            var context = GetDatabase();
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            // UserId session'a set edilmedi

            // Unauthorized dönmeli
            var result = controller.GetTasks(null, null, null, null, null) as UnauthorizedObjectResult;

            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);
        }

        // Admin'in tüm kullanıcıların görevlerini API'den görebilmesini test eder
        [Fact]
        public void GetTasks_AsAdmin_ShouldReturnAllUsersTasks()
        {
            // Farklı kullanıcılara ait görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "Admin", Email = "admin@test.com", Password = "123", Role = "Admin" });
            context.Users.Add(new User { Id = 2, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Admin Task", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User Task", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "Admin");

            // API'den tüm görevleri getir
            var result = controller.GetTasks(null, null, null, null, null) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(2, data.count); // Admin tüm görevleri görebilmeli
        }

        // Normal User'ın API'den sadece kendi görevlerini görebilmesini test eder
        [Fact]
        public void GetTasks_AsUser_ShouldReturnOnlyOwnTasks()
        {
            // Farklı kullanıcılara ait görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 2, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "User1 Task", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "User2 Task", UserId = 2, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // API'den görevleri getir
            var result = controller.GetTasks(null, null, null, null, null) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(1, data.count); // Sadece kendi görevini görebilmeli
        }

        // API'nin doğru JSON formatında veri döndürmesini test eder
        [Fact]
        public void GetTasks_ShouldReturnCorrectJsonFormat()
        {
            // Test görevi oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask 
            { 
                Id = 1,
                Title = "Test Task", 
                Description = "Description",
                UserId = 1, 
                Category = 1, 
                Status = 0, 
                DueDate = new DateTime(2025, 12, 31), 
                DueTime = new TimeSpan(14, 30, 0) 
            });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // API çağrısı yap
            var result = controller.GetTasks(null, null, null, null, null) as OkObjectResult;
            dynamic data = result.Value;

            // JSON formatını kontrol et
            Assert.True(data.success);
            Assert.NotNull(data.count);
            Assert.NotNull(data.data);
        }

        // Çoklu filtre kombinasyonunun doğru çalışmasını test eder
        [Fact]
        public void GetTasks_WithMultipleFilters_ShouldReturnCorrectResults()
        {
            // Farklı özelliklerde görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Title = "Match", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "No Match", UserId = 1, Category = 2, Status = 0, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Title = "No Match 2", UserId = 1, Category = 1, Status = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // category=1 ve status=0 ile filtrele
            var result = controller.GetTasks(1, 0, null, null, null) as OkObjectResult;

            dynamic data = result.Value;
            Assert.Equal(1, data.count); // Sadece her iki kritere uyan görev dönmeli
        }

        // Alert seviyelerinin doğru hesaplanmasını test eder
        [Fact]
        public void GetTasks_ShouldCalculateAlertLevelsCorrectly()
        {
            // Farklı tarihlerde görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            // Gecikmiş görev
            context.Tasks.Add(new UserTask { Title = "Overdue", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(-1), DueTime = TimeSpan.Zero });
            // Yakın tarihli görev (24 saat içinde)
            context.Tasks.Add(new UserTask { Title = "Urgent", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddHours(12), DueTime = TimeSpan.Zero });
            // Yaklaşan görev (72 saat içinde)
            context.Tasks.Add(new UserTask { Title = "Approaching", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddHours(48), DueTime = TimeSpan.Zero });
            // Normal görev
            context.Tasks.Add(new UserTask { Title = "Normal", UserId = 1, Category = 1, Status = 0, DueDate = DateTime.Now.AddDays(7), DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // Tüm görevleri getir
            var result = controller.GetTasks(null, null, null, null, null) as OkObjectResult;

            // Alert seviyelerinin hesaplandığını doğrula
            Assert.NotNull(result);
            dynamic data = result.Value;
            Assert.Equal(4, data.count);
        }
    }
}
