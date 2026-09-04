# Cấu hình hệ thống

Mọi giá trị dưới đây đọc theo thứ tự ưu tiên chuẩn của .NET:
`appsettings.json` → `appsettings.<Environment>.json` → **user secrets** (chỉ máy dev)
→ **biến môi trường** (đè lên tất cả).

Quy tắc đổi tên khi dùng biến môi trường: dấu `:` viết thành `__` (hai dấu gạch dưới).
`Auth:SigningKey` → `Auth__SigningKey`.

---

## 1. Các khoá cấu hình

| Khoá | Mặc định | Ý nghĩa |
|---|---|---|
| `ConnectionStrings:Default` | trỏ localhost | Chuỗi kết nối PostgreSQL |
| `Auth:SigningKey` | **rỗng — bắt buộc nhập** | Khoá ký JWT, tối thiểu 32 byte |
| `Auth:Issuer` / `Auth:Audience` | `nenep` | Định danh token |
| `Auth:AccessTokenMinutes` | 15 | Hạn access token |
| `Auth:RefreshTokenDays` | 30 | Hạn phiên trong bảng `sessions` |
| `Auth:MaxFailedAttempts` | 5 | Sai bao nhiêu lần thì khoá tạm |
| `Auth:LockMinutes` | 15 | Khoá tạm bao lâu |
| `Auth:TemporaryPasswordLength` | 10 | Độ dài mật khẩu tạm cấp cho cán bộ lớp |
| `Auth:MinPasswordLength` | 8 | Mật khẩu người dùng tự đặt |
| `Bootstrap:AdminUsername` | trống | Tài khoản ADMIN đầu tiên |
| `Bootstrap:AdminPassword` | trống | Mật khẩu tạm của tài khoản đó |
| `Bootstrap:AdminFullName` | "Quản trị hệ thống" | Họ tên hiển thị |

Đây là **tham số an ninh vận hành**, không phải con số của quy chế nề nếp. Điểm gốc,
ngưỡng xếp loại, ngưỡng hạ bậc nằm trong bảng `school_settings`, `classification_levels`,
`conduct_rules` — xem mục 2 của `CLAUDE.md`.

Nếu `Auth:SigningKey` trống hoặc ngắn hơn 32 byte, **ứng dụng từ chối khởi động** kèm
thông báo rõ ràng. Đây là cố ý: chạy bằng khoá mặc định nguy hiểm hơn là không chạy.

---

## 2. Máy dev

Khoá ký và mật khẩu admin để trong user secrets, không commit vào Git:

```bash
# tạo khoá ngẫu nhiên
KEY=$(openssl rand -base64 48)

dotnet user-secrets set "Auth:SigningKey"        "$KEY"           --project src/NeNep.Api
dotnet user-secrets set "Bootstrap:AdminUsername" "admin"          --project src/NeNep.Api
dotnet user-secrets set "Bootstrap:AdminPassword" "QuanTri@123456" --project src/NeNep.Api

dotnet user-secrets list --project src/NeNep.Api   # xem lại
```

Rồi:

```bash
docker compose up -d
dotnet ef database update --project src/NeNep.Infrastructure --startup-project src/NeNep.Infrastructure
dotnet run --project src/NeNep.Api
```

Đăng nhập lần đầu:

```bash
curl -X POST http://localhost:5199/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"QuanTri@123456"}'
```

Token trả về có `"mustChangePassword": true`, và **mọi endpoint khác đều bị chặn** cho
đến khi gọi `POST /api/auth/change-password`. Sau khi đổi, mật khẩu trong cấu hình
không còn tác dụng.

---

## 3. VPS

Dùng biến môi trường, không sửa `appsettings.json`. Ví dụ với systemd:

```ini
# /etc/systemd/system/nenep.service
[Service]
WorkingDirectory=/opt/nenep
ExecStart=/usr/bin/dotnet /opt/nenep/NeNep.Api.dll
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=ConnectionStrings__Default=Host=localhost;Database=nenep;Username=nenep;Password=<mat-khau-db>
Environment=Auth__SigningKey=<chuoi-48-byte-base64>
# Chỉ đặt hai dòng dưới cho lần chạy đầu tiên, sau đó xoá đi và restart:
Environment=Bootstrap__AdminUsername=admin
Environment=Bootstrap__AdminPassword=<mat-khau-tam>
```

Tạo khoá ký trên VPS: `openssl rand -base64 48`.

`Bootstrap` chỉ chạy **khi bảng `users` hoàn toàn trống**. Đã có tài khoản thì hai biến
đó bị bỏ qua, nên để quên cũng không tạo thêm tài khoản nào — nhưng vẫn nên xoá khỏi
service file sau lần chạy đầu.

Migration chạy tay khi triển khai, ứng dụng không bao giờ tự migrate:

```bash
dotnet ef database update --project src/NeNep.Infrastructure --startup-project src/NeNep.Infrastructure
```

---

## 4. Đổi khoá ký

Đổi `Auth:SigningKey` làm **mọi access token đang lưu hành mất hiệu lực ngay**; refresh
token vẫn dùng được vì nó không được ký bằng khoá này. Thực tế người dùng chỉ thấy một
lần tải lại trang. Nếu muốn buộc tất cả đăng nhập lại, xoá phiên bằng cách đặt
`revoked_at` cho các dòng trong `sessions`.
