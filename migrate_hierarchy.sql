DROP TABLE IF EXISTS "DynamicDomains" CASCADE;
CREATE TABLE "DynamicDomains" (
    "Code" text PRIMARY KEY,
    "Name" text NOT NULL,
    "Description" text,
    "Icon" text NOT NULL DEFAULT 'AccountBalance',
    "ColorBadge" text NOT NULL DEFAULT '#1976d2',
    "IsSystemStandard" boolean NOT NULL DEFAULT true,
    "TenantId" uuid,
    "ParentDomainCode" text,
    "IsActive" boolean NOT NULL DEFAULT true,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now()
);

INSERT INTO "DynamicDomains" ("Code", "Name", "Description", "Icon", "ColorBadge", "IsSystemStandard", "IsActive", "DisplayOrder", "CreatedAt")
VALUES 
  ('BANKING', 'Ngân hàng & Tài chính Enterprise', 'Nghiệp vụ Tín dụng, Thanh toán quốc tế, Kế toán ngân hàng, Pháp chế & Tuân thủ', 'Landmark', '#0284c7', true, true, 1, now()),
  ('EDUCATION', 'Khảo thí & Giáo dục Quốc gia', 'Đề thi Tốt nghiệp THPT, Khảo sát năng lực Toán học, Ngoại ngữ, KHTN', 'GraduationCap', '#10b981', true, true, 2, now()),
  ('GOV_DRIVING', 'Sát hạch Lái xe Quốc gia (Cục Đường Bộ)', 'Bộ 600 câu luật giao thông đường bộ, 60 câu điểm liệt, sa hình và biển báo', 'Car', '#f59e0b', true, true, 3, now()),
  ('GENERAL', 'Chuyên đề Tổng hợp & Cộng đồng', 'Kiến thức xã hội, khoa học mở, kỹ năng mềm và câu hỏi đóng góp cộng đồng', 'Layers', '#64748b', true, true, 4, now())
ON CONFLICT ("Code") DO NOTHING;
