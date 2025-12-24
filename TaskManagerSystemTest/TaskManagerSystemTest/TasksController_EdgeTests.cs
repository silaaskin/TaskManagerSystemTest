using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskManagerSystem.Controllers;
using TaskManagerSystem.Data;
using TaskManagerSystem.Models;
using Xunit;


namespace TaskManagerSystem.Tests
{
    public class TasksController_EdgeTests : TestBase
    {
        private AppDbContext GetDatabase()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void UploadAttachment_ExceedingSizeLimit_ShouldReturnFailure()
        {
            var context = GetDatabase();
            var controller = new TasksController(context, new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.FileName).Returns("huge.pdf");
            fileMock.Setup(_ => _.Length).Returns(15 * 1024 * 1024); // 15MB

            var result = controller.UploadAttachment(1, fileMock.Object) as JsonResult;
            var jsonValues = new RouteValueDictionary(result.Value);

            Assert.False((bool)jsonValues["success"]);
            Assert.Contains("boyutu", (string)jsonValues["message"]);
        }

        [Fact]
        public void Edit_TaskOwnedByAnotherUser_ShouldReturnUnauthorized()
        {
            var context = GetDatabase();
            context.Tasks.Add(new UserTask { Id = 10, UserId = 99, Title = "Not Mine" });
            context.SaveChanges();

            var controller = new TasksController(context, new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1); // Farklı kullanıcı

            // DÜZELTME: Sonuna null, null ekleyerek yeni imzaya (9 parametre) uyum sağlıyoruz.
            var result = controller.Edit(10, "Hack", "Desc", 1, 0, DateTime.Now, TimeSpan.Zero, null, null);

            Assert.IsType<UnauthorizedResult>(result);
        }


        [Fact]
        public void Delete_Task_ShouldAlsoDeleteRelatedAttachments()
        {
            // 1. Arrange (Hazırlık)
            var context = GetDatabase();

            // Görev ve ilgili dosya ekini veritabanına ekliyoruz
            var task = new UserTask { Id = 1, Title = "Delete Me", UserId = 1 };
            context.Tasks.Add(task);

            context.TaskAttachments.Add(new TaskAttachment
            {
                Id = 1,
                TaskId = 1,
                OriginalFileName = "test.pdf",
                ContentType = "application/pdf",
                StoragePath = "test_guid.pdf" // Path3 parametresi
            });
            context.SaveChanges();

            // DÜZELTME: IWebHostEnvironment nesnesini kuruyoruz
            var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

            // Path.Combine(path1, path2, path3) metodundaki path1 (WebRootPath) değerini set ediyoruz.
            // Bu satır olmazsa path1 null gelir ve hata verir.
            mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot");

            var controller = new TasksController(context, mockEnv.Object);
            SetupControllerContext(controller);

            // Yetki için Session ayarı
            controller.HttpContext.Session.SetInt32("UserId", 1);
            controller.HttpContext.Session.SetString("UserRole", "User");

            // 2. Act (Eylem)
            var result = controller.Delete(1);

            // 3. Assert (Doğrulama)
            Assert.Equal(0, context.Tasks.Count());
            Assert.Equal(0, context.TaskAttachments.Count()); // Cascade delete kontrolü
        }


    }
}