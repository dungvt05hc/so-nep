# PROMPTS — chạy tuần tự với Claude Code

Cách dùng: mỗi phase là một phiên làm việc riêng. Chạy `/clear` giữa các phase.
Dán nguyên khối prompt vào Claude Code. Review và commit trước khi sang phase kế.

Chuẩn bị một lần, trước Phase 0:

```bash
mkdir ne-nep && cd ne-nep && git init
mkdir -p docs
# copy CLAUDE.md vào thư mục gốc
# copy PLAN-v2.md, schema.prisma, wireframes.html vào docs/

# cài skill giao diện
npm install -g ui-ux-pro-max-cli
uipro init --ai claude
```

---

## PHASE 0 — Nền móng và port schema

> Đọc `CLAUDE.md` và `docs/schema.prisma` trước khi làm gì.
>
> Mục tiêu: dựng solution .NET 10 và port toàn bộ 25 model từ `docs/schema.prisma`
> sang EF Core entities, chạy được migration đầu tiên trên Postgres.
>
> Việc cần làm:
> 1. Tạo solution theo đúng cấu trúc ở mục 5 của `CLAUDE.md`. Năm project trong
>    `src/`, hai project test trong `tests/`.
> 2. Port toàn bộ entity sang `NeNep.Domain`. Giữ nguyên tên bảng, tên cột, kiểu
>    dữ liệu, unique constraint, index. Chuyển mọi comment `///` trong file Prisma
>    thành XML doc comment tiếng Việt trên property tương ứng — các comment đó ghi
>    lại quyết định nghiệp vụ đã chốt với nhà trường, không được bỏ.
> 3. Cấu hình EF trong `NeNep.Infrastructure` bằng `IEntityTypeConfiguration<T>`
>    riêng cho từng entity, không nhét hết vào `OnModelCreating`. Dùng
>    `EFCore.NamingConventions` cho snake_case. Đăng ký toàn bộ Postgres enum.
> 4. Viết `AuditSaveChangesInterceptor` và `TimestampInterceptor` (xử lý `@updatedAt`).
>    Chưa cần đầy đủ, nhưng phải có khung.
> 5. Global query filter cho soft delete trên mọi entity có `DeletedAt`.
> 6. `docker-compose.yml` với Postgres 17 để dev.
> 7. Migration `Initial` và chạy được `dotnet ef database update`.
>
> Ràng buộc: không thiết kế lại schema. Nếu thấy chỗ nào trong `schema.prisma`
> có vẻ sai hoặc thiếu, dừng lại và hỏi tôi, đừng tự sửa.
>
> Xong khi: `dotnet build` sạch, `dotnet ef database update` chạy được, và
> `\d+` trong psql cho thấy đủ 25 bảng với đúng tên cột snake_case.

---

## PHASE 1 — Xác thực, phân quyền, cơ cấu tổ chức

> Mục tiêu: đăng nhập được, phân quyền đúng 6 vai trò, quản lý được năm học,
> lớp, học sinh, tài khoản.
>
> Việc cần làm:
> 1. Auth bằng JWT: access token 15 phút + refresh token lưu ở bảng `sessions`.
>    Băm mật khẩu bằng `PasswordHasher<T>`. Bắt đổi mật khẩu lần đầu
>    (`MustChangePasswrd`). Khoá tạm sau 5 lần sai.
>    Thu hồi tài khoản phải vô hiệu hoá phiên đang mở ngay lập tức.
> 2. Authorization policy theo vai trò và theo lớp. Viết một
>    `IClassAccessGuard` mà mọi endpoint chạm dữ liệu lớp đều phải đi qua.
>    Không tin `classId` từ client trong bất kỳ trường hợp nào.
> 3. CRUD năm học, học kỳ, tuần học, khối, lớp. Sinh 35 tuần từ ngày khai giảng.
> 4. Nhập danh sách học sinh từ Excel (dùng ClosedXML): tải file mẫu, xem trước,
>    báo lỗi theo từng dòng, rồi mới ghi. 62 lớp × 45 học sinh nên phải chịu được
>    file lớn và phải báo rõ dòng nào sai.
> 5. GVCN cấp / khoá / reset mật khẩu tài khoản cán bộ lớp. Gán chức vụ
>    (`ClassOfficer`) có thời hạn.
> 6. `SchoolBreak`: nhập kỳ nghỉ giữa năm và dời ngày các tuần **chưa chốt** về
>    sau, giữ nguyên `WeekNo`. Tuần đã chốt tuyệt đối không được đụng tới.
>
> Xong khi: có integration test chứng minh GVCN lớp 9/1 không đọc được dữ liệu
> lớp 9/2 qua bất kỳ endpoint nào, và thu hồi tài khoản làm token hiện tại
> hết hiệu lực ngay.

---

## PHASE 2 — Danh mục lỗi, nhập lỗi, duyệt lỗi

> Mục tiêu: hoàn thành vòng đời một bản ghi vi phạm, từ lúc cán bộ lớp nhập
> đến lúc GVCN duyệt, kèm audit log đầy đủ.
>
> Việc cần làm:
> 1. Seed 49 mã lỗi, 4 nhóm, 14 quy tắc, 4 bậc xếp loại. Dữ liệu gốc nằm trong
>    `docs/seed-data.ts` — port sang C# thành một class hằng số trong
>    `NeNep.Domain`, giữ nguyên toàn bộ giá trị và comment. Viết luôn hàm kiểm
>    tra toàn vẹn tương đương `validateSeedData()` và gọi nó trong test.
> 2. `ClassViolationOverride`: GVCN bật/tắt mã lỗi cho lớp mình. Chỉ được sửa
>    mức điểm khi `SchoolSettings.AllowClassPointOverride = true` (hiện là false).
> 3. API tạo bản ghi vi phạm. Bắt buộc: kiểm tra `AllowedRoles` của mã lỗi,
>    kiểm tra `MaxPerWeek`, chặn cán bộ lớp tự ghi điểm cộng cho chính mình,
>    đánh dấu `IsFlagged` khi cán bộ lớp ghi cho cán bộ lớp khác,
>    tự tính `RemediationDeadline` từ `RemediationDays`,
>    snapshot `TypeCode` / `TypeName` / `Points`.
>    Chặn ghi vào tuần đã `LOCKED`.
> 4. Duyệt / từ chối / sửa / xoá mềm. Từ chối bắt buộc có lý do. Duyệt hàng loạt.
> 5. Hoàn thiện `AuditSaveChangesInterceptor`: ghi `BeforeJson` / `AfterJson`,
>    người thực hiện, IP, user agent. API xem nhật ký có lọc.
> 6. Báo cáo thiếu dữ liệu: ngày lớp không có bản ghi nào, học sinh chưa từng bị
>    ghi lỗi trong tuần/kỳ, bản ghi `PENDING` quá hạn.
>
> Xong khi: có test cho từng ràng buộc ở mục 3, và mọi thao tác tạo/sửa/xoá/duyệt
> đều xuất hiện trong `audit_logs` với đủ before/after.

---

## PHASE 3 — Scoring engine và Rule engine

> Đây là phase rủi ro nhất của dự án. Làm chậm, test kỹ.
>
> Mục tiêu: tính đúng điểm tuần, điểm học kỳ, xếp loại, xếp hạng, và phát hiện
> đúng các cảnh báo hạ bậc.
>
> Việc cần làm:
> 1. `NeNep.Scoring` là thư viện **thuần hàm**: không tham chiếu EF Core, không
>    DI, không I/O, không `DateTime.Now`. Nhận vào một bản ghi đầu vào bất biến
>    (danh sách vi phạm đã duyệt + cấu hình + danh sách bậc xếp loại + quy tắc),
>    trả ra kết quả. Nếu bạn thấy mình cần inject `DbContext` vào đây thì thiết
>    kế đã sai.
> 2. Điểm tuần: `100 + Σ điểm cộng − Σ điểm trừ`, chỉ tính bản ghi `APPROVED` và
>    `CountsForScore = true`. Không trần, cho phép âm. Cộng tự động C06 (tuần
>    không có N01) và C07 (số lần C01 > 10), cộng dồn với C01.
> 3. Điểm học kỳ: tổng điểm các tuần đã chốt và `IsCounted = true`, chia cho
>    **số tuần đó**, không phải tổng số tuần của kỳ.
> 4. Xếp loại: tra `ClassificationLevels` theo `Min ≤ điểm < Max`. Không hardcode.
> 5. Xếp hạng trong lớp: dense rank, đồng điểm đồng hạng.
> 6. Rule engine sinh `ConductAlert`, **không** sửa xếp loại:
>    - `IMMEDIATE`: có ≥1 lần → cảnh báo (R06 N12, R07 N13, R11 H11)
>    - `THRESHOLD`: đếm trong kỳ, `> ngưỡng` → cảnh báo. R02 đếm gộp N02+N03.
>    - `REMEDIATION_TIMEOUT`: R04, khi bản ghi N08 quá hạn khắc phục
>    - `MANUAL`: R12, R13 do GVCN ghi nhận
>    - `WARNING`: R14, vắng > 45 buổi/năm, không liên quan hạnh kiểm
>    - Trạng thái `APPROACHING` khi còn `SchoolSettings.AlertLeadCount` lần nữa
>      là chạm ngưỡng.
> 7. `ConductAdjustment`: API cho ADMIN/BGH ghi kết luận cuộc họp. Đây là nguồn
>    **duy nhất** được phép làm `FinalLevel` khác `LevelByScore`.
> 8. Job Hangfire chốt tuần Chủ nhật 20:00 giờ Việt Nam: nếu còn `PENDING` thì
>    không chốt, gửi cảnh báo; sau 16 giờ ân hạn thì chuyển `PENDING` thành
>    `EXPIRED` rồi chốt. Job phải idempotent — chạy lại hai lần không được nhân đôi điểm.
>
> Test bắt buộc, viết trước khi viết logic:
> - Bộ đối chiếu từ `docs/test-cases/tuan-12-lop-9-1.md` (bảng tính tay của GVCN).
>   Nếu file này chưa có, dừng lại và báo tôi.
> - Điểm âm, điểm rất cao, tuần không có bản ghi nào.
> - Xem điểm kỳ ở tuần 5: mẫu số phải là 5, không phải 18.
> - Sửa mức điểm của một mã lỗi rồi tính lại: điểm các tuần đã chốt không đổi.
> - Đồng điểm: hai em cùng 72 điểm phải cùng hạng.
> - R02 đếm gộp: 2 lần N02 + 2 lần N03 = 4 > 3 → phải sinh cảnh báo.
> - Chạy job chốt tuần hai lần liên tiếp: kết quả không đổi.

---

## PHASE 4 — Báo cáo, trang phụ huynh, dashboard BGH

> Đọc `docs/wireframes.html` và `docs/design-system/MASTER.md` trước khi làm UI.
>
> Mục tiêu: hoàn thiện đầu ra cho ba nhóm người dùng còn lại.
>
> Việc cần làm:
> 1. `ParentAccessCode`: sinh token 32 ký tự + mã 6 số, chỉ lưu bcrypt hash của
>    mã. GVCN cấp hàng loạt và in được phiếu cho cả lớp. Sai 5 lần khoá 30 phút.
>    Đếm lượt xem, thu hồi được, hết hạn theo học kỳ.
> 2. Trang phụ huynh (public, không đăng nhập), theo tab 03 của wireframe:
>    điểm tuần, biểu đồ 12 tuần, cảnh báo sắp chạm ngưỡng, danh sách lỗi,
>    điểm cộng, việc cần khắc phục, hạng x/45.
>    **Không hiển thị**: tên học sinh khác, bảng xếp hạng cả lớp, tên người ghi lỗi.
>    Tách hiển thị "điểm nề nếp" và "điểm khen thưởng" thành hai dòng riêng để
>    phụ huynh hiểu vì sao có em điểm rất cao.
> 3. Dashboard BGH chỉ đọc, theo tab 04 của wireframe: KPI, bảng 62 lớp, khối
>    "Cần chú ý" (lớp có tỉ lệ Tốt cao bất thường, lớp nhiều ngày không ai nhập).
> 4. Màn hình GVCN: hàng chờ duyệt, sổ nề nếp tuần, việc cần khắc phục,
>    hàng chờ cảnh báo hạ bậc.
> 5. Màn hình nhập lỗi nhanh cho cán bộ lớp, PWA, mục tiêu ≤ 15 giây/lỗi.
> 6. Xuất Excel và PDF báo cáo tuần/kỳ.
>
> Ràng buộc UI: theo mục 7 của `CLAUDE.md`. Sinh type frontend từ OpenAPI bằng
> `openapi-typescript`, không viết type tay.
>
> Xong khi: bấm thử trên điện thoại thật, nhập một lỗi dưới 15 giây, và mở link
> phụ huynh bằng mã sai 5 lần thì bị khoá.

---

## PHASE 5 — Chuẩn bị chạy thật

> Mục tiêu: hệ thống chạy được trên VPS và sống sót qua pilot lớp 9/1.
>
> Việc cần làm:
> 1. `docker-compose.prod.yml`: Caddy (tự động HTTPS) + API + Postgres.
>    React build tĩnh, Caddy phục vụ.
> 2. Script backup: `pg_dump` hằng đêm, nén, giữ 30 bản, đẩy lên lưu trữ ngoài.
>    Viết luôn script restore và **chứng minh nó chạy được** trước khi go-live.
> 3. Health check, structured logging bằng Serilog, trang giám sát Hangfire.
> 4. Rate limit cho endpoint public của phụ huynh và endpoint đăng nhập.
> 5. Seed dữ liệu thật: 62 lớp, danh sách GVCN, danh sách học sinh.
> 6. Tài liệu vận hành ngắn cho nhà trường: cách cấp tài khoản, cách cấp mã
>    phụ huynh, cách xử lý khi tuần không chốt được. Viết cho giáo viên đọc,
>    không phải cho lập trình viên.
>
> Xong khi: restore từ backup ra một database sạch và dữ liệu khớp hoàn toàn.

---

## PHASE 6 — Sau pilot

Chỉ bắt đầu sau khi lớp 9/1 chạy song song với sổ giấy đủ 3–4 tuần và số liệu
khớp. Nội dung tuỳ phản hồi thực tế, không lên kế hoạch trước.

Thứ tự mở rộng: 1 lớp → khối 9 (15 lớp) → toàn trường. Mỗi bước cách nhau ít
nhất 2 tuần.
