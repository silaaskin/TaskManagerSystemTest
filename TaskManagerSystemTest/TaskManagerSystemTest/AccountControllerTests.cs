using Microsoft.AspNetCore.Mvc;
using TaskManagerSystem.Controllers;
using Xunit;

namespace TaskManagerSystem.Tests
{
    public class AccountControllerTests : TestBase
    {
        [Fact]
        public void Register_ExistingEmail_ShouldReturnError()
        {
            var context = GetDatabase();
            context.Users.Add(new Models.User { Email = "test@test.com", Password = "123", Name = "User" });
            context.SaveChanges();

            var controller = new AccountController(context);
            SetupControllerContext(controller);

            controller.Register("Name", "test@test.com", "123", "123", "User");

            Assert.Equal("Bu email zaten kayıtlı!", controller.TempData["Message"]);
        }

        [Fact]
        public void Login_WithInvalidCredentials_ShouldReturnError()
        {
            var context = GetDatabase();
            var controller = new AccountController(context);
            SetupControllerContext(controller);

            var result = controller.Login("unknown@test.com", "123");

            Assert.Equal("Hatalı email veya şifre!", controller.TempData["Message"]);
        }
    }
}