using Xunit;
using TaskManagerSystem;

namespace TaskManagerSystem.Tests
{
    public class PasswordHelperTests
    {
        [Fact]
        public void Password_Should_Hash_And_Verify_Correctly()
        {
            string myPassword = "User123!";
            string hashed = PasswordHelper.HashPassword(myPassword);
            bool isValid = PasswordHelper.VerifyPassword(myPassword, hashed);

            Assert.True(isValid);
        }
    }
}