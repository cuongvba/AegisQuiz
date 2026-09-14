using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemMaintenanceController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<SystemMaintenanceController> _logger;

        public SystemMaintenanceController(AegisQuizDbContext db, ILogger<SystemMaintenanceController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// 1-Click Tự Chữa Lành Database (Non-destructive: Bổ sung ngay các cột thiếu như DepthLevel, MaterializedPath mà KHÔNG mất dữ liệu).
        /// Gọi qua: GET hoặc POST /api/system/auto-heal
        /// </summary>
        [HttpGet("auto-heal")]
        [HttpPost("auto-heal")]
        public async Task<IActionResult> AutoHealDatabase()
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync(@"
                    -- 1. BẢNG BankTopics
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DepthLevel"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""MaterializedPath"" text NULL;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Scope"" text NOT NULL DEFAULT 'COMMUNITY';
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'GENERAL';
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DisplayOrder"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""QuestionCountCached"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Urn"" text NULL;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""ParentId"" uuid NULL;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""AllowedTenantIds"" text[] NOT NULL DEFAULT '{}';
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

                    -- 2. BẢNG Questions
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""ContextId"" uuid NULL;
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'EDUCATION';
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""IsCritical"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""SubCategory"" text NULL;
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""Tags"" text[] NOT NULL DEFAULT '{}';
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

                    -- 3. BẢNG DynamicDomains
                    CREATE TABLE IF NOT EXISTS ""DynamicDomains"" (
                        ""Code"" text NOT NULL,
                        ""Name"" text NOT NULL,
                        ""Description"" text,
                        ""Icon"" text NOT NULL DEFAULT 'Layers',
                        ""ColorBadge"" text NOT NULL DEFAULT '#0284c7',
                        ""IsSystemStandard"" boolean NOT NULL DEFAULT true,
                        ""TenantId"" uuid NULL,
                        ""ParentDomainCode"" text NULL,
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""DisplayOrder"" integer NOT NULL DEFAULT 0,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        CONSTRAINT ""PK_DynamicDomains"" PRIMARY KEY (""Code"")
                    );

                    INSERT INTO ""DynamicDomains"" (""Code"", ""ColorBadge"", ""CreatedAt"", ""Description"", ""DisplayOrder"", ""Icon"", ""IsActive"", ""IsSystemStandard"", ""Name"", ""ParentDomainCode"", ""TenantId"")
                    VALUES 
                      ('EDUCATION', '#2563eb', now(), 'Khảo thí đại học, phổ thông, học thuật tổng quát', 1, 'GraduationCap', TRUE, TRUE, 'Giáo dục & Học thuật', NULL, NULL),
                      ('BANKING', '#059669', now(), 'Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro', 2, 'Landmark', TRUE, TRUE, 'Tài chính - Ngân hàng', NULL, NULL),
                      ('HEALTHCARE', '#dc2626', now(), 'Y học, dược lâm sàng, quy trình điều dưỡng, kiểm soát nhiễm khuẩn', 3, 'HeartPulse', TRUE, TRUE, 'Y tế - Sức khỏe', NULL, NULL),
                      ('HSE', '#d97706', now(), 'An toàn vệ sinh lao động, PCCC, quy chuẩn ISO 45001', 4, 'HardHat', TRUE, TRUE, 'An toàn - Môi trường LĐ', NULL, NULL),
                      ('GOV_DRIVING', '#7c3aed', now(), 'Bộ 600 câu GPLX Bộ GTVT, 60 câu điểm liệt, sa hình AI', 5, 'Car', TRUE, TRUE, 'Sát hạch Lái xe Quốc gia', NULL, NULL),
                      ('IT_SECURITY', '#0284c7', now(), 'Bảo mật an ninh mạng, kiến trúc hệ thống, chứng chỉ CISSP/CompTIA', 6, 'ShieldCheck', TRUE, TRUE, 'An toàn TT & CNTT', NULL, NULL),
                      ('GENERAL', '#4b5563', now(), 'Kiến thức đại cương, kỹ năng mềm, văn hóa doanh nghiệp', 7, 'Layers', TRUE, TRUE, 'Tổng hợp / Đại cương', NULL, NULL)
                    ON CONFLICT (""Code"") DO NOTHING;
                ");

                _logger.LogInformation("[AutoHeal] Database schema verified and healed successfully.");
                return Ok(new
                {
                    success = true,
                    message = "Cơ sở dữ liệu đã được tự động chữa lành thành công 100%! Cột DepthLevel và tất cả bảng mới đã sẵn sàng.",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AutoHeal] Failed to heal database.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tái tạo toàn bộ bảng theo chuẩn mới nhất 2026 (Xóa sạch bảng cũ và tạo lại từ đầu).
        /// Gọi qua: POST /api/system/recreate-tables?confirm=RESET_2026
        /// </summary>
        [HttpPost("recreate-tables")]
        public async Task<IActionResult> RecreateAllTables([FromQuery] string confirm = "")
        {
            if (confirm != "RESET_2026")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Để tái tạo toàn bộ cơ sở dữ liệu, vui lòng truyền tham số ?confirm=RESET_2026"
                });
            }

            try
            {
                string sqlPath = Path.Combine(AppContext.BaseDirectory, "RECREATE_ALL_TABLES_LATEST_2026.sql");
                if (!System.IO.File.Exists(sqlPath))
                {
                    sqlPath = Path.Combine(Directory.GetCurrentDirectory(), "RECREATE_ALL_TABLES_LATEST_2026.sql");
                }
                if (!System.IO.File.Exists(sqlPath))
                {
                    sqlPath = @"d:\Cuong\DuAn\mybank\AegisQuiz\RECREATE_ALL_TABLES_LATEST_2026.sql";
                }

                if (System.IO.File.Exists(sqlPath))
                {
                    string ddl = await System.IO.File.ReadAllTextAsync(sqlPath);
                    await _db.Database.ExecuteSqlRawAsync(ddl);
                    _logger.LogInformation("[RecreateTables] Full database recreation executed from script.");
                }
                else
                {
                    // Fallback: EnsureDeleted + EnsureCreated
                    await _db.Database.EnsureDeletedAsync();
                    await _db.Database.EnsureCreatedAsync();
                    _logger.LogInformation("[RecreateTables] Full database recreation executed via EnsureCreated.");
                }

                return Ok(new
                {
                    success = true,
                    message = "Đã xóa và tái tạo toàn bộ các bảng theo chuẩn mới nhất 2026 thành công 100%!",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RecreateTables] Failed to recreate tables.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
