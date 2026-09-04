# Hệ thống theo dõi nề nếp THCS — khởi tạo dữ liệu

## Chạy lần đầu

```bash
npm install
cp .env.example .env      # sửa DATABASE_URL
npx prisma migrate dev --name init
npx prisma db seed                 # cấu hình + 49 mã lỗi + 14 quy tắc
SEED_DEMO=1 npx prisma db seed     # nạp thêm lớp 9/1 với 45 HS để bấm thử
```

Tài khoản mẫu (khi dùng SEED_DEMO), mật khẩu `Nenep@2026`:
`gvcn.91` · `lt.91` · `pht.91` · `pld.91` · `bgh`

## Cấu trúc

| File | Nội dung |
|---|---|
| `prisma/schema.prisma` | 22 model, đã validate |
| `prisma/seed-data.ts` | Dữ liệu thuần: 49 mã lỗi, 14 quy tắc, 4 bậc xếp loại. Không phụ thuộc Prisma nên dùng lại được trong unit test |
| `prisma/seed.ts` | Upsert theo `code`, chạy lại nhiều lần không tạo trùng |
| `wireframes.html` | 4 màn hình cốt lõi |

## Nguyên tắc

Không con số nào của quy chế được xuất hiện trong code. Tất cả nằm ở
`SchoolSetting`, `ClassificationLevel` và `ConductRule`.

Các chỗ chưa chốt với nhà trường được đánh dấu `[Q1]`..`[Q8]` trong schema
và seed. Tìm bằng `grep -rn "\[Q" prisma/`.
