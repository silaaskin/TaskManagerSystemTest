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
    // Admin ve User yetki kontrollerini test eden sınıf
    public class AdminAuthorizationTests : TestBase
    {
        // Admin'in tüm kullanıcıların görevlerini görebilmesini test eder
        [Fact]
        public void Index_AsAdmin_ShouldSeeAllTasks()
        {
            // Veritabanına farklı kullanıcılara ait görevler ekle
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "Admin", Email = "admin@test.com", Password = "123", Role = "Admin" });
            context.Users.Add(new User { Id = 2, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "Admin Task", UserId = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Id = 2, Title = "User1 Task", UserId = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            // Admin olarak giriş yap
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "Admin");

            // Index sayfasını görüntüle
            var result = controller.Index() as ViewResult;
            var tasks = result.Model as System.Collections.Generic.List<UserTask>;

            // Admin tüm görevleri görebilmeli (2 adet)
            Assert.NotNull(tasks);
            Assert.Equal(2, tasks.Count);
            Assert.True(controller.ViewBag.IsAdmin);
        }

        // Normal User'ın sadece kendi görevlerini görebilmesini test eder
        [Fact]
        public void Index_AsUser_ShouldSeeOnlyOwnTasks()
        {
            // Farklı kullanıcılara ait görevler oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 2, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "User1 Task", UserId = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.Tasks.Add(new UserTask { Id = 2, Title = "User2 Task", UserId = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            // User1 olarak giriş yap
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // Index sayfasını görüntüle
            var result = controller.Index() as ViewResult;
            var tasks = result.Model as System.Collections.Generic.List<UserTask>;

            // User sadece kendi görevini görebilmeli (1 adet)
            Assert.NotNull(tasks);
            Assert.Single(tasks);
            Assert.Equal(1, tasks[0].UserId);
            Assert.False(controller.ViewBag.IsAdmin);
        }

        // Admin'in başka kullanıcının görevini düzenleyebilmesini test eder
        [Fact]
        public void Edit_AsAdmin_CanEditOtherUserTask()
        {
            // Başka kullanıcıya ait görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "Admin", Email = "admin@test.com", Password = "123", Role = "Admin" });
            context.Users.Add(new User { Id = 2, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "User Task", UserId = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            // Admin olarak giriş yap ve görevi düzenle
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "Admin");

            // Görevi düzenleme sayfasını aç
            var result = controller.Edit(1) as ViewResult;

            // Admin başkasının görevini düzenleyebilmeli
            Assert.NotNull(result);
            Assert.NotNull(result.Model);
        }

        // Normal User'ın başka kullanıcının görevini düzenleyememesini test eder
        [Fact]
        public void Edit_AsUser_CannotEditOtherUserTask()
        {
            // Başka kullanıcıya ait görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 2, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "User2 Task", UserId = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            // User1 olarak giriş yap ve User2'nin görevini düzenlemeyi dene
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // Düzenleme denemesi Unauthorized dönmeli
            var result = controller.Edit(1);

            Assert.IsType<UnauthorizedResult>(result);
        }

        // Admin'in başka kullanıcının görevini silebilmesini test eder
        [Fact]
        public void Delete_AsAdmin_CanDeleteOtherUserTask()
        {
            // Başka kullanıcıya ait görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "Admin", Email = "admin@test.com", Password = "123", Role = "Admin" });
            context.Users.Add(new User { Id = 2, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "User Task", UserId = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");

            // Admin olarak giriş yap ve görevi sil
            var controller = new TasksController(context, mockEnv.Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "Admin");

            // Görevi sil
            var result = controller.Delete(1);

            // Silme işlemi başarılı olmalı
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(0, context.Tasks.Count());
        }

        // Normal User'ın başka kullanıcının görevini silememesini test eder
        [Fact]
        public void Delete_AsUser_CannotDeleteOtherUserTask()
        {
            // Başka kullanıcıya ait görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 2, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "User2 Task", UserId = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");

            // User1 olarak giriş yap ve User2'nin görevini silmeyi dene
            var controller = new TasksController(context, mockEnv.Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // Silme denemesi Unauthorized dönmeli
            var result = controller.Delete(1);

            Assert.IsType<UnauthorizedResult>(result);
            Assert.Equal(1, context.Tasks.Count()); // Görev silinmemiş olmalı
        }

        // Admin'in görevi başka kullanıcıya atayabilmesini test eder
        [Fact]
        public void Create_AsAdmin_CanAssignTaskToOtherUser()
        {
            // Test ortamını hazırla
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "Admin", Email = "admin@test.com", Password = "123", Role = "Admin" });
            context.Users.Add(new User { Id = 2, Name = "User", Email = "user@test.com", Password = "123", Role = "User" });
            context.SaveChanges();

            // Admin olarak giriş yap ve User'a görev ata
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "Admin");

            // Admin User2'ye görev atıyor
            var result = controller.Create("Admin Assigned Task", "Description", 1, DateTime.Now, TimeSpan.Zero, null, 2);

            // Görev User2'ye atanmış olmalı
            var createdTask = context.Tasks.FirstOrDefault();
            Assert.NotNull(createdTask);
            Assert.Equal(2, createdTask.UserId); // Görev User2'ye ait
            Assert.Equal(1, createdTask.CreatedByUserId); // Görevi Admin oluşturdu
        }

        // Normal User'ın görev oluştururken sadece kendine atayabilmesini test eder
        [Fact]
        public void Create_AsUser_CanOnlyAssignToSelf()
        {
            // Test ortamını hazırla
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 2, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            context.SaveChanges();

            // User1 olarak giriş yap
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // User1 görev oluşturuyor (assignedUserId parametresi yok sayılmalı)
            var result = controller.Create("User Task", "Description", 1, DateTime.Now, TimeSpan.Zero, null, 2);

            // Görev User1'e atanmış olmalı (User başkasına atayamaz)
            var createdTask = context.Tasks.FirstOrDefault();
            Assert.NotNull(createdTask);
            Assert.Equal(1, createdTask.UserId); // Görev kendisine atandı
            Assert.Equal(1, createdTask.CreatedByUserId);
        }

        // Admin'in görevi yeniden atayabilmesini (reassign) test eder
        [Fact]
        public void Edit_AsAdmin_CanReassignTaskToAnotherUser()
        {
            // Mevcut görev oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "Admin", Email = "admin@test.com", Password = "123", Role = "Admin" });
            context.Users.Add(new User { Id = 2, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 3, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "Task", UserId = 2, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            // Admin olarak giriş yap ve görevi User2'ye yeniden ata
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "Admin");

            // Görevi User2'ye yeniden ata
            controller.Edit(1, "Updated Task", "Desc", 1, 0, DateTime.Now, TimeSpan.Zero, null, 3);

            // Görev artık User2'ye ait olmalı
            var updatedTask = context.Tasks.FirstOrDefault(t => t.Id == 1);
            Assert.Equal(3, updatedTask.UserId);
        }

        // Normal User'ın görev sahipliğini değiştiremeyeceğini test eder
        [Fact]
        public void Edit_AsUser_CannotReassignTask()
        {
            // Kendi görevini oluştur
            var context = GetDatabase();
            context.Users.Add(new User { Id = 1, Name = "User1", Email = "user1@test.com", Password = "123", Role = "User" });
            context.Users.Add(new User { Id = 2, Name = "User2", Email = "user2@test.com", Password = "123", Role = "User" });
            context.Tasks.Add(new UserTask { Id = 1, Title = "Task", UserId = 1, DueDate = DateTime.Now, DueTime = TimeSpan.Zero });
            context.SaveChanges();

            // User1 olarak giriş yap ve görevi düzenle (başkasına atamayı dene)
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // assignedUserId parametresi yok sayılmalı
            controller.Edit(1, "Updated", "Desc", 1, 0, DateTime.Now, TimeSpan.Zero, null, 2);

            // Görev hala User1'e ait olmalı
            var task = context.Tasks.FirstOrDefault(t => t.Id == 1);
            Assert.Equal(1, task.UserId);
        }

        // Giriş yapmamış kullanıcının Index sayfasına erişememesini test eder
        [Fact]
        public void Index_WithoutLogin_ShouldRedirectToLogin()
        {
            // Giriş yapmadan Index sayfasına erişmeyi dene
            var context = GetDatabase();
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            // Session'da UserId yok

            // Login sayfasına yönlendirme yapmalı
            var result = controller.Index() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);
        }
    }
}
