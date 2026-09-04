/**
 * SEED — chạy được nhiều lần, không tạo trùng (upsert theo code).
 *
 *   npx prisma db seed              → chỉ nạp cấu hình + danh mục + quy tắc
 *   SEED_DEMO=1 npx prisma db seed  → nạp thêm dữ liệu mẫu để bấm thử giao diện
 */

import { PrismaClient, Prisma } from '@prisma/client';
import bcrypt from 'bcryptjs';
import {
  CATEGORIES,
  CLASSIFICATION_LEVELS,
  CONDUCT_RULES,
  VIOLATION_TYPES,
  validateSeedData,
} from './seed-data';

const prisma = new PrismaClient();

async function seedSettings() {
  await prisma.schoolSetting.upsert({
    where: { id: 1 },
    update: {},
    create: {
      id: 1,
      schoolName: 'Trường THCS (đổi tên trong phần Cấu hình)',
      baseScore: 100,
      // Q1: điểm cộng không có trần.
      scoreCap: null,
      // Q2: điểm được phép âm, không chặn sàn.
      scoreFloor: null,
      keepRawScore: true,
      // Q3 + Q4: app chỉ nhắc, không tự hạ/tăng bậc.
      autoApplyLevelRules: false,
      alertLeadCount: 2,
      // Q7: GVCN chỉ được bật/tắt mã lỗi, không sửa mức điểm.
      allowClassPointOverride: false,
      // Q8: C07 cộng dồn với C01, hệ thống tự cộng.
      stackAutoBonus: true,
      lockDayOfWeek: 0,
      lockHour: 20,
      graceHours: 16,
      expirePendingOnLock: true,
      parentShowRank: true,
    },
  });
  console.log('  ✓ Cấu hình trường');
}

async function seedLevels() {
  for (const lv of CLASSIFICATION_LEVELS) {
    await prisma.classificationLevel.upsert({
      where: { code: lv.code },
      update: {
        name: lv.name,
        rankOrder: lv.rankOrder,
        minScore: lv.minScore == null ? null : new Prisma.Decimal(lv.minScore),
        maxScore: lv.maxScore == null ? null : new Prisma.Decimal(lv.maxScore),
        color: lv.color,
      },
      create: {
        code: lv.code,
        name: lv.name,
        rankOrder: lv.rankOrder,
        minScore: lv.minScore == null ? null : new Prisma.Decimal(lv.minScore),
        maxScore: lv.maxScore == null ? null : new Prisma.Decimal(lv.maxScore),
        color: lv.color,
      },
    });
  }
  console.log(`  ✓ ${CLASSIFICATION_LEVELS.length} bậc xếp loại`);
}

async function seedCatalog() {
  const catIdByKind = new Map<string, number>();

  for (const c of CATEGORIES) {
    const row = await prisma.violationCategory.upsert({
      where: { kind: c.kind as any },
      update: { name: c.name, ordinal: c.ordinal },
      create: { kind: c.kind as any, name: c.name, ordinal: c.ordinal },
    });
    catIdByKind.set(c.kind, row.id);
  }
  console.log(`  ✓ ${CATEGORIES.length} nhóm lỗi`);

  let i = 0;
  for (const t of VIOLATION_TYPES) {
    const data = {
      categoryId: catIdByKind.get(t.kind)!,
      name: t.name,
      points: t.points,
      countsForScore: t.countsForScore ?? true,
      allowedRoles: (t.allowedRoles ?? []) as any,
      isBulkCapable: t.isBulkCapable ?? false,
      isAutoComputed: t.isAutoComputed ?? false,
      maxPerWeek: t.maxPerWeek ?? null,
      requiresRemediation: t.requiresRemediation ?? false,
      remediationNote: t.remediationNote ?? null,
      remediationDays: t.remediationDays ?? null,
      ordinal: ++i,
      note: t.note ?? null,
    };
    await prisma.violationType.upsert({
      where: { code: t.code },
      update: data,
      create: { code: t.code, ...data },
    });
  }
  console.log(`  ✓ ${VIOLATION_TYPES.length} mã lỗi`);
}

async function seedRules() {
  for (const r of CONDUCT_RULES) {
    const data = {
      name: r.name,
      ruleType: r.ruleType as any,
      violationCodes: r.violationCodes,
      operator: (r.operator ?? null) as any,
      thresholdValue: r.thresholdValue ?? null,
      periodScope: r.periodScope as any,
      effect: r.effect as any,
      effectLevels: r.effectLevels ?? 1,
      note: r.note ?? null,
    };
    await prisma.conductRule.upsert({
      where: { code: r.code },
      update: data,
      create: { code: r.code, ...data },
    });
  }
  console.log(`  ✓ ${CONDUCT_RULES.length} quy tắc hạ/tăng bậc`);
}

/** Sinh các tuần học liên tiếp từ ngày khai giảng. */
function buildWeeks(start: Date, count: number) {
  const out: { weekNo: number; startDate: Date; endDate: Date }[] = [];
  const cur = new Date(start);
  // lùi về thứ Hai của tuần chứa ngày khai giảng
  cur.setDate(cur.getDate() - ((cur.getDay() + 6) % 7));
  for (let i = 1; i <= count; i++) {
    const s = new Date(cur);
    const e = new Date(cur);
    e.setDate(e.getDate() + 6);
    out.push({ weekNo: i, startDate: s, endDate: e });
    cur.setDate(cur.getDate() + 7);
  }
  return out;
}

async function seedDemo() {
  console.log('\n  Dữ liệu mẫu (SEED_DEMO=1)');

  const year = await prisma.academicYear.upsert({
    where: { name: '2026-2027' },
    update: { isCurrent: true },
    create: {
      name: '2026-2027',
      startDate: new Date('2026-09-07'),
      endDate: new Date('2027-05-28'),
      isCurrent: true,
    },
  });

  // Khai giảng T7 05/9/2026 → tuần học đầu tiên bắt đầu T2 07/9.
  // HK1: 18 tuần (tuần 1–18)  → 07/9/2026 đến 10/01/2027, trước mốc 18/01. ✔
  // HK2: 17 tuần (tuần 19–35) → từ 11/01/2027; cộng ~2 tuần nghỉ Tết
  //      (mùng 1 Tết Đinh Mùi rơi vào 06/02/2027) thì kết thúc khoảng 23/5,
  //      vẫn trước mốc 31/05 và còn dư 1 tuần đệm cho nghỉ đột xuất. ✔
  const HK1_WEEKS = 18;
  const TOTAL_WEEKS = 35;

  const hk1 = await prisma.term.upsert({
    where: { yearId_code: { yearId: year.id, code: 'HK1' } },
    update: {},
    create: {
      yearId: year.id, code: 'HK1', name: 'Học kỳ 1', ordinal: 1,
      startDate: new Date('2026-09-07'), endDate: new Date('2027-01-10'),
    },
  });
  const hk2 = await prisma.term.upsert({
    where: { yearId_code: { yearId: year.id, code: 'HK2' } },
    update: {},
    create: {
      yearId: year.id, code: 'HK2', name: 'Học kỳ 2', ordinal: 2,
      startDate: new Date('2027-01-11'), endDate: new Date('2027-05-31'),
    },
  });

  // 35 tuần học LIÊN TỤC, chưa trừ kỳ nghỉ.
  // Kỳ nghỉ được nhập giữa năm qua SchoolBreak và sẽ dời ngày các tuần
  // chưa chốt về sau — weekNo giữ nguyên 1..35.
  const weeks = buildWeeks(new Date('2026-09-07'), TOTAL_WEEKS);
  for (const w of weeks) {
    const termId = w.weekNo <= HK1_WEEKS ? hk1.id : hk2.id;
    await prisma.academicWeek.upsert({
      where: { yearId_weekNo: { yearId: year.id, weekNo: w.weekNo } },
      update: {},
      create: {
        yearId: year.id, termId, weekNo: w.weekNo,
        startDate: w.startDate, endDate: w.endDate,
        originalStartDate: w.startDate,
      },
    });
  }
  console.log(`    ✓ Năm học 2026-2027 · HK1 ${HK1_WEEKS} tuần · HK2 ${TOTAL_WEEKS - HK1_WEEKS} tuần`);

  // Nghỉ Tết mới là DỰ KIẾN — nhà trường chưa có thông báo chính thức.
  // isConfirmed = false nên hệ thống chưa dời lịch, chỉ hiện cảnh báo cho admin.
  await prisma.schoolBreak.create({
    data: {
      yearId: year.id,
      name: 'Nghỉ Tết Đinh Mùi (dự kiến)',
      startDate: new Date('2027-02-01'),
      endDate: new Date('2027-02-14'),
      isConfirmed: false,
      note: 'Mùng 1 Tết 06/02/2027. Chờ thông báo chính thức của trường để xác nhận và dời lịch.',
    },
  }).catch(() => {});

  for (const level of [6, 7, 8, 9]) {
    await prisma.grade.upsert({
      where: { level },
      update: {},
      create: { level, name: `Khối ${level}` },
    });
  }
  const grade9 = await prisma.grade.findUniqueOrThrow({ where: { level: 9 } });

  const pwd = await bcrypt.hash('Nenep@2026', 10);

  const gvcn = await prisma.user.upsert({
    where: { username: 'gvcn.91' },
    update: {},
    create: {
      username: 'gvcn.91', passwordHash: pwd, fullName: 'GVCN lớp 9/1',
      mustChangePassword: true,
      roles: { create: [{ role: 'GVCN' }] },
    },
  });
  await prisma.user.upsert({
    where: { username: 'bgh' },
    update: {},
    create: {
      username: 'bgh', passwordHash: pwd, fullName: 'Ban giám hiệu',
      mustChangePassword: true,
      roles: { create: [{ role: 'BGH' }] },
    },
  });

  const cls = await prisma.class.upsert({
    where: { yearId_code: { yearId: year.id, code: '9/1' } },
    update: { homeroomTeacherId: gvcn.id },
    create: {
      gradeId: grade9.id, yearId: year.id, code: '9/1', name: 'Lớp 9/1',
      homeroomTeacherId: gvcn.id,
    },
  });

  const HO = ['Nguyễn', 'Trần', 'Lê', 'Phạm', 'Hoàng', 'Huỳnh', 'Vũ', 'Đặng', 'Bùi', 'Đỗ'];
  const DEM = ['Văn', 'Thị', 'Hữu', 'Ngọc', 'Minh', 'Thanh', 'Quang', 'Gia'];
  const TEN = ['An', 'Bình', 'Chi', 'Dũng', 'Giang', 'Hà', 'Hải', 'Khoa', 'Lan', 'Linh',
    'Mai', 'Nam', 'Ngân', 'Phong', 'Quân', 'Sơn', 'Thảo', 'Trang', 'Tuấn', 'Vy'];

  for (let i = 1; i <= 45; i++) {
    const code = `HS2609${String(i).padStart(3, '0')}`;
    const fullName = `${HO[i % HO.length]} ${DEM[i % DEM.length]} ${TEN[i % TEN.length]}`;
    const st = await prisma.student.upsert({
      where: { code },
      update: {},
      create: { code, fullName },
    });
    await prisma.enrollment.upsert({
      where: { studentId_yearId: { studentId: st.id, yearId: year.id } },
      update: {},
      create: { studentId: st.id, classId: cls.id, yearId: year.id, orderNo: i },
    });
  }
  console.log('    ✓ Lớp 9/1 với 45 học sinh');

  // 3 cán bộ lớp — tài khoản do GVCN cấp
  const officers: [number, 'LOP_TRUONG' | 'PHO_HOC_TAP' | 'PHO_LAO_DONG', string][] = [
    [1, 'LOP_TRUONG', 'lt.91'],
    [2, 'PHO_HOC_TAP', 'pht.91'],
    [3, 'PHO_LAO_DONG', 'pld.91'],
  ];
  for (const [orderNo, role, username] of officers) {
    const en = await prisma.enrollment.findFirstOrThrow({
      where: { classId: cls.id, orderNo },
      include: { student: true },
    });
    await prisma.classOfficer.create({
      data: {
        classId: cls.id, studentId: en.studentId, role,
        validFrom: new Date('2026-09-07'),
      },
    }).catch(() => {});
    await prisma.user.upsert({
      where: { username },
      update: {},
      create: {
        username, passwordHash: pwd, fullName: en.student.fullName,
        studentId: en.studentId, createdById: gvcn.id, mustChangePassword: true,
        roles: { create: [{ role, classId: cls.id }] },
      },
    });
  }
  console.log('    ✓ 3 tài khoản cán bộ lớp (mật khẩu: Nenep@2026)');
}

async function main() {
  const errors = validateSeedData();
  if (errors.length) {
    console.error('Dữ liệu seed không hợp lệ:');
    errors.forEach((e) => console.error('  •', e));
    process.exit(1);
  }

  console.log('Nạp dữ liệu nền:');
  await seedSettings();
  await seedLevels();
  await seedCatalog();
  await seedRules();

  if (process.env.SEED_DEMO === '1') await seedDemo();

  console.log('\nXong.');
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(() => prisma.$disconnect());
