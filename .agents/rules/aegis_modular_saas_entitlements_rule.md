# 🛡️ AEGISQUIZ — LUẬT BẮT BUỘC VỀ ĐÓNG GÓI B2B SAAS & PHÂN QUYỀN 4 TẦNG (ENTITLEMENTS RULE)

> **Phạm vi áp dụng**: Mọi kỹ sư phần mềm, AI Agent và tổng công trình sư khi thiết kế, triển khai tính năng mới hoặc chỉnh sửa giao diện Frontend/API Backend của AegisQuiz.  
> **Tài liệu nền tảng**: [`docs/architecture/AEGIS_MODULAR_B2B_SAAS_ENTITLEMENT_ARCHITECTURE.md`](file:///docs/architecture/AEGIS_MODULAR_B2B_SAAS_ENTITLEMENT_ARCHITECTURE.md).

---

## I. NGUYÊN TẮC BẤT BIẾN: CẤM TÍNH NĂNG CÀO BẰNG (ZERO UNGATED FEATURES)

1. **Tuyệt đối không hiển thị tính năng lộ thiên (No Naked Features)**:
   * Mọi tính năng mang tính nghiệp vụ, công nghệ cao (AI, Khảo thí, Xuất bản file, Đấu trường, Quản trị tổ chức) **BẮT BUỘC** phải được định danh bằng một `FeatureKey` duy nhất.
   * CẤM viết nút bấm, menu item hoặc router page mà không gắn kèm cơ chế kiểm tra bản quyền gói (`FeatureGate` hoặc `useEntitlement`).

2. **Tôn trọng Tuyệt Đối Ma Trận 4 Tầng**:
   $$\mathbf{Access} = \mathbf{Tenant} \;\cap\; \mathbf{Domain} \;\cap\; \mathbf{Group} \;\cap\; \mathbf{User}$$
   * **Tenant**: Kiểm tra gói thuê bao (`Starter` | `Professional` | `Enterprise` | `Global`) và Quotas.
   * **Domain**: Kiểm tra lĩnh vực tri thức của Tenant (`general`, `education`, `banking`, `medical`).
   * **Group (OrgUnit)**: Kiểm tra phân cấp quản lý theo đường dẫn $O(1)$ `HierarchyPath`.
   * **User**: Kiểm tra quyền hành động cụ thể (`capabilities`).

3. **Product-Led Growth (PLG) over Blind Blocking**:
   * Khi người dùng thuộc gói thấp chạm vào tính năng thuộc gói cao hơn, **KHÔNG ĐƯỢC** trả về lỗi 403 cộc cằn hoặc làm biến mất tính năng không giải thích.
   * Bắt buộc hiển thị theo phong cách **Teaser UI**: Có huy hiệu `<FeatureBadge />` lấp lánh và khi người dùng click vào sẽ tự động kích hoạt `<PaywallModal />` để giới thiệu giá trị nâng cấp gói dịch vụ.

---

## II. CHECKLIST 5 BƯỚC BẮT BUỘC KHI THÊM TÍNH NĂNG MỚI

Bất kỳ PR hoặc commit nào thêm tính năng mới mà bỏ qua 1 trong 5 bước sau sẽ bị **TỪ CHỐI DUYỆT (Hard Reject)**:

- [ ] **Bước 1 (Backend Domain)**: Khai báo hằng số trong `AegisQuiz.Domain.Entities.FeatureKeys`.
- [ ] **Bước 2 (Backend Catalog)**: Khai báo gói mặc định được hưởng tính năng trong `AegisQuiz.Domain.Entities.PlanFeatureCatalog`.
- [ ] **Bước 3 (Frontend Types)**: Đồng bộ mã tính năng vào `Frontend/src/types/entitlement.ts` và bổ sung bản ghi vào `FEATURE_METADATA_CATALOG` (Tên, mô tả, gói yêu cầu, badge).
- [ ] **Bước 4 (Frontend UI)**: Bọc component hoặc nút bấm bằng `<FeatureGate feature={...}>`. Nếu là menu Sidebar, khai báo thuộc tính `feature: FeatureKey...`.
- [ ] **Bước 5 (Unit Testing)**: Bổ sung ít nhất 1 test case trong `EnterpriseEntitlementMatrixTests.cs` để xác nhận tính năng đã được phân quyền chính xác theo gói.

---

## III. QUY CHUẨN THIẾT KẾ GIAO DIỆN FRONTEND CHO TÍNH NĂNG ĐÓNG GÓI

1. **Giới hạn kích thước Component UI**:
   * File component UI không được vượt quá **250 dòng code/file** (tuân thủ nghiêm ngặt Luật Zero Monolith).
   * Phân rã Paywall, Tooltip, Modals thành các micro-components độc lập.
2. **Hiệu ứng & Thị giác (Aesthetics)**:
   * Huy hiệu `PRO`: Nền gradient vàng hổ phách (`from-amber-500 via-orange-500 to-amber-600`), viền sáng nhẹ.
   * Huy hiệu `ENTERPRISE`: Nền gradient tím hoàng gia (`from-indigo-600 via-purple-600 to-pink-600`).
   * Huy hiệu `GLOBAL`: Nền gradient xanh ngọc lam (`from-cyan-500 via-blue-600 to-indigo-700`).
   * Luôn có chấm sáng nhấp nháy `animate-pulse` để tăng tính kích thích thị giác.
3. **Zero Layout Shift (CLS = 0)**:
   * Không để việc tải trạng thái entitlements làm nhảy layout của Sidebar hay trang nội dung. Cung cấp fallback skeleton hoặc trạng thái giữ chỗ cố định.
