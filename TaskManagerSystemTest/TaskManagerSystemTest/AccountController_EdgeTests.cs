using Microsoft.AspNetCore.Mvc;
using TaskManagerSystem.Controllers;
using TaskManagerSystem.Models;
using Xunit;

namespace TaskManagerSystem.Tests
{
    public class AccountController_EdgeTests : TestBase // Miras alıyoruz
    {
        [Fact]
        public void Register_WithEmptyFields_ShouldReturnRequiredFieldErrorMessage()
        {
            // Arrange
            var context = GetDatabase();
            var controller = new AccountController(context);
            SetupControllerContext(controller); // TestBase'den gelen metot

            // Act
            var result = controller.Register("", "", "123", "123", "User");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Lütfen tüm alanları doldurun!", controller.ViewBag.Message);
        }

        [Fact]
        public void Register_WhenPasswordsDoNotMatch_ShouldReturnErrorMessage()
        {
            // Arrange
            var context = GetDatabase();
            var controller = new AccountController(context);
            SetupControllerContext(controller);

            // Act
            var result = controller.Register("Test", "test@test.com", "123", "456", "User");

            // Assert
            Assert.Equal("Şifreler eşleşmiyor!", controller.TempData["Message"]);
        }

        [Fact]
        public void Register_WithExistingEmail_ShouldReturnDuplicateEmailMessage()
        {
            // Arrange
            var context = GetDatabase();
            context.Users.Add(new User { Email = "duplicate@test.com", Name = "User", Password = "123" });
            context.SaveChanges();

            var controller = new AccountController(context);
            SetupControllerContext(controller);

            // Act
            controller.Register("New User", "duplicate@test.com", "123", "123", "User");

            // Assert
            Assert.Equal("Bu email zaten kayıtlı!", controller.TempData["Message"]);
        }

        [Fact]
        public void Login_WithInvalidCredentials_ShouldReturnFailureMessage()
        {
            // Arrange
            var context = GetDatabase();
            var controller = new AccountController(context);
            SetupControllerContext(controller);

            // Act
            var result = controller.Login("notfound@test.com", "wrongpass");

            // Assert
            Assert.Equal("Hatalı email veya şifre!", controller.TempData["Message"]);
        }
    }
}