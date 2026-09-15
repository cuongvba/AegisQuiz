using System;
using System.Text;
using AegisQuiz.Infrastructure.Security;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class TotpSecurityTests
    {
        [Fact]
        public void Base32_Roundtrip_Should_Preserve_Original_Bytes()
        {
            var original = Encoding.UTF8.GetBytes("AegisQuiz World Class Security 2026!");
            var encoded = TotpSecurityHelper.ToBase32String(original);
            Assert.False(string.IsNullOrWhiteSpace(encoded));

            var decoded = TotpSecurityHelper.FromBase32String(encoded);
            Assert.Equal(original, decoded);
        }

        [Fact]
        public void GenerateSecretKey_Should_Produce_Valid_Base32_String()
        {
            var secret = TotpSecurityHelper.GenerateSecretKey(20);
            Assert.NotNull(secret);
            Assert.Equal(32, secret.Length); // 20 bytes * 8 / 5 = 32 chars

            var bytes = TotpSecurityHelper.FromBase32String(secret);
            Assert.Equal(20, bytes.Length);
        }

        [Fact]
        public void GenerateTotpCode_Should_Return_6_Digit_String()
        {
            var secret = TotpSecurityHelper.GenerateSecretKey(20);
            var code = TotpSecurityHelper.GenerateTotpCode(secret);

            Assert.NotNull(code);
            Assert.Equal(6, code.Length);
            Assert.True(int.TryParse(code, out _));
        }

        [Fact]
        public void ValidateTotpCode_Should_Accept_Current_And_Adjacent_TimeSteps()
        {
            var secret = TotpSecurityHelper.GenerateSecretKey(20);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Current step
            var currentCode = TotpSecurityHelper.GenerateTotpCode(secret, now);
            Assert.True(TotpSecurityHelper.ValidateTotpCode(secret, currentCode, toleranceSteps: 1));

            // Step - 1 (-30s)
            var prevCode = TotpSecurityHelper.GenerateTotpCode(secret, now - 30);
            Assert.True(TotpSecurityHelper.ValidateTotpCode(secret, prevCode, toleranceSteps: 1));

            // Step + 1 (+30s)
            var nextCode = TotpSecurityHelper.GenerateTotpCode(secret, now + 30);
            Assert.True(TotpSecurityHelper.ValidateTotpCode(secret, nextCode, toleranceSteps: 1));

            // Invalid code
            Assert.False(TotpSecurityHelper.ValidateTotpCode(secret, "999999", toleranceSteps: 0));
        }

        [Fact]
        public void GenerateOtpAuthUri_Should_Format_Standard_Uri()
        {
            var secret = "JBSWY3DPEHPK3PXP";
            var email = "admin@dehoc.vn";
            var issuer = "AegisQuiz";

            var uri = TotpSecurityHelper.GenerateOtpAuthUri(issuer, email, secret);

            Assert.StartsWith("otpauth://totp/", uri);
            Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri);
            Assert.Contains("issuer=AegisQuiz", uri);
        }

        [Fact]
        public void RecoveryCodes_Should_Be_Usable_Only_Once()
        {
            var rawCodes = TotpSecurityHelper.GenerateRecoveryCodes(8);
            Assert.Equal(8, rawCodes.Count);

            string? serialized = TotpSecurityHelper.SerializeRecoveryCodes(rawCodes);
            Assert.NotNull(serialized);

            var firstCode = rawCodes[0];

            // First consumption -> true
            var success1 = TotpSecurityHelper.ValidateAndConsumeRecoveryCode(firstCode, ref serialized);
            Assert.True(success1);

            // Second consumption -> false (Burn after use)
            var success2 = TotpSecurityHelper.ValidateAndConsumeRecoveryCode(firstCode, ref serialized);
            Assert.False(success2);

            // Invalid code -> false
            var success3 = TotpSecurityHelper.ValidateAndConsumeRecoveryCode("INVALID-CODE", ref serialized);
            Assert.False(success3);
        }
    }
}
