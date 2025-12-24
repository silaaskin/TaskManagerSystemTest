using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using System;
using System.Linq;
using TaskManagerSystem.Controllers;
using TaskManagerSystem.Models;
using Xunit;

namespace TaskManagerSystem.Tests
{
    public class TasksControllerTests : TestBase
    {
        [Fact]
        public void Create_WithEmptyTitle_ShouldNotSaveToDatabase()
        {
            var context = GetDatabase();
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);

            controller.Create("", "Desc", 1, DateTime.Now, TimeSpan.Zero, null, 1);

            Assert.Equal(0, context.Tasks.Count());
        }

        [Fact]
        public void Delete_NonExistentTask_ShouldReturnNotFound()
        {
            var context = GetDatabase();
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);

            var result = controller.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void UploadAttachment_WithInvalidExtension_ShouldReturnFailureMessage()
        {
            var context = GetDatabase();
            var controller = new TasksController(context, new Mock<IWebHostEnvironment>().Object);
            SetupControllerContext(controller);
            controller.HttpContext.Session.SetInt32("UserId", 1);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.FileName).Returns("danger.exe");
            fileMock.Setup(_ => _.Length).Returns(100);

            var result = controller.UploadAttachment(1, fileMock.Object) as JsonResult;
            var jsonValues = new RouteValueDictionary(result.Value);

            Assert.False((bool)jsonValues["success"]);
            Assert.Contains("formatı hatalı", (string)jsonValues["message"]);
        }
    }
}