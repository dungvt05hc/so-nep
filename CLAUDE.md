# CLAUDE.md — Hệ thống theo dõi nề nếp THCS

File này là nguồn sự thật cho toàn dự án. Đọc kỹ trước khi viết bất kỳ dòng code nào.

---

## 1. Bối cảnh

Web app thay thế sổ theo dõi nề nếp giấy của một trường THCS Việt Nam.
Quy mô: 62 lớp, 45 học sinh/lớp, khoảng 2.790 học sinh, 255 tài khoản.
Ngân sách tối thiểu, chạy trên một VPS. Deadline linh hoạt, ưu tiên đúng hơn nhanh.

Luồng chính: cán bộ lớp ghi lỗi bằng điện thoại → GVCN duyệt → hệ thống tự chốt
tuần vào Chủ nhật và tính điểm → phụ huynh xem báo cáo qua link + mã 6 số.

Vai trò: `ADMIN` (quản trị trường), `BGH` (chỉ đọc toàn trường), `GVCN`
(toàn quyền trên lớp mình), `LOP_TRUONG` / `PHO_HOC_TAP` / `PHO_LAO_DONG`
(chỉ ghi lỗi, chờ duyệt), phụ huynh (không có tài khoản).

Tài liệu nghiệp vụ đầy đủ: `docs/PLAN-v2.md`.
Thiết kế dữ liệu gốc: `docs/schema.prisma` (xem mục 3).
Bản vẽ giao diện: `docs/wireframes.html`.

---

## 2. Bảy nguyên tắc không được vi phạm

Đây là những chỗ mà làm sai sẽ hỏng dữ liệu của 2.790 học sinh, và thường chỉ
phát hiện ra sau vài tháng.

**1. Không hardcode con số của quy chế.**
Điểm gốc 100, ngưỡng xếp loại 90/60/30, ngưỡng hạ bậc 10/5/3 lần — tất cả nằm
trong bảng `SchoolSettings`, `ClassificationLevels`, `ConductRules`. Nếu bạn gõ
một con số nghiệp vụ vào file `.cs`, bạn đã làm sai.

**2. Snapshot điểm tại thời điểm ghi nhận.**
Mỗi `ViolationRecord` lưu `TypeCodeSnapshot`, `TypeNameSnapshot`,
`PointsSnapshot`. Sửa bảng điểm không bao giờ được làm đổi báo cáo đã phát hành
cho phụ huynh.

**3. App KHÔNG tự hạ bậc hạnh kiểm.**
Rule engine chỉ phát hiện và sinh `ConductAlert` để nhắc. Việc hạ bậc do cuộc
họp BGH – GVCN – phụ huynh quyết định, ghi vào `ConductAdjustment` kèm số biên
bản. Không có code nào được tự sửa `FinalLevelId` dựa trên quy tắc.

**4. Mẫu số của điểm học kỳ là số tuần ĐÃ CHỐT, không phải tổng số tuần kỳ.**
Xem điểm ở tuần 5 mà chia cho 18 thì mọi học sinh đều ra "Chưa đạt".

**5. Chỉ lỗi `APPROVED` mới tính điểm.**
`PENDING`, `REJECTED`, `EXPIRED` không bao giờ được cộng vào bất kỳ phép tính nào.

**6. Soft delete toàn bộ dữ liệu nghiệp vụ.**
Không có `DELETE FROM` nào trên bảng nghiệp vụ. Dùng `DeletedAt` + global query filter.

**7. Mọi thao tác ghi phải vào audit log.**
Qua `AuditSaveChangesInterceptor`, không phải gọi thủ công ở từng service.
`AuditLogs` là append-only: không update, không delete, không soft delete.

---

## 3. Về file schema.prisma

`docs/schema.prisma` là **tài liệu thiết kế**, không phải code chạy được trong
dự án này. Nó được viết khi stack còn là Node + Prisma. Stack đã đổi sang .NET,
nên nhiệm vụ ở Phase 0 là **port nó sang EF Core entities**, giữ nguyên 100%
ngữ nghĩa: tên bảng, tên cột, kiểu dữ liệu, ràng buộc unique, index, và toàn bộ
comment nghiệp vụ (chuyển thành XML doc comment tiếng Anh — xem mục 8).

Đừng thiết kế lại. Mọi quyết định trong file đó đều đã được chốt với nhà trường.

### Bảng quy đổi Prisma → EF Core + Npgsql

| Prisma | EF Core / Npgsql |
|---|---|
| `enum Role { ... }` | C# enum + `builder.HasPostgresEnum<Role>()` + `dataSourceBuilder.MapEnum<Role>()` |
| `String[]` | `List<string>` → `text[]`, Npgsql hỗ trợ sẵn |
| `Int[]` | `List<int>` → `integer[]` |
| `Json?` | `JsonDocument?` với `.HasColumnType("jsonb")` |
| `DateTime @db.Date` | `DateOnly` |
| `DateTime` (thời điểm) | `DateTimeOffset`, cột `timestamptz` |
| `Decimal @db.Decimal(8,2)` | `decimal` + `.HasPrecision(8, 2)` |
| `BigInt @id` | `long` |
| `@default(now())` | `.HasDefaultValueSql("now()")` |
| `@updatedAt` | Xử lý trong `SaveChangesInterceptor`, không có sẵn trong EF |
| `@@unique([a, b])` | `.HasIndex(x => new { x.A, x.B }).IsUnique()` |
| `@@map("ten_bang")` | `.ToTable("ten_bang")`, giữ nguyên snake_case |
| `deletedAt` | `.HasQueryFilter(x => x.DeletedAt == null)` |

Đặt tên cột snake_case bằng `UseSnakeCaseNamingConvention()` (gói
`EFCore.NamingConventions`) để khớp với `@@map` trong schema gốc.

---

## 4. Múi giờ — cái bẫy lớn nhất của stack này

Nghiệp vụ chạy theo giờ Việt Nam: tuần học từ thứ Hai đến Chủ nhật, chốt tuần
Chủ nhật 20:00 giờ VN, hạn khắc phục tính theo ngày VN.

Quy tắc:
- Lưu mọi mốc thời gian dưới dạng `timestamptz` (UTC) trong database.
- Ngày nghiệp vụ (`OccurredDate`, `StartDate`, `RemediationDeadline`) dùng
  `DateOnly`, không dính múi giờ.
- Mọi phép quy đổi "bây giờ là ngày nào, thuộc tuần nào" phải đi qua
  `TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")`.
- Không bao giờ dùng `DateTime.Now`. Chỉ dùng `TimeProvider` được inject, để
  test tua được thời gian.

Npgsql rất nghiêm về `DateTime.Kind`. Dùng `DateTimeOffset` sẽ tránh được cả lớp lỗi này.

---

## 5. Cấu trúc solution

```
NeNep.sln
src/
  NeNep.Domain/          Entity, enum, hằng số nghiệp vụ. KHÔNG tham chiếu gói nào khác.
  NeNep.Scoring/         Scoring engine + Rule engine. THUẦN HÀM, không EF, không DI,
                         không I/O. Nhận vào record + config, trả ra kết quả.
  NeNep.Infrastructure/  DbContext, cấu hình entity, migration, interceptor, repository.
  NeNep.Api/             ASP.NET Core. Minimal API gom theo tính năng.
  NeNep.Web/             React + Vite + TypeScript.
tests/
  NeNep.Scoring.Tests/   xUnit. Bộ test đối chiếu bảng tính tay của lớp 9/1.
  NeNep.Api.Tests/       Integration test với Testcontainers (Postgres thật).
docs/
  PLAN-v2.md  schema.prisma  wireframes.html  design-system/
```

`NeNep.Scoring` tách riêng là quyết định quan trọng nhất về kiến trúc. Đây là
phần dễ sai nhất và phải test được mà không cần database.

---

## 6. Thư viện được dùng

Backend: .NET 10, EF Core 10 + Npgsql, `EFCore.NamingConventions`,
FluentValidation, Serilog, Hangfire (+ Hangfire.PostgreSql) cho job chốt tuần,
`Microsoft.AspNetCore.Identity` chỉ để lấy `PasswordHasher<T>`, JWT Bearer cho auth,
OpenAPI có sẵn trong .NET 10.

Frontend: React 19 + Vite + TypeScript, TanStack Query, React Router,
Tailwind CSS, shadcn/ui, react-hook-form + zod, `vite-plugin-pwa`,
`openapi-typescript` sinh type từ OpenAPI của API.

Test: xUnit, FluentAssertions, Testcontainers.

Không dùng: AutoMapper (viết mapping tay), MediatR (chưa cần cho quy mô này),
ASP.NET Core Identity đầy đủ (quá nặng cho 255 tài khoản do GVCN cấp thủ công),
localStorage cho token nhạy cảm.

---

## 7. Giao diện

Dự án đã cài skill `ui-ux-pro-max`. Design system đã sinh sẵn và commit tại
`docs/design-system/MASTER.md`. **Đọc file đó trước mỗi phiên làm UI.** Nếu có
file `docs/design-system/pages/<ten-trang>.md` thì luật trong đó ưu tiên hơn.

Ràng buộc riêng của dự án này, ưu tiên cao hơn mọi gợi ý thẩm mỹ:

- Người dùng chính là giáo viên 40–55 tuổi và học sinh lớp 6–9 dùng điện thoại
  Android phổ thông. Ưu tiên tương phản cao, chữ lớn, vùng chạm ≥ 44px.
- Không glassmorphism, không neumorphism, không gradient tím/hồng, không dark mode
  mặc định. Đây là công cụ hành chính nhà trường, không phải sản phẩm SaaS.
- Toàn bộ nhãn, thông báo, nút bấm bằng tiếng Việt có dấu. Font phải hỗ trợ
  đầy đủ dấu tiếng Việt (Be Vietnam Pro, Inter, Roboto đều được).
- Mã lỗi (N01, C07) và điểm số dùng font mono để dễ quét mắt.
- Màn hình "Nhập lỗi nhanh" phải hoàn thành một lỗi trong ≤ 15 giây trên điện thoại.
  Đây là ràng buộc nghiệm thu, không phải mong muốn.

---

## 8. Quy ước code

- **Toàn bộ code và comment viết bằng tiếng Anh.** Tên lớp, tên biến, tên hàm,
  XML doc comment, thông báo lỗi dành cho lập trình viên — tất cả tiếng Anh.
- Ngoại lệ, vẫn giữ tiếng Việt: (a) nhãn/thông báo hiển thị cho người dùng
  (xem mục 7); (b) giá trị dữ liệu và ví dụ dữ liệu trong comment
  (`"Chưa đạt"`, `"Nghỉ Tết"`, `"Khối 6"`); (c) nhãn enum và tên bảng/cột đã chốt
  trong `schema.prisma` (`GVCN`, `LOP_TRUONG`, `KHEN_THUONG`) — đây là dữ liệu,
  đổi là hỏng. Khi giữ tiếng Việt trong comment, ghi rõ lý do ngay tại chỗ.
- Tên bảng và cột giữ snake_case đúng như `schema.prisma`.
- Endpoint gom theo tính năng: `Features/Violations/ApproveViolation.cs`.
- DTO tách riêng entity. Không bao giờ trả entity trực tiếp ra API.
- Mọi query đọc dữ liệu lớp phải lọc theo quyền ở tầng middleware/filter,
  không tin `classId` client gửi lên.
- Migration đặt tên có nghĩa: `AddConductAlertAndAdjustment`, không phải `Migration1`.

---

## 9. Khi nào phải dừng lại và hỏi

Dừng, hỏi tôi, không tự quyết:

- Cần thêm/bớt/đổi một cột trong schema so với `docs/schema.prisma`.
- Gặp mâu thuẫn giữa `PLAN-v2.md` và `schema.prisma`.
- Một quy tắc nghiệp vụ trong quy chế có nhiều cách hiểu.
- Cần thêm một thư viện chưa có ở mục 6.
- Định dùng `DateTime.Now`, `DELETE FROM` trên bảng nghiệp vụ, hoặc hardcode
  một con số của quy chế — nghĩa là bạn đang chuẩn bị vi phạm mục 2.
