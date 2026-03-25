# Production Intelligence & Decision Support System (PIDSS)

<p align="right">
  🇺🇸 <a href="README.md">English</a>
  | 🇻🇳 <a href="README_VI.md">Tiếng Việt</a>
</p>

## 1. Tổng quan

**Production Intelligence & Decision Support System (PIDSS)** là một nền tảng số hỗ trợ **ra quyết định cho các bài toán tăng năng lực sản xuất trong điều kiện ràng buộc thực tế**, thông qua phân tích dữ liệu, mô phỏng và so sánh kịch bản.

Hệ thống giúp các đội ngũ sản xuất:
- Phát hiện bottleneck dựa trên dữ liệu
- Đánh giá các chiến lược tối ưu hóa (bao gồm automation và liên kết công đoạn)
- Đưa ra quyết định đầu tư và cải tiến dựa trên định lượng

PIDSS nằm **trên các hệ thống MES/ERP hiện hữu** và tập trung vào **đánh giá – so sánh – đề xuất**, không thực thi sản xuất.

> **PIDSS không phải MES, không phải hệ thống lập lịch, và không phải hệ thống điều khiển thời gian thực.**
> PIDSS là một **lớp Decision Support & Production Intelligence** dành cho planner, manager và digital/process engineer.

---

## 2. Bối cảnh nghiệp vụ

Các doanh nghiệp sản xuất hiện nay thường đặt ra mục tiêu chiến lược:

> **Tăng năng lực sản xuất 40–60% trong 3–5 năm,
> mà không tăng nhân sự và không mở rộng diện tích nhà xưởng.**

Trong khi đó, họ phải đối mặt với nhiều ràng buộc:
- Diện tích nhà xưởng có hạn
- Khó khăn trong tuyển dụng và giữ chân nhân công
- Chi phí vận hành ngày càng tăng
- Các khoản đầu tư automation (CAPEX) có giá trị lớn ($200K–$1M) và rủi ro cao

Trên thực tế, nhiều quyết định cải tiến và đầu tư hiện nay vẫn dựa trên:
- File Excel rời rạc
- Ước lượng cảm tính
- Giả định không nhất quán
- Khó so sánh khách quan giữa các phương án

Điều này dẫn đến **rủi ro đầu tư cao và độ tin cậy quyết định thấp**.

---

## 3. Bài toán nghiệp vụ cốt lõi

PIDSS được thiết kế để trả lời câu hỏi:

> **Làm thế nào để đánh giá và so sánh các phương án tối ưu hóa và automation
> trước khi đầu tư, trong điều kiện ràng buộc thực tế của nhà máy?**

Các câu hỏi nghiệp vụ chính bao gồm:
- Bottleneck thực sự nằm ở đâu (line / stage / flow)?
- Yếu tố nào gây mất hiệu suất chính (lao động, downtime, chất lượng, mất cân bằng)?
- KPI sẽ thay đổi thế nào nếu áp dụng các phương án khác nhau?
- Đầu tư automation $500K có đáng không?
- Thời gian hoàn vốn (payback) là bao lâu?
- ROI trong 3–5 năm là bao nhiêu?
- Phương án A hay B mang lại giá trị tốt hơn?

---

## 4. PIDSS làm gì

PIDSS cho phép các đội ngũ sản xuất:

- Thu thập và chuẩn hóa dữ liệu sản xuất (observed hoặc simulated)
- Định lượng và xếp hạng hiệu suất của stage và line
- Phát hiện bottleneck và nguyên nhân chính gây tổn thất
- Mô phỏng các kịch bản **what-if** ở mức aggregate (phục vụ quyết định)
- Đánh giá các chiến lược:
  - Tối ưu thủ công (Kaizen, cân bằng cycle time)
  - Bán tự động
  - Tự động hóa và liên kết công đoạn
- So sánh **baseline vs phương án cải tiến (A/B comparison)**
- Sinh recommendation có giải thích và định lượng tác động
- Lưu lại **lịch sử run dạng append-only, có thể audit và tái tạo**

---

## 5. PIDSS KHÔNG làm gì

Để giữ đúng ranh giới hệ thống, PIDSS **không**:

- Dispatch work order hoặc giao việc cho công nhân
- Theo dõi WIP, routing chi tiết, serial/lot
- Lập lịch sản xuất thời gian thực
- Điều khiển PLC, máy móc hoặc thiết bị automation
- Thay thế MES, ERP, SCADA

> **PIDSS hỗ trợ ra quyết định, không thực thi sản xuất.**

---

## 6. Đối tượng sử dụng

### Production / Plant Manager
- Theo dõi KPI và hiệu suất tổng thể
- Ưu tiên các sáng kiến cải tiến và automation
- Đánh giá trade-off giữa chi phí, năng lực và rủi ro

### Production Planner
- Đánh giá capacity so với demand
- So sánh các cấu hình line, ca và chiến lược sản xuất

### Process / Digital Engineer
- Phân tích cycle time, downtime, defect
- Thiết kế và kiểm chứng các kịch bản tối ưu hóa
- Hỗ trợ OE, Lean, Kaizen và chuyển đổi số

### Không hướng tới
- Operator
- Hệ thống thực thi thời gian thực
- Hệ thống điều khiển

---

## 7. Triết lý thiết kế & mô hình hóa

- **Decision-Centric**: Tập trung vào quyết định chiến lược và chiến thuật
- **Aggregate Modeling**: Đủ chi tiết để so sánh phương án, không đi xuống mức MES
- **Automation-Aware**: Automation là một lựa chọn chiến lược để đánh giá
- **Explainable**: KPI, ROI và rationale rõ ràng
- **Human-in-the-Loop**: Con người quyết định cuối cùng
- **Run-Based & Auditable**: Mọi phân tích đều có thể tái tạo và kiểm chứng

---

## 8. Giá trị kinh doanh

> **PIDSS giúp doanh nghiệp đánh giá các khoản đầu tư automation trị giá $500K
> trước khi chi tiền, bằng cách định lượng ROI và thời gian hoàn vốn (payback),
> thay vì chỉ dựa trên ước lượng cảm tính.**

Giá trị mang lại:
- Giảm rủi ro đầu tư sai
- Hỗ trợ quyết định CAPEX dựa trên dữ liệu
- So sánh phương án một cách nhất quán
- Tạo audit trail cho quyết định chiến lược
- Chuẩn hóa tri thức OE và automation thành hệ thống số

---

## 9. Phạm vi kỹ thuật (High-Level)

| Khu vực | Công nghệ |
| --- | --- |
| Backend Platform | ASP.NET Core (.NET) |
| Simulation Engine | C++ (aggregate digital twin) |
| Analytics & Optimization | Python |
| Database | SQL Server hoặc PostgreSQL |
| UI Client (Web) | React (TypeScript + Vite) |
| UI Client (Desktop) | WinForms (.NET) — tương lai |
| Data Contracts | JSON + JSON Schema |
| Kiến trúc | Run-based, append-only, versioned |

---

## 10. Khái niệm cốt lõi

### Scenario
**Scenario** mô tả một chiến lược sản xuất giả định, bao gồm:
- Cấu hình line và stage
- Chính sách nhân lực
- Giả định automation và liên kết công đoạn
- Tham số năng lực, diện tích và chi phí
- Random seed (tái tạo kết quả)
- `schema_version` (tương thích)

### Run
**Run** là một lần thực thi scenario:
- Định danh duy nhất bằng `run_id` (UUID)
- **Append-only** (không ghi đè kết quả)
- Sinh ra artifacts (dataset, log, report)
- Lưu KPI và recommendation

---

## 11. Kiến trúc tổng thể (High-Level)

```text
Machines / PLC / MES Export (Observed Data)
            │
            ▼
Data Platform (Ingestion → Feature Engineering → Calibration)
            │
            ▼
Platform Backend (.NET)
  - ScenarioBuilder (gộp user input + feature store + calibration)
  - Adapter (canonical authority — validation, versioning, stage weights)
  - Run Orchestration & Artifact Management
            │
            ├── Simulation Engine (C++)
            ├── Analytics Engine (Python)
            │
            ▼
KPIs & Recommendations (Database + Artifacts)
            │
            ▼
UI Client (React Web / WinForms Desktop)
```

---

## 12. Cấu trúc repository

```text
ProductionOptimization/
├─ data/
│  ├─ contracts/          # Payload ví dụ có phiên bản
│  ├─ schemas/            # Định nghĩa JSON Schema (Draft-07)
│  ├─ validation/         # Script và test validation
│  ├─ transforms/         # Định nghĩa biến đổi phân tích
│  ├─ lineage/            # Chính sách lineage artifact
│  └─ documentation/      # Mô hình domain, từ điển dữ liệu, tài liệu phiên bản
├─ platform/
│  └─ Pidss.Platform/     # ASP.NET Core — orchestration, adapter, API
├─ engines/
│  ├─ simulation/
│  │  └─ Pidss.Simulation/        # C++ CLI — engine mô phỏng tổng hợp
│  ├─ analytics/
│  │  └─ Pidss.Analytics/         # Python CLI — tính KPI & đề xuất
│  └─ optimization/
│     └─ Pidss.Optimization/      # Python CLI — khám phá scenario theo lô
├─ data_platform/
│  ├─ ingestion/
│  ├─ feature_engineering/
│  ├─ calibration/
│  └─ synthetic/mes/              # Sinh dữ liệu MES tổng hợp + API
├─ presentation/
│  ├─ web/
│  │  └─ Pidss.Web.React/         # React SPA — UI client chính
│  └─ desktop/
│     └─ Pidss.Desktop.Winforms/  # WinForms — desktop client tương lai
├─ data_storage/
│  ├─ feature_store/
│  ├─ calibration_store/
│  └─ model_store/
├─ artifacts/                     # Artifact run (append-only, gitignored)
└─ docs/                          # Tài liệu kiến trúc và dự án
```

---

## Lộ trình triển khai

- Phase 0 — Repository Foundation & Data-Layer Conventions
- Phase 1 — Domain & Canonical Model
- Phase 2 — Public Contracts & Schemas
- Phase 3 — Database & Run Metadata
- Phase 4 — Platform Core (ScenarioBuilder + Adapter + Orchestration)
- Phase 5 — C++ Simulation v1 (Aggregate Model)
- Phase 6 — Python Analytics v1
- Phase 7 — UI
- Phase 8 — Optimization Batch
- Phase 9 — ML-based Decision Intelligence
- Phase 10 — Observed Import (Optional / Future Extension)

## License

MIT License
