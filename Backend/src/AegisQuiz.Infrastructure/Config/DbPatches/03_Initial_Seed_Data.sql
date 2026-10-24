-- ============================================================================
-- 03_Initial_Seed_Data.sql
-- Mục đích: Khởi tạo dữ liệu hạt giống ban đầu cho DynamicDomains, 
--           AiModelRegistries và UniversalAiCouncils.
-- Đặc tính: Idempotent với ON CONFLICT DO NOTHING.
-- ============================================================================

-- 1. Nạp các ngành chuẩn DynamicDomains
INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
VALUES 
  ('EDUCATION', '#2563eb', now(), 'Khảo thí đại học, phổ thông, học thuật tổng quát', 1, 'GraduationCap', TRUE, TRUE, 'Giáo dục & Học thuật', NULL, NULL),
  ('BANKING', '#059669', now(), 'Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro', 2, 'Landmark', TRUE, TRUE, 'Tài chính - Ngân hàng', NULL, NULL),
  ('HEALTHCARE', '#dc2626', now(), 'Y học, dược lâm sàng, quy trình điều dưỡng, kiểm soát nhiễm khuẩn', 3, 'HeartPulse', TRUE, TRUE, 'Y tế - Sức khỏe', NULL, NULL),
  ('HSE', '#d97706', now(), 'An toàn vệ sinh lao động, PCCC, quy chuẩn ISO 45001', 4, 'HardHat', TRUE, TRUE, 'An toàn - Môi trường LĐ', NULL, NULL),
  ('GOV_DRIVING', '#7c3aed', now(), 'Bộ 600 câu GPLX Bộ GTVT, 60 câu điểm liệt, sa hình AI', 5, 'Car', TRUE, TRUE, 'Sát hạch Lái xe Quốc gia', NULL, NULL),
  ('IT_SECURITY', '#0284c7', now(), 'Bảo mật an ninh mạng, kiến trúc hệ thống, chứng chỉ CISSP/CompTIA', 6, 'ShieldCheck', TRUE, TRUE, 'An toàn TT & CNTT', NULL, NULL),
  ('GENERAL', '#4b5563', now(), 'Kiến thức đại cương, kỹ năng mềm, văn hóa doanh nghiệp', 7, 'Layers', TRUE, TRUE, 'Tổng hợp / Đại cương', NULL, NULL)
ON CONFLICT ("Code") DO NOTHING;

-- 2. Hạt giống mô hình AI ban đầu (AiModelRegistries)
INSERT INTO "AiModelRegistries" ("Id", "ModelCode", "DisplayName", "ProviderCompany", "ProtocolType", "CognitiveArchetype", "EndpointUrl", "ModelName", "MaxContextTokens", "CostInputPerMillion", "CostOutputPerMillion", "BenchmarkElo", "AvgLatencyMs", "IsEnabled", "IsDefaultInArchetype", "TenantId", "CreatedAt")
VALUES 
  ('a0000001-0000-0000-0000-000000000001', 'deepseek-r1', 'DeepSeek R1 Reasoning (CoT)', 'DeepSeek', 1, 1, 'https://api.deepseek.com/v1', 'deepseek-reasoner', 128000, 0.14, 0.28, 1340, 450, true, true, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000002', 'gemini-2.0-flash', 'Google Gemini 2.0 Flash (1M Tokens)', 'Google', 2, 2, 'https://generativelanguage.googleapis.com/v1beta', 'gemini-2.0-flash', 1048576, 0.10, 0.40, 1310, 220, true, true, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000003', 'claude-3-5-sonnet', 'Anthropic Claude 3.5 Sonnet (Pedagogy & Arbiter)', 'Anthropic', 3, 3, 'https://api.anthropic.com/v1', 'claude-3-5-sonnet-20241022', 200000, 3.00, 15.00, 1360, 600, true, true, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000004', 'groq-llama-3-3-70b', 'Groq LPU Llama 3.3 70B Versatile', 'Groq', 1, 4, 'https://api.groq.com/openai/v1', 'llama-3.3-70b-versatile', 128000, 0.59, 0.79, 1280, 180, true, true, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000005', 'ollama-qwen-local', 'Local Sovereign Qwen 2.5 32B (On-Premise)', 'Ollama', 1, 5, 'http://localhost:11434/v1', 'qwen2.5:32b', 32768, 0.0, 0.0, 1260, 300, true, true, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000006', 'sambanova-llama-3-1-405b', 'SambaNova SN40L Llama 3.1 405B Frontier', 'SambaNova', 1, 1, 'https://api.sambanova.ai/v1', 'Meta-Llama-3.1-405B-Instruct', 128000, 5.00, 10.00, 1370, 350, true, false, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000007', 'cerebras-llama-3-3-70b', 'Cerebras Wafer-Scale Llama 3.3 70B (2000+ t/s)', 'Cerebras', 1, 4, 'https://api.cerebras.ai/v1', 'llama-3.3-70b', 128000, 0.60, 0.60, 1290, 80, true, false, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000008', 'mistral-large-latest', 'Mistral Large 2411 Sovereign Frontier', 'Mistral', 1, 3, 'https://api.mistral.ai/v1', 'mistral-large-latest', 128000, 2.00, 6.00, 1330, 400, true, false, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000009', 'github-gpt-4o', 'GitHub Models GPT-4o Gateway', 'GitHub', 1, 3, 'https://models.inference.ai.azure.com', 'gpt-4o', 128000, 2.50, 10.00, 1350, 450, true, false, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000010', 'openai-gpt-4o-mini', 'OpenAI GPT-4o-mini High-Throughput', 'OpenAI', 1, 4, 'https://api.openai.com/v1', 'gpt-4o-mini', 128000, 0.15, 0.60, 1290, 250, true, false, '00000000-0000-0000-0000-000000000000', now()),
  ('a0000001-0000-0000-0000-000000000011', 'replicate-musicgen', 'Replicate MusicGen Multimodal Audio', 'Replicate', 1, 2, 'https://api.replicate.com/v1', 'facebook/musicgen-small', 32000, 0.05, 0.10, 1200, 800, true, false, '00000000-0000-0000-0000-000000000000', now())
ON CONFLICT ("ModelCode") DO NOTHING;

-- 3. Hạt giống 6 Viện Hàn Lâm Nhận Thức Vũ Trụ (UniversalAiCouncils)
INSERT INTO "UniversalAiCouncils" ("Id", "CouncilCode", "DisplayName", "DisciplineArea", "MaterializedTaxonomyPath", "EpistemicManifesto", "BenchmarkPrestigeElo", "IsActive", "CreatedAt")
VALUES
  ('c0000001-0000-0000-0000-000000000001', 'COSMOLOGY', 'Viện Vũ Trụ & Vật Lý Lượng Tử', 'Vật lý lý thuyết & Lượng tử', '/UNIVERSE/PHYSICS/QUANTUM', 'Thực tại vật lý vận hành bởi trường lượng tử và hình học không-thời gian phi tuyến.', 2950, true, now()),
  ('c0000001-0000-0000-0000-000000000002', 'MATHEMATICS', 'Viện Toán Học & Logic Hình Thức', 'Toán học thuần túy & Độ phức tạp', '/UNIVERSE/MATHEMATICS/LOGIC', 'Chân lý tuyệt đối chỉ tồn tại qua phép suy diễn hình thức không mâu thuẫn.', 2980, true, now()),
  ('c0000001-0000-0000-0000-000000000003', 'PHILOSOPHY', 'Viện Triết Học, Nhân Văn & Ý Thức', 'Bản thể luận, Đạo đức & Ý nghĩa', '/HUMANITY/PHILOSOPHY/EPISTEMOLOGY', 'Mọi tri thức khoa học phải được soi chiếu qua phẩm giá con người và ý nghĩa tồn tại.', 2900, true, now()),
  ('c0000001-0000-0000-0000-000000000004', 'AGI_CYBERNETICS', 'Viện Siêu Trí Tuệ & Điều Khiển Học', 'Kiến trúc AGI & Căn chỉnh nhân loại', '/AI/SUPERINTELLIGENCE/ALIGNMENT', 'Sự xuất hiện của Siêu trí tuệ là bước tiến hóa tất yếu của thông tin vũ trụ.', 2990, true, now()),
  ('c0000001-0000-0000-0000-000000000005', 'ECONOMICS', 'Viện Kinh Tế Chính Trị & Địa Chiến Lược', 'Kinh tế vĩ mô & Lý thuyết trò chơi', '/SOCIETY/ECONOMICS/GEOPOLITICS', 'Dòng chảy giá trị và phân bổ tài nguyên quyết định sự hưng thịnh của nền văn minh.', 2850, true, now()),
  ('c0000001-0000-0000-0000-000000000006', 'LIFE_SCIENCES', 'Viện Khoa Học Sự Sống & Sinh Quyển', 'Sinh học phân tử & Sinh thái học', '/NATURE/BIOLOGY/EVOLUTION', 'Sự sống là cấu trúc tiêu tán vi mô chống lại entropy nhiệt động lực học.', 2880, true, now())
ON CONFLICT ("CouncilCode") DO NOTHING;
