# PLAN v2.0 — HỆ THỐNG THEO DÕI NỀ NẾP THCS
### Bản cập nhật sau khi có câu trả lời khách hàng + Quy định xếp hạnh kiểm lớp 9/1

Ngày: 29/08/2026 · Thay thế bản v1.0

---

## 0. Những gì đã chốt

| # | Nội dung | Chốt |
|---|---|---|
| 1 | Phạm vi | **1 trường duy nhất**, không bán lại → **bỏ multi-tenant** |
| 2 | Quy mô | THCS, **62 lớp × 45 HS ≈ 2.790 học sinh**, khối 6–9 |
| 3 | Quy chế | Đã có file *Quy định và bảng điểm xếp hạnh kiểm lớp 9/1* |
| 4 | Công thức kỳ | Điểm kỳ = Tổng điểm các tuần ÷ Số tuần (**trung bình tuần**) |
| 5 | Điểm cộng | **Có** — 12 loại (C01–C12) |
| 6 | Hạ bậc | **Có** — nhiều quy tắc, xem mục 3 |
| 7 | Phụ huynh | Chỉ xem dữ liệu **con mình** |
| 8 | Xác thực PH | **Mã 6 số** do GVCN cấp |
| 9 | Chốt tuần | **Tự động Chủ nhật**, sau khi GVCN xác nhận hết lỗi |
| 10 | Cán bộ lớp | PHT/PLĐ chỉ chọn từ **danh mục lỗi có sẵn**, không tự tạo lỗi mới |
| 11 | BGH | **Có** — xem toàn trường, nhóm theo lớp (chỉ đọc) |
| 12 | Thi đua lớp | **Không** → bỏ khỏi scope |
| 13 | Ảnh bằng chứng | **Không** → bỏ upload, tiết kiệm hạ tầng |
| 14 | Ngân sách | Tối thiểu, ưu tiên phí duy trì thấp. Deadline linh hoạt |

**Ước tính tải hệ thống:** ~2.790 HS × ~2 bản ghi/tuần × 35 tuần ≈ **200.000 bản ghi/năm học** (~60 MB DB/năm). Tài khoản: 62 GVCN + 186 cán bộ lớp + ~5 BGH ≈ **255 tài khoản**. Đây là tải rất nhẹ — một VPS 2GB RAM thừa sức chạy 5–10 năm.

> **Ghi chú khi đọc file quy định**: các ký hiệu ≥ ≤ < trong phần xếp loại được nhúng dưới dạng đối tượng công thức (Equation), nên khi copy ra text bị mất. Tôi đã mở file gốc để khôi phục và diễn giải như mục 3.3. Vui lòng xác nhận lại cách hiểu này.

---

## 1. Số hóa toàn bộ bảng điểm (dữ liệu seed)

Đây là bảng sẽ được nhập thẳng vào database khi khởi tạo hệ thống.

### 1.1 Điểm cộng (nhóm `KHEN_THUONG`)
| Mã | Nội dung | Điểm | Ghi chú triển khai |
|---|---|---:|---|
| C01 | Phát biểu xây dựng bài | +2 | Nhập nhiều lần/ngày |
| C02 | Đạt điểm 9 (KT thường xuyên) | +10 | |
| C03 | Đạt điểm 10 (KT thường xuyên) | +20 | |
| C04 | Trả lại của rơi | +10 | |
| C05 | Lao động / vệ sinh tự nguyện | +5 | |
| C06 | Đi học đúng giờ cả tuần | +5 | ⚙️ **Hệ thống tự tính**: tuần không có N01 |
| C07 | Tích cực phát biểu (>10 lần) | +10 | ⚙️ **Tự tính** khi đếm C01 > 10 trong tuần |
| C08 | Giúp đỡ GV / nhà trường | +10 | |
| C09 | Giúp đỡ bạn bè học tập, lao động | +10 | |
| C10 | Hành động tốt được GV & bạn công nhận | +20 | Nên giới hạn: chỉ GVCN nhập |
| C11 | Tham gia văn nghệ, hoạt động trường lớp | +20 | |
| C12 | Cán bộ lớp hoàn thành tốt nhiệm vụ | +20 | Chỉ GVCN nhập, 1 lần/tuần/HS |

### 1.2 Nề nếp (nhóm `NE_NEP`)
| Mã | Nội dung | Điểm | Quy tắc kèm theo |
|---|---|---:|---|
| N01 | Đi học trễ, vào lớp trễ sau ra chơi | −5 | 🔻 >10 lần/HK → hạ 1 bậc |
| N02 | Vắng học không phép | −20 | 🔻 (N02+N03) >3 lần/HK → hạ 1 bậc |
| N03 | Trốn tiết | −20 | 🔻 gộp chung ngưỡng với N02 |
| N04 | Vắng có phép | 0 | 📊 Chỉ theo dõi. ⚠️ >45 buổi/năm → cảnh báo ở lại lớp (TT22) |
| N05 | Không tập trung chào cờ, TD giữa giờ | −10 | |
| N06 | Tập trung chào cờ, TD giữa giờ chậm | −5 | |
| N07 | Đồng phục không đúng quy định | −5 | 🔻 >10 lần/HK → hạ 1 bậc |
| N08 | Tóc không đúng quy định | −10 | 🔧 Khắc phục trong ngày; 🔻 sau 3 ngày chưa sửa → hạ 1 bậc |
| N09 | Trang điểm không phù hợp | −10 | 🔧 Tẩy trang ngay |
| N10 | Chửi tục | −20 | 🔻 >5 lần/HK → hạ 1 bậc |
| N11 | Dùng MXH xúc phạm người khác | −20 | |
| N12 | Đánh nhau | −30 | 🔻 **Hạ 1 bậc ngay** |
| N13 | Mang dao, vũ khí sắc nhọn đến trường | −30 | 🔻 **Hạ 1 bậc ngay** |
| N14 | Mang điện thoại có kết nối mạng đến trường | −20 | 🔻 >3 lần/HK → hạ 1 bậc |
| N15 | Vi phạm an toàn giao thông | −20 | 🔻 >3 lần/HK → hạ 1 bậc |

### 1.3 Vệ sinh (nhóm `VE_SINH`)
| Mã | Nội dung | Điểm | Quy tắc kèm theo |
|---|---|---:|---|
| V01 | Không trực nhật lớp | −10 | 🔧 Trực bổ sung 1 tuần · 🔻 >5 lần/HK → hạ 1 bậc |
| V02 | Xả rác | −10 | 🔧 Trực bổ sung 1 ngày |
| V03 | Trực nhật không đầy đủ | −5 | 🔧 Trực bổ sung 1 ngày |
| V04 | Cố tình làm bẩn bàn ghế | −10 | 🔧 Tẩy sạch, hoàn trả nguyên trạng |
| V05 | Phá hoại tài sản nhà trường, bạn bè | −20 | 🔧 Đền bù thiệt hại |
| V06 | Ăn trong lớp | −10 | |
| V07 | Mang đồ ăn vào trong lớp | −5 | |

### 1.4 Học tập (nhóm `HOC_TAP`)
| Mã | Nội dung | Điểm | Quy tắc kèm theo |
|---|---|---:|---|
| H01 | Nói chuyện riêng trong giờ học | −5 | |
| H02 | Không làm BTVN | −10 | |
| H03 | Không chuẩn bị bài | −10 | |
| H04 | Không mang sách vở, đồ dùng HT | −5 | |
| H05 | Không chép bài | −5 | |
| H06 | Làm việc riêng trong giờ học | −10 | |
| H07 | Tự ý ra khỏi chỗ trong giờ học | −10 | |
| H08 | Ngủ trong lớp | −10 | |
| H09 | Giờ học xếp loại Khá | −15 | ❓ Lỗi **tập thể**? — xem mục 2, câu hỏi Q5 |
| H10 | Giờ học xếp loại TB | −25 | 🔧 Viết bản kiểm điểm · ❓ tập thể? |
| H11 | Gian lận trong kiểm tra | −30 | 🔻 **Hạ 1 bậc ngay** |
| H12 | Thái độ không lễ phép với giáo viên | −20 | |
| H13 | Gây mất trật tự trong giờ học | −15 | |

**Tổng: 47 mã (12 cộng + 35 trừ).**

Chú thích ký hiệu: 🔻 quy tắc hạ bậc · 🔧 yêu cầu khắc phục · ⚙️ hệ thống tự tính · 📊 chỉ theo dõi · ❓ cần làm rõ

### 1.5 Tăng bậc (trường hợp đặc biệt)
| Mã | Nội dung | Hiệu lực |
|---|---|---|
| B01 | HS tiến bộ về nề nếp, được GV & bạn bè công nhận | 🔺 Tăng 1 bậc (GVCN quyết định, có ghi lý do) |
| B02 | HS thường xuyên giúp đỡ nhà trường, GV, bạn bè | 🔺 Tăng 1 bậc |

---

## 2. ⚠️ 8 điểm trong quy định cần khách hàng làm rõ trước khi code

Đây là phần quan trọng nhất của bản plan này. Mỗi câu hỏi dưới đây nếu code sai sẽ khiến kết quả xếp loại của gần 2.800 học sinh bị sai.

**Q1 — Điểm cộng có trần không?**
Nếu một em được C03 (+20) + C10 (+20) + C11 (+20) + C12 (+20) + 5×C01 (+10) = **180 điểm/tuần**. Vậy:
- (a) Không giới hạn → xếp hạng lớp sẽ do điểm cộng quyết định, em nào được cán bộ lớp/GVCN ghi nhận nhiều sẽ đứng đầu;
- (b) Chặn trần ở 100 điểm → điểm cộng chỉ có tác dụng "bù" lại điểm trừ;
- (c) Chặn trần ở mức khác (VD 120).

*Khuyến nghị: (b) hoặc (c). Với (a), thứ hạng gửi phụ huynh dễ gây thắc mắc vì phụ thuộc vào việc ai được để ý ghi điểm cộng chứ không phản ánh nề nếp.*

**Q2 — Điểm có được xuống dưới 0 không?**
N12 (−30) + H11 (−30) + N02 (−20) + vài lỗi nhỏ là đã âm. Chặn sàn ở 0, hay cho âm? *Khuyến nghị: chặn sàn ở 0 cho điểm hiển thị, nhưng vẫn lưu điểm thực để GVCN thấy mức độ.*

**Q3 — Nhiều quy tắc hạ bậc cùng lúc thì hạ mấy bậc?**
Ví dụ trong 1 học kỳ: em A vừa đánh nhau (N12, −1 bậc), vừa gian lận kiểm tra (H11, −1 bậc), vừa đi trễ 12 lần (N01, −1 bậc).
- (a) Cộng dồn → hạ 3 bậc (từ Tốt xuống Chưa đạt);
- (b) Tối đa hạ 1 bậc dù vi phạm bao nhiêu quy tắc.

*Khuyến nghị: (a) cộng dồn, nhưng chặn sàn ở "Chưa đạt". Cần chốt rõ vì đây là logic then chốt.*

**Q4 — Tăng bậc (B01/B02) và hạ bậc có bù trừ nhau không?**
Em vừa bị hạ 1 bậc do đi trễ, vừa được tăng 1 bậc do tiến bộ → kết quả giữ nguyên? *Khuyến nghị: có bù trừ, tính tổng đại số, nhưng GVCN phải xác nhận cuối cùng.*

**Q5 — H09/H10 "Gây giờ Khá / Gây giờ TB" là lỗi cá nhân hay tập thể?**
Đây thường là kết quả xếp loại tiết học do GV bộ môn chấm, áp cho **cả lớp**. Nếu vậy hệ thống cần chức năng **"Ghi lỗi cho toàn lớp"** (1 thao tác tạo 45 bản ghi). Nếu là lỗi cá nhân của em gây ra thì giữ như hiện tại. *Đây là câu hỏi ảnh hưởng trực tiếp tới thiết kế màn hình nhập liệu.*

**Q6 — Tuần nghỉ lễ / tuần kiểm tra tính vào "số tuần" của học kỳ không?**
Công thức là *Tổng điểm học kỳ ÷ số tuần*. Nếu tuần nghỉ Tết mà HS mặc định được 100 điểm thì trung bình bị đội lên. Cần đánh dấu tuần nào **được tính** (`is_counted`), và tuần không tính thì không sinh điểm.

**Q7 — Bảng điểm này dùng chung cho cả 62 lớp hay mỗi lớp một bảng?**
File hiện tại ghi rõ "lớp 9/1". Nhưng BGH lại muốn xem dữ liệu toàn trường theo lớp — nếu mỗi lớp một bảng điểm khác nhau thì **không thể so sánh giữa các lớp**.
*Khuyến nghị: một bảng điểm chuẩn cấp trường, GVCN chỉ được bật/tắt lỗi áp dụng cho lớp mình, không được sửa mức điểm. Việc sửa điểm thuộc quyền BGH/Quản trị trường.*

**Q8 — C01 và C07 có cộng chồng không?**
Em phát biểu 12 lần: được 12×2 = 24 điểm (C01), có cộng thêm C07 (+10) nữa không, thành 34? *Khuyến nghị: có, C07 là thưởng thêm, và để hệ thống tự cộng khi đếm được >10 lần — cán bộ lớp không phải nhớ.*

---

## 3. Thiết kế cơ chế tính điểm & xếp loại

### 3.1 Điểm tuần
```
Điểm tuần(HS) = 100
              + Σ (điểm cộng đã DUYỆT trong tuần)
              − Σ (điểm trừ đã DUYỆT trong tuần)
              [+ C06 tự động nếu tuần không có N01]
              [+ C07 tự động nếu số lần C01 > 10]
Áp trần/sàn theo cấu hình (Q1, Q2)
```

### 3.2 Điểm học kỳ
```
Điểm HK = Σ (Điểm tuần của các tuần is_counted = true) ÷ Số tuần is_counted
```

### 3.3 Ngưỡng xếp loại (áp dụng cho cả tuần và học kỳ)
| Xếp loại | Điều kiện |
|---|---|
| **Tốt** | Điểm ≥ 90 |
| **Khá** | 60 ≤ Điểm < 90 |
| **Đạt** | 30 ≤ Điểm < 60 |
| **Chưa đạt** | Điểm < 30 |

*(Đã khôi phục từ các ký hiệu công thức nhúng trong file docx — vui lòng xác nhận.)*

### 3.4 Áp dụng hạ/tăng bậc — CHỈ cho học kỳ
Đọc kỹ quy định: mọi quy tắc đều ghi *"hạ 1 bậc hạnh kiểm **của học kỳ đó**"*. Vậy:
- **Xếp loại tuần** = thuần theo điểm, không áp hạ bậc.
- **Xếp loại học kỳ** = xếp loại theo điểm trung bình → sau đó áp hạ/tăng bậc.

```
Bậc = [Chưa đạt=0, Đạt=1, Khá=2, Tốt=3]

Bậc cuối = Bậc theo điểm − Σ(hạ bậc) + Σ(tăng bậc)
Kẹp trong khoảng [0, 3]
```

### 3.5 Bảng quy tắc hạ/tăng bậc (lưu thành dữ liệu, không hardcode)
| Mã QT | Loại | Điều kiện | Hiệu lực |
|---|---|---|---|
| R01 | Ngưỡng đếm | N01 > 10 lần / học kỳ | −1 bậc |
| R02 | Ngưỡng đếm | (N02 + N03) > 3 lần / học kỳ | −1 bậc |
| R03 | Ngưỡng đếm | N07 > 10 lần / học kỳ | −1 bậc |
| R04 | Khắc phục trễ hạn | N08 chưa khắc phục sau 3 ngày | −1 bậc |
| R05 | Ngưỡng đếm | N10 > 5 lần / học kỳ | −1 bậc |
| R06 | Tức thời | Có N12 | −1 bậc |
| R07 | Tức thời | Có N13 | −1 bậc |
| R08 | Ngưỡng đếm | N14 > 3 lần / học kỳ | −1 bậc |
| R09 | Ngưỡng đếm | N15 > 3 lần / học kỳ | −1 bậc |
| R10 | Ngưỡng đếm | V01 > 5 lần / học kỳ | −1 bậc |
| R11 | Tức thời | Có H11 | −1 bậc |
| R12 | Thủ công | GVCN ghi nhận B01 (tiến bộ) | +1 bậc |
| R13 | Thủ công | GVCN ghi nhận B02 (giúp đỡ) | +1 bậc |
| R14 | Cảnh báo | N04 > 45 buổi / năm học | ⚠️ Cảnh báo ở lại lớp (không liên quan hạnh kiểm) |

**Cấu trúc bảng `conduct_rules`:**
```
id, code, name, rule_type (THRESHOLD|IMMEDIATE|REMEDIATION_TIMEOUT|MANUAL),
violation_codes (mảng: cho phép gộp N02+N03), operator (>|>=), threshold_value,
period_scope (TERM|YEAR), effect (DOWNGRADE|UPGRADE|WARNING), effect_levels (1),
is_active, effective_from
```
Nhờ vậy khi trường đổi ngưỡng (VD từ >10 xuống >8 lần đi trễ) chỉ cần sửa dữ liệu, không cần sửa code.

### 3.6 Cơ chế khắc phục (mới — theo quy định)
Quy định có nhiều hành động khắc phục kèm lỗi. Cần thêm vào bản ghi vi phạm:
```
requires_remediation  : bool
remediation_note      : "Trực bổ sung 1 tuần" / "Viết bản kiểm điểm" / "Đền bù thiệt hại"
remediation_deadline  : date (VD N08 = ngày vi phạm + 3)
remediation_status    : PENDING | DONE | OVERDUE
remediation_confirmed_by / at
```
Màn hình GVCN có tab **"Việc cần khắc phục"** để tick xác nhận. Quy tắc R04 (tóc) chạy tự động dựa trên trạng thái này.

---

## 4. Vai trò & phân quyền (cập nhật)

| Vai trò | Số lượng | Quyền |
|---|---|---|
| **Quản trị trường** | 1–2 | Tạo năm học/tuần học, tạo 62 lớp, gán GVCN, sửa bảng điểm & quy tắc, xem toàn trường |
| **BGH** (mới) | ~5 | **Chỉ đọc** toàn trường: bảng tổng hợp theo lớp/khối, tỉ lệ xếp loại, top vi phạm, tình trạng nhập liệu của từng lớp |
| **GVCN** | 62 | Toàn quyền trên lớp mình: duyệt/sửa/xóa lỗi, cấp–thu hồi tài khoản cán bộ lớp, cấp mã 6 số cho PH, ghi nhận B01/B02, xác nhận khắc phục |
| **Lớp trưởng / PHT / PLĐ** | 186 | Nhập lỗi từ danh mục có sẵn (→ chờ duyệt), xem bản ghi mình đã nhập |
| **Phụ huynh** | ~2.790 | Nhập mã 6 số → xem dữ liệu con mình |

**Quy tắc chống lạm quyền:** cán bộ lớp không được tự ghi điểm cộng cho chính mình; bản ghi mà cán bộ lớp này nhập cho cán bộ lớp khác được đánh dấu ⚑ để GVCN chú ý khi duyệt. C10, C12 chỉ GVCN được nhập.

---

## 5. Quy trình chốt tuần tự động (theo câu trả lời #9)

```
Thứ 2 – Thứ 7 : Cán bộ lớp nhập lỗi → PENDING
                GVCN duyệt/từ chối/sửa → APPROVED / REJECTED
Thứ 7 20:00   : Hệ thống nhắc GVCN nếu còn bản ghi PENDING
Chủ nhật 20:00: Cron chạy chốt tuần
                ├── Nếu KHÔNG còn PENDING → tính điểm, xếp loại, xếp hạng,
                │   khóa tuần, sinh báo cáo, thông báo PH
                └── Nếu CÒN PENDING → KHÔNG chốt, gửi cảnh báo cho GVCN + BGH
Thứ 2 sau 12:00 (ân hạn): vẫn còn PENDING → tự động chuyển EXPIRED
                (không tính điểm), chốt tuần, ghi log rõ lý do
```

**Cần khách hàng chốt:** bản ghi PENDING quá hạn nên bị **hủy** (như đề xuất trên) hay **tự động duyệt**? Tôi khuyến nghị hủy — an toàn hơn cho học sinh, và tạo áp lực đúng chỗ (lên GVCN, không lên HS).

---

## 6. Mô hình dữ liệu (đã tinh gọn — bỏ multi-tenant)

```
school_settings        cấu hình: điểm gốc, trần/sàn, ngưỡng xếp loại, nhãn xếp loại
academic_years ─┬─ terms (HK1, HK2)
                └─ academic_weeks (week_no, start, end, term_id, is_counted, status)
grades (6,7,8,9) ── classes (62 lớp, homeroom_teacher_id, academic_year_id)
users ── user_roles          (ADMIN | BGH | GVCN | LOP_TRUONG | PHO_HOC_TAP | PHO_LAO_DONG)
students ── enrollments (student × class × year) ── class_officers (chức vụ, valid_from/to)
violation_categories ── violation_types (47 mã ở mục 1)
conduct_rules          (14 quy tắc ở mục 3.5)
violation_records      ← bảng trung tâm
remediations           (việc cần khắc phục)
conduct_scores         (điểm & xếp loại theo tuần / học kỳ / năm)
rule_applications      (log: em nào bị hạ bậc bởi quy tắc nào, tại sao)
weekly_reports ── parent_access_codes
audit_logs
```

### `violation_records` — các cột chính
```
id, class_id, student_id, violation_type_id
type_code_snapshot, type_name_snapshot, points_snapshot   ← BẤT BIẾN
quantity, occurred_date, period_no, week_id, note
status (PENDING | APPROVED | REJECTED | EXPIRED)
is_bulk, bulk_group_id              ← cho lỗi tập thể H09/H10 (nếu chốt Q5)
requires_remediation, remediation_status, remediation_deadline
reported_by, reported_at, reviewed_by, reviewed_at, reject_reason
is_locked, deleted_at, deleted_by
```

### `rule_applications` — bảng giải trình hạ bậc
Rất quan trọng: khi phụ huynh hỏi *"tại sao con tôi 85 điểm mà xếp loại Đạt?"*, GVCN phải trả lời được ngay.
```
student_id, term_id, rule_code, trigger_count, effect, applied_at, evidence_record_ids[]
```

### `parent_access_codes`
```
student_id, code_hash (bcrypt mã 6 số), issued_by, issued_at,
expires_at, revoked_at, failed_attempts, locked_until, last_used_at
```
Mã 6 số chỉ có 1 triệu tổ hợp → **bắt buộc** rate limit: sai 5 lần → khóa 30 phút; kết hợp URL token dài đi kèm để chống dò hàng loạt. Link có dạng `/ph/<token-32-ký-tự>` rồi mới nhập mã 6 số.

---

## 7. Danh sách màn hình (MVP)

**Cán bộ lớp (mobile-first)**
1. Đăng nhập → bắt đổi mật khẩu lần đầu
2. **Nhập lỗi nhanh**: chọn HS → chọn mã lỗi (nhóm chip: Nề nếp / Vệ sinh / Học tập / Điểm cộng) → ngày, tiết → Lưu. Mục tiêu ≤ 15 giây
3. Nhập hàng loạt theo danh sách lớp (chế độ điểm danh)
4. Bản ghi của tôi — trạng thái duyệt, lý do bị từ chối

**GVCN (desktop + mobile)**
5. Dashboard lớp: số chờ duyệt, cảnh báo tuần chưa chốt, HS sắp chạm ngưỡng hạ bậc
6. **Hàng chờ duyệt** — duyệt/từ chối hàng loạt, sửa trước khi duyệt
7. Sổ nề nếp tuần — ma trận HS × Ngày
8. Việc cần khắc phục — xác nhận đã khắc phục
9. Ghi nhận đặc biệt — B01/B02, C10, C12
10. Quản lý học sinh — import Excel, gán chức vụ cán bộ lớp
11. Quản lý tài khoản — cấp/khóa/reset cán bộ lớp; cấp mã 6 số cho phụ huynh (in phiếu hàng loạt)
12. Báo cáo tuần / học kỳ — có cột "Vì sao bị hạ bậc"
13. Nhật ký thay đổi (audit)

**BGH**
14. Tổng quan toàn trường: tỉ lệ xếp loại theo khối/lớp, top vi phạm, **bảng tình trạng nhập liệu 62 lớp** (lớp nào chưa nhập, chưa duyệt, chưa chốt)

**Quản trị**
15. Năm học & tuần học (đánh dấu tuần không tính điểm)
16. Bảng điểm & quy tắc hạ bậc
17. Cấu hình hệ thống

**Phụ huynh (public)**
18. Nhập mã → xem: điểm tuần/kỳ, biểu đồ theo tuần, danh sách lỗi (mã, tên, ngày, số lần, điểm), điểm cộng, xếp loại, **hạng x/45**, lý do hạ bậc nếu có, việc cần khắc phục

---

## 8. Công nghệ & chi phí duy trì (ưu tiên rẻ)

### 8.1 So sánh phương án hạ tầng
| Phương án | Chi phí/năm | Ưu | Nhược |
|---|---:|---|---|
| **VPS Việt Nam 2GB** (AZDIGI/Tinohost/Vietnix) + domain | **~1.5 – 2 triệu đ** | Dữ liệu HS lưu trong nước, toàn quyền, chi phí cố định, dễ backup | Phải tự vá bảo mật, tự dựng |
| Oracle Cloud Always Free (ARM 4 vCPU / 24GB) + domain | ~250.000 đ | Mạnh, gần như miễn phí | Server ở nước ngoài; có rủi ro bị thu hồi tài nguyên nếu idle |
| Cloudflare Workers + D1 + Pages | ~250.000 đ | Gần như 0đ, không cần quản trị server | Khóa chặt vào nền tảng CF, D1 hạn chế, khó chuyển đi |
| Vercel + Supabase (free tier) | 0 đ | Triển khai nhanh nhất | Free tier Vercel không cho phép dùng thương mại/tổ chức; Supabase free tạm dừng DB khi ít dùng — **không phù hợp cho hệ thống của trường** |

**Khuyến nghị: VPS Việt Nam ~1,5 triệu/năm.** Với dữ liệu của gần 2.800 học sinh, chi phí này là mức bảo hiểm rẻ nhất cho tính ổn định và chủ quyền dữ liệu. Nếu bắt buộc phải gần 0 đồng, chọn Oracle Free và chấp nhận rủi ro thu hồi.

### 8.2 Stack đề xuất
- **Next.js 15 (TypeScript) + Prisma + PostgreSQL**, một codebase, Docker Compose
- **Caddy** làm reverse proxy (tự động HTTPS Let's Encrypt — miễn phí)
- **PWA** cho cán bộ lớp (thêm vào màn hình chính, không cần build app store)
- **node-cron** cho việc chốt tuần và chạy quy tắc hạ bậc
- Backup: `pg_dump` hằng đêm → nén → đẩy lên Cloudflare R2 hoặc Google Drive (free tier đủ dùng)
- Xuất báo cáo: ExcelJS (Excel) + Puppeteer/react-pdf (PDF)

**Nguyên tắc kỹ thuật bắt buộc:**
1. `points_snapshot` — sửa bảng điểm không được làm đổi báo cáo cũ.
2. Scoring engine + Rule engine là **module thuần hàm**, có unit test riêng, đối chiếu với bảng Excel do GVCN tính tay.
3. Mọi thao tác ghi đi qua một lớp `AuditableService`.
4. Soft delete toàn bộ dữ liệu nghiệp vụ.

---

## 9. Lộ trình (1 dev, deadline linh hoạt)

| Phase | Nội dung | Thời lượng |
|---|---|---|
| **P0** | Chốt 8 câu hỏi mục 2, dựng ERD, wireframe 4 màn hình cốt lõi | 1 tuần |
| **P1** | Auth, RBAC 6 vai trò, năm học/tuần học, khối/lớp/HS, import Excel 2.790 HS, quản lý tài khoản | 2 tuần |
| **P2** | Danh mục 47 mã lỗi, nhập lỗi mobile, hàng chờ duyệt, sửa/xóa, audit log | 2 tuần |
| **P3** | **Scoring engine + Rule engine** (14 quy tắc hạ/tăng bậc), cơ chế khắc phục, chốt tuần tự động | 2 tuần |
| **P4** | Báo cáo tuần/kỳ, mã 6 số + trang phụ huynh, xuất Excel/PDF, dashboard BGH | 2 tuần |
| **P5** | **Pilot lớp 9/1** — chạy song song với sổ giấy 3–4 tuần, đối chiếu từng con điểm | 4 tuần |
| **P6** | Sửa theo phản hồi → mở rộng dần 1 khối → toàn trường | 2–3 tuần |

**Tổng: ~15 tuần** (trong đó 4 tuần pilot chủ yếu là chờ dữ liệu thực).

**Chiến lược triển khai:** tuyệt đối không mở 62 lớp cùng lúc. Lộ trình an toàn: 1 lớp (9/1) → 1 khối (khối 9, ~15 lớp) → toàn trường. Mỗi bước cách nhau ít nhất 2 tuần.

---

## 10. Rủi ro trọng yếu

| Rủi ro | Mức | Xử lý |
|---|---|---|
| Rule engine hạ bậc tính sai → sai xếp loại học kỳ của HS | **Rất cao** | Unit test từng quy tắc; pilot đối chiếu thủ công; bảng `rule_applications` giải trình được từng trường hợp |
| Bảng điểm khác nhau giữa 62 lớp → BGH không so sánh được | Cao | Chốt Q7 ngay ở P0 |
| 62 GVCN không kịp duyệt → tuần không chốt được | Cao | Ân hạn + nhắc tự động + dashboard BGH hiển thị lớp chậm |
| Cán bộ lớp thiên vị khi ghi điểm cộng | Cao | C10/C12 chỉ GVCN nhập; đánh dấu ⚑ bản ghi giữa các cán bộ lớp; báo cáo HS chưa từng có bản ghi |
| Mã 6 số bị dò | Trung bình | URL token dài + rate limit + khóa tạm + hết hạn theo học kỳ |
| Sửa bảng điểm làm sai lệch báo cáo đã gửi PH | Cao | `points_snapshot` + khóa tuần đã chốt |
| Phụ huynh khiếu nại thứ hạng | Trung bình | Chỉ hiện hạng của con; hiển thị đầy đủ lý do; có kênh phản hồi qua GVCN |

---

## 11. Việc cần làm ngay

1. Gửi **8 câu hỏi ở mục 2** cho GVCN/BGH — đặc biệt Q1 (trần điểm cộng), Q3 (cộng dồn hạ bậc), Q5 (H09/H10 tập thể), Q7 (dùng chung bảng điểm).
2. Xin **danh sách 62 lớp + GVCN** và **file Excel danh sách học sinh mẫu 1 lớp** để thiết kế bộ import.
3. Xin **khung kế hoạch năm học** (ngày bắt đầu, số tuần HK1/HK2, các tuần nghỉ) — cần cho Q6.
4. Xin **bảng tính tay điểm nề nếp thật của 1 tuần lớp 9/1** để làm bộ test đối chiếu cho scoring engine.
5. Xác nhận cách hiểu ngưỡng xếp loại ở mục 3.3.
6. Chốt hạ tầng theo mục 8.1 và mua VPS + domain.
