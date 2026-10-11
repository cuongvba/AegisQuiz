-- ==============================================================================
-- CẬP NHẬT TỔ TRỌNG TÀI ĐỘC LẬP & GIÁM KHẢO CHẤM MÙ (SYS_ARBITRATION)
-- Đảm bảo đủ chân TẤT CẢ các họ Sói trong hệ thống để thực hiện Chấm Mù Độc Lập Tuyệt Đối
-- ==============================================================================

DO $$
DECLARE
    v_council_id UUID;
BEGIN
    -- 1. Tìm hoặc tạo Viện SYS_ARBITRATION
    SELECT "Id" INTO v_council_id FROM "UniversalAiCouncils" WHERE "CouncilCode" = 'SYS_ARBITRATION';

    IF v_council_id IS NULL THEN
        v_council_id := 'a2000000-0000-0000-0000-000000000002'::UUID;
        INSERT INTO "UniversalAiCouncils" (
            "Id", "CouncilCode", "DisplayName", "Category", "DisciplineArea",
            "MaterializedTaxonomyPath", "EpistemicManifesto", "TriggerKeywordsJson",
            "BenchmarkPrestigeElo", "IsActive", "SpatioTemporalAffinity", "CreatedAt"
        ) VALUES (
            v_council_id, 'SYS_ARBITRATION', 'Tổ Trọng Tài Độc Lập & Giám Khảo Chấm Mù', 3,
            'Đánh giá ma trận Rubric 5 trục, Kiểm định Popperian & Thẩm định khách quan',
            '/SYSTEM/ARBITRATION/EVALUATION',
            'Sự công tâm tuyệt đối chỉ đạt được khi người phán xét hoàn toàn độc lập với các bên tranh luận.',
            '[]', 3000, true, 'Toàn Cầu Thực Chứng & Siêu Hình Học', NOW()
        );
    END IF;

    -- 2. Xóa các ghế cũ để nạp mới 4 ghế chuẩn mực
    DELETE FROM "UniversalCouncilSeats" WHERE "CouncilId" = v_council_id;

    -- 3. Nạp 4 Ghế chuyên môn hóa đầy đủ chân tất cả các Sói hệ thống:
    -- Ghế 1: Chánh Trọng Tài Độc Lập Chấm Mù (President)
    INSERT INTO "UniversalCouncilSeats" (
        "Id", "CouncilId", "SeatTitle", "Role", "IdealArchetype",
        "PreferredModelCode", "AssignedModelCode", "AssignmentRationale", "AlphaModelCode",
        "SwarmModelCodesJson", "SwarmPolicy", "IsReady", "DecisionWeight", "SortOrder",
        "PersonaPromptDirective"
    ) VALUES (
        'b2000000-0000-0000-0000-000000000001'::UUID, v_council_id,
        'Chánh Trọng Tài Độc Lập Chấm Mù', 1, 1,
        'deepseek-r1', '', '', 'deepseek-r1',
        '["deepseek-r1", "gemini-2.0-flash", "claude-3-5-sonnet", "groq-llama-3-3-70b", "ollama-qwen-local", "gpt-4o"]',
        'CASCADE_FAILOVER', true, 1.0, 1,
        'Chánh Trọng Tài tối cao: Thẩm định Trục 1 (Logical Rigor) và tính toán Chỉ số Tiến Hóa Nhận Thức (EEV) tổng thể của phiên tranh luận.'
    );

    -- Ghế 2: Trọng Tài Kiểm Chứng Thực Nghiệm & Sổ Cái Bằng Chứng (EmpiricalAuditor)
    INSERT INTO "UniversalCouncilSeats" (
        "Id", "CouncilId", "SeatTitle", "Role", "IdealArchetype",
        "PreferredModelCode", "AssignedModelCode", "AssignmentRationale", "AlphaModelCode",
        "SwarmModelCodesJson", "SwarmPolicy", "IsReady", "DecisionWeight", "SortOrder",
        "PersonaPromptDirective"
    ) VALUES (
        'b2000000-0000-0000-0000-000000000002'::UUID, v_council_id,
        'Trọng Tài Kiểm Chứng Thực Nghiệm & Sổ Cái Bằng Chứng', 6, 2,
        'gemini-2.0-flash', '', '', 'gemini-2.0-flash',
        '["gemini-2.0-flash", "deepseek-r1", "claude-3-5-sonnet", "groq-llama-3-3-70b", "ollama-qwen-local", "gpt-4o"]',
        'CASCADE_FAILOVER', true, 1.0, 2,
        'Thẩm định khắt khe Trục 2 (Empirical Falsifiability) và Trục 4 (Pragmatic Actionability) qua việc đối chiếu từng bằng chứng mã hóa [E1..En] trong Sổ Cái Thực Địa.'
    );

    -- Ghế 3: Trọng Tài Phản Biện Đối Kháng & Giải Bẫy Nhận Thức (AdversarialCritic)
    INSERT INTO "UniversalCouncilSeats" (
        "Id", "CouncilId", "SeatTitle", "Role", "IdealArchetype",
        "PreferredModelCode", "AssignedModelCode", "AssignmentRationale", "AlphaModelCode",
        "SwarmModelCodesJson", "SwarmPolicy", "IsReady", "DecisionWeight", "SortOrder",
        "PersonaPromptDirective"
    ) VALUES (
        'b2000000-0000-0000-0000-000000000003'::UUID, v_council_id,
        'Trọng Tài Phản Biện Đối Kháng & Giải Bẫy Nhận Thức', 2, 3,
        'claude-3-5-sonnet', '', '', 'claude-3-5-sonnet',
        '["claude-3-5-sonnet", "deepseek-r1", "gemini-2.0-flash", "groq-llama-3-3-70b", "ollama-qwen-local", "gpt-4o"]',
        'CASCADE_FAILOVER', true, 1.0, 3,
        'Thẩm định Trục 3 (Ontological Depth) và Trục 5 (Trap Resistance) qua Tiêu chuẩn Phản nghiệm Popperian, vạch trần các bẫy ngụy biện và thiên kiến xác nhận.'
    );

    -- Ghế 4: Bồi Thẩm Đoàn Đa Mô Hình Đồng Thuận (Synthesizer)
    INSERT INTO "UniversalCouncilSeats" (
        "Id", "CouncilId", "SeatTitle", "Role", "IdealArchetype",
        "PreferredModelCode", "AssignedModelCode", "AssignmentRationale", "AlphaModelCode",
        "SwarmModelCodesJson", "SwarmPolicy", "IsReady", "DecisionWeight", "SortOrder",
        "PersonaPromptDirective"
    ) VALUES (
        'b2000000-0000-0000-0000-000000000004'::UUID, v_council_id,
        'Bồi Thẩm Đoàn Đa Mô Hình Đồng Thuận', 3, 2,
        'groq-llama-3-3-70b', '', '', 'groq-llama-3-3-70b',
        '["groq-llama-3-3-70b", "deepseek-r1", "gemini-2.0-flash", "claude-3-5-sonnet", "ollama-qwen-local", "gpt-4o"]',
        'CASCADE_FAILOVER', true, 1.0, 4,
        'Đóng vai trò Bồi thẩm đoàn tổng hợp, điều hòa sự dị biệt quan điểm giữa các họ mô hình AI để đưa ra kết luận trung dung khách quan nhất.'
    );

END $$;
