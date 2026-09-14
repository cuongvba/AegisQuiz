using Xunit;
using AegisQuiz.Application.Common.Security;

namespace AegisQuiz.UnitTests
{
    public class PasswordSecurityHelperTests
    {
        [Fact]
        public void HashPassword_ShouldReturnValidHashString()
        {
            var hash = PasswordSecurityHelper.HashPassword("SecretPassword123!");

            Assert.NotNull(hash);
            Assert.StartsWith("pbkdf2_sha256:100000:", hash);
        }

        [Fact]
        public void HashPassword_ShouldProduceDifferentHashesForSamePassword()
        {
            var hash1 = PasswordSecurityHelper.HashPassword("SamePassword2026!");
            var hash2 = PasswordSecurityHelper.HashPassword("SamePassword2026!");

            Assert.NotEqual(hash1, hash2); // Salt ngẫu nhiên đảm bảo 2 hash khác nhau
        }

        [Fact]
        public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
        {
            var password = "CorrectPassword@2026";
            var hash = PasswordSecurityHelper.HashPassword(password);

            var result = PasswordSecurityHelper.VerifyPassword(password, hash);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
        {
            var hash = PasswordSecurityHelper.HashPassword("CorrectPassword@2026");

            var result = PasswordSecurityHelper.VerifyPassword("WrongPassword!", hash);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalseForInvalidHash()
        {
            Assert.False(PasswordSecurityHelper.VerifyPassword("Pass123", "invalid-hash"));
            Assert.False(PasswordSecurityHelper.VerifyPassword("Pass123", ""));
            Assert.False(PasswordSecurityHelper.VerifyPassword("", "pbkdf2_sha256:100000:abc:def"));
        }
    }
}
