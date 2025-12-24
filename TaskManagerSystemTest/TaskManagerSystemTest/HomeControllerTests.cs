using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagerSystem.Controllers;
using TaskManagerSystem.Models;
using Xunit;

namespace TaskManagerSystem.Tests
{
    public class HomeControllerTests : TestBase
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _controller = new HomeController(_mockLogger.Object);
        }

        [Fact]
        public void Logout_ShouldRedirectToAccountLogin()
        {
            var result = _controller.Logout();
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void Error_ShouldReturnViewWithErrorViewModel()
        {
            SetupControllerContext(_controller);
            var result = _controller.Error();
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<ErrorViewModel>(viewResult.ViewData.Model);
        }
    }
}