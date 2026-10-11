using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class WorldClassProtocolsTests
    {
        [Fact]
        public async Task NtpTimeSyncService_Should_Calibrate_And_Normalize_Timestamps()
        {
            // Arrange
            var service = new NtpTimeSyncService(NullLogger<NtpTimeSyncService>.Instance);
            var req = new NtpSyncRequestDto
            {
                ClientSendTimestampUs = 1700000000000000,
                ClientSessionId = "test_sess_01",
                PlayerId = "player_123"
            };

            // Act
            var res = await service.SynchronizeClockAsync(req);

            // Assert
            Assert.Equal(req.ClientSendTimestampUs, res.ClientSendTimestampUs);
            Assert.True(res.ServerReceiveTimestampUs > 0);
            Assert.True(res.ServerTransmitTimestampUs >= res.ServerReceiveTimestampUs);
            Assert.False(string.IsNullOrWhiteSpace(res.SessionSecretToken));

            // Test Normalization: client clock is 50ms ahead (skew = +50ms)
            long fingerDownUs = 1700000000050000;
            long normalizedUs = service.NormalizeFingerDownTimestamp(fingerDownUs, clockSkewMs: 50.0);
            Assert.Equal(1700000000000000, normalizedUs);

            // Test HMAC-SHA256 signature verification
            var payload = $"ROOM_1:player_123:{fingerDownUs}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(res.SessionSecretToken));
            var validSigHex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

            bool isValid = service.VerifyBuzzerSignature("ROOM_1", "player_123", fingerDownUs, validSigHex, res.SessionSecretToken);
            Assert.True(isValid);

            bool isTampered = service.VerifyBuzzerSignature("ROOM_1", "player_123", fingerDownUs + 1000, validSigHex, res.SessionSecretToken);
            Assert.False(isTampered);
        }

        [Fact]
        public async Task OfflineSyncService_Should_Accept_Valid_Merkle_Ledger()
        {
            // Arrange
            var service = new OfflineSyncService(NullLogger<OfflineSyncService>.Instance);
            var req = new OfflineSyncRequestDto
            {
                ExamSessionId = "session_xyz",
                UserId = "user_456",
                StartedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 60000,
                LastUpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                MerkleHeadHash = "a1b2c3d4e5f67890123456789abcdef0123456789abcdef0123456789abcdef0",
                Answers = new Dictionary<string, OfflineAnswerItemDto>
                {
                    ["q1"] = new OfflineAnswerItemDto { SelectedOption = "1", AnsweredAt = 1000, ProofHash = "hash1" },
                    ["q2"] = new OfflineAnswerItemDto { SelectedOption = "3", AnsweredAt = 2000, ProofHash = "hash2" }
                }
            };

            // Act
            var result = await service.ProcessOfflineSyncAsync(req);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.AcceptedAnswersCount);
            Assert.Equal(req.MerkleHeadHash, result.VerifiedMerkleHeadHash);
        }

        [Fact]
        public async Task Lti13Service_Should_Decode_Valid_Jwt_Payload()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            var service = new Lti13Service(config, null!, NullLogger<Lti13Service>.Instance);

            // Create a mock LTI 1.3 JWT Payload (Header.Payload.Signature)
            var mockPayloadJson = @"{
                ""sub"": ""canvas_student_99"",
                ""email"": ""student@stanford.edu"",
                ""name"": ""Alexander Hamilton"",
                ""https://purl.imsglobal.org/spec/lti/claim/custom"": {
                    ""exam_id"": ""exam_fin_2026""
                },
                ""https://purl.imsglobal.org/spec/lti-ags/claim/endpoint"": {
                    ""lineitem"": ""https://canvas.instructure.com/api/lti/courses/1/line_items/5""
                }
            }";

            string base64Header = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"{""alg"":""RS256"",""typ"":""JWT""}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(mockPayloadJson)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string mockToken = $"{base64Header}.{base64Payload}.mock_signature";

            // Act
            var result = await service.ValidateLtiLaunchAsync(new LtiLaunchPayloadDto { IdToken = mockToken });

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("canvas_student_99", result.UserId);
            Assert.Equal("student@stanford.edu", result.UserEmail);
            Assert.Equal("exam_fin_2026", result.CustomExamId);
            Assert.Equal("https://canvas.instructure.com/api/lti/courses/1/line_items/5", result.AgsLineItemUrl);
            Assert.Equal("/quiz/secure?examId=exam_fin_2026", result.RedirectAppUrl);
        }
    }
}
