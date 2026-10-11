using System;
using System.Threading.Tasks;
using Xunit;
using AegisQuiz.Infrastructure.Security;

namespace AegisQuiz.UnitTests
{
    public class WolfPackSwarmTests
    {
        [Fact]
        public void ApiKeyEncryptionHelper_EncryptAndDecrypt_ReturnsOriginalKey()
        {
            var rawKey = "sk-deepseek-r1-super-secret-quantum-2026";
            var secret = "TestMasterSecretForAegisQuizUnitTests";

            var encrypted = ApiKeyEncryptionHelper.Encrypt(rawKey, secret);
            Assert.False(string.IsNullOrEmpty(encrypted));
            Assert.NotEqual(rawKey, encrypted);

            var decrypted = ApiKeyEncryptionHelper.Decrypt(encrypted, secret);
            Assert.Equal(rawKey, decrypted);
        }

        [Fact]
        public void ApiKeyEncryptionHelper_GenerateFingerprint_HidesSecret()
        {
            var rawKey = "sk-1234567890abcdef";
            var fingerprint = ApiKeyEncryptionHelper.GenerateFingerprint(rawKey);

            Assert.StartsWith("sk-1", fingerprint);
            Assert.EndsWith("cdef", fingerprint);
            Assert.Contains("••••", fingerprint);
            Assert.DoesNotContain("567890", fingerprint);
        }
    }
}
