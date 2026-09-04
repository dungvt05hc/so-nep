/**
 * DỮ LIỆU GỐC — số hoá từ file "Quy định và bảng điểm xếp hạnh kiểm lớp 9/1".
 *
 * File này CHỈ chứa dữ liệu, không import @prisma/client, để có thể dùng lại
 * trong unit test của scoring engine mà không cần database.
 *
 * Quy ước điểm: CÓ DẤU. Cộng là số dương, trừ là số âm.
 */

export type RoleName =
  | 'ADMIN'
  | 'BGH'
  | 'GVCN'
  | 'LOP_TRUONG'
  | 'PHO_HOC_TAP'
  | 'PHO_LAO_DONG';

export type CategoryKindName = 'KHEN_THUONG' | 'NE_NEP' | 'VE_SINH' | 'HOC_TAP';

/** Ai được phép nhập mã lỗi này */
const CAN_BO_LOP: RoleName[] = ['GVCN', 'LOP_TRUONG', 'PHO_HOC_TAP', 'PHO_LAO_DONG'];
const CHI_GVCN: RoleName[] = ['GVCN'];
const HE_THONG: RoleName[] = []; // không ai nhập tay, hệ thống tự tính

// ============================================================================
// BẬC XẾP LOẠI  — mục 3.3 của plan
// Ngưỡng: min <= điểm < max   (max = null nghĩa là không giới hạn trên)
// ============================================================================

export const CLASSIFICATION_LEVELS = [
  { code: 'TOT', name: 'Tốt', rankOrder: 3, minScore: 90, maxScore: null, color: '#2F7A56' },
  { code: 'KHA', name: 'Khá', rankOrder: 2, minScore: 60, maxScore: 90, color: '#3B6FB0' },
  { code: 'DAT', name: 'Đạt', rankOrder: 1, minScore: 30, maxScore: 60, color: '#B08400' },
  { code: 'CHUA_DAT', name: 'Chưa đạt', rankOrder: 0, minScore: null, maxScore: 30, color: '#B3453E' },
] as const;

// ============================================================================
// NHÓM LỖI
// ============================================================================

export const CATEGORIES: { kind: CategoryKindName; name: string; ordinal: number }[] = [
  { kind: 'KHEN_THUONG', name: 'Khen thưởng — điểm cộng', ordinal: 1 },
  { kind: 'NE_NEP', name: 'Nề nếp', ordinal: 2 },
  { kind: 'VE_SINH', name: 'Vệ sinh', ordinal: 3 },
  { kind: 'HOC_TAP', name: 'Học tập', ordinal: 4 },
];

// ============================================================================
// DANH MỤC MÃ LỖI  — 47 mã trong quy định + 2 mã ghi nhận tăng bậc (B01, B02)
// ============================================================================

export interface SeedViolationType {
  code: string;
  kind: CategoryKindName;
  name: string;
  points: number;
  countsForScore?: boolean;
  allowedRoles?: RoleName[];
  isBulkCapable?: boolean;
  isAutoComputed?: boolean;
  maxPerWeek?: number | null;
  requiresRemediation?: boolean;
  remediationNote?: string | null;
  remediationDays?: number | null;
  note?: string | null;
}

export const VIOLATION_TYPES: SeedViolationType[] = [
  // ------------------------- ĐIỂM CỘNG (C01–C12) ---------------------------
  { code: 'C01', kind: 'KHEN_THUONG', name: 'Phát biểu xây dựng bài', points: +2, allowedRoles: CAN_BO_LOP },
  { code: 'C02', kind: 'KHEN_THUONG', name: 'Đạt điểm 9 (kiểm tra thường xuyên)', points: +10, allowedRoles: CHI_GVCN },
  { code: 'C03', kind: 'KHEN_THUONG', name: 'Đạt điểm 10 (kiểm tra thường xuyên)', points: +20, allowedRoles: CHI_GVCN },
  { code: 'C04', kind: 'KHEN_THUONG', name: 'Trả lại của rơi', points: +10, allowedRoles: CAN_BO_LOP },
  { code: 'C05', kind: 'KHEN_THUONG', name: 'Lao động / vệ sinh tự nguyện', points: +5, allowedRoles: CAN_BO_LOP },
  {
    code: 'C06', kind: 'KHEN_THUONG', name: 'Đi học đúng giờ cả tuần', points: +5,
    allowedRoles: HE_THONG, isAutoComputed: true, maxPerWeek: 1,
    note: 'Hệ thống tự cộng khi tuần không có bản ghi N01 nào được duyệt.',
  },
  {
    code: 'C07', kind: 'KHEN_THUONG', name: 'Tích cực phát biểu xây dựng bài (trên 10 lần)', points: +10,
    allowedRoles: HE_THONG, isAutoComputed: true, maxPerWeek: 1,
    note: 'CHỐT Q8: hệ thống tự cộng khi tổng số lần C01 trong tuần > 10, CỘNG DỒN với C01.',
  },
  { code: 'C08', kind: 'KHEN_THUONG', name: 'Có hành động giúp đỡ GV hoặc nhà trường', points: +10, allowedRoles: CAN_BO_LOP },
  { code: 'C09', kind: 'KHEN_THUONG', name: 'Giúp đỡ bạn bè trong học tập, lao động', points: +10, allowedRoles: CAN_BO_LOP },
  { code: 'C10', kind: 'KHEN_THUONG', name: 'Có hành động tốt được GV và bạn bè công nhận', points: +20, allowedRoles: CHI_GVCN },
  { code: 'C11', kind: 'KHEN_THUONG', name: 'Tham gia văn nghệ, các hoạt động trường lớp', points: +20, allowedRoles: CAN_BO_LOP },
  { code: 'C12', kind: 'KHEN_THUONG', name: 'Cán bộ lớp hoàn thành tốt nhiệm vụ', points: +20, allowedRoles: CHI_GVCN, maxPerWeek: 1 },

  // ------------------------- NỀ NẾP (N01–N15) ------------------------------
  { code: 'N01', kind: 'NE_NEP', name: 'Đi học trễ, vào lớp trễ sau giờ ra chơi', points: -5, allowedRoles: CAN_BO_LOP, note: 'R01: quá 10 lần/học kỳ → hạ 1 bậc.' },
  { code: 'N02', kind: 'NE_NEP', name: 'Vắng học không phép', points: -20, allowedRoles: CAN_BO_LOP, note: 'R02: (N02+N03) quá 3 lần/học kỳ → hạ 1 bậc.' },
  { code: 'N03', kind: 'NE_NEP', name: 'Trốn tiết', points: -20, allowedRoles: CAN_BO_LOP, note: 'R02: gộp ngưỡng với N02.' },
  {
    code: 'N04', kind: 'NE_NEP', name: 'Vắng có phép', points: 0, countsForScore: false, allowedRoles: CAN_BO_LOP,
    note: 'Không trừ điểm. R14: quá 45 buổi/năm → cảnh báo ở lại lớp (TT22/2021).',
  },
  { code: 'N05', kind: 'NE_NEP', name: 'Không tập trung chào cờ, thể dục giữa giờ', points: -10, allowedRoles: CAN_BO_LOP },
  { code: 'N06', kind: 'NE_NEP', name: 'Tập trung chào cờ, thể dục giữa giờ chậm', points: -5, allowedRoles: CAN_BO_LOP },
  { code: 'N07', kind: 'NE_NEP', name: 'Đồng phục không đúng quy định', points: -5, allowedRoles: CAN_BO_LOP, note: 'R03: quá 10 lần/học kỳ → hạ 1 bậc.' },
  {
    code: 'N08', kind: 'NE_NEP', name: 'Tóc không đúng quy định', points: -10, allowedRoles: CAN_BO_LOP,
    requiresRemediation: true, remediationNote: 'Chỉnh sửa tóc đúng quy định', remediationDays: 3,
    note: 'R04: sau 3 ngày chưa khắc phục → hạ 1 bậc.',
  },
  {
    code: 'N09', kind: 'NE_NEP', name: 'Trang điểm không phù hợp', points: -10, allowedRoles: CAN_BO_LOP,
    requiresRemediation: true, remediationNote: 'Tẩy trang ngay', remediationDays: 0,
  },
  { code: 'N10', kind: 'NE_NEP', name: 'Chửi tục', points: -20, allowedRoles: CAN_BO_LOP, note: 'R05: quá 5 lần/học kỳ → hạ 1 bậc.' },
  { code: 'N11', kind: 'NE_NEP', name: 'Sử dụng mạng xã hội để xúc phạm người khác', points: -20, allowedRoles: CHI_GVCN },
  { code: 'N12', kind: 'NE_NEP', name: 'Đánh nhau', points: -30, allowedRoles: CHI_GVCN, note: 'R06: hạ 1 bậc ngay.' },
  { code: 'N13', kind: 'NE_NEP', name: 'Mang dao, vũ khí sắc nhọn đến trường', points: -30, allowedRoles: CHI_GVCN, note: 'R07: hạ 1 bậc ngay.' },
  { code: 'N14', kind: 'NE_NEP', name: 'Mang điện thoại có kết nối mạng đến trường', points: -20, allowedRoles: CAN_BO_LOP, note: 'R08: quá 3 lần/học kỳ → hạ 1 bậc.' },
  { code: 'N15', kind: 'NE_NEP', name: 'Vi phạm an toàn giao thông', points: -20, allowedRoles: CAN_BO_LOP, note: 'R09: quá 3 lần/học kỳ → hạ 1 bậc.' },

  // ------------------------- VỆ SINH (V01–V07) -----------------------------
  {
    code: 'V01', kind: 'VE_SINH', name: 'Không trực nhật lớp', points: -10, allowedRoles: CAN_BO_LOP,
    requiresRemediation: true, remediationNote: 'Trực nhật bổ sung 1 tuần', remediationDays: 7,
    note: 'R10: quá 5 lần/học kỳ → hạ 1 bậc.',
  },
  {
    code: 'V02', kind: 'VE_SINH', name: 'Xả rác', points: -10, allowedRoles: CAN_BO_LOP,
    requiresRemediation: true, remediationNote: 'Trực nhật bổ sung 1 ngày', remediationDays: 1,
  },
  {
    code: 'V03', kind: 'VE_SINH', name: 'Trực nhật không đầy đủ', points: -5, allowedRoles: CAN_BO_LOP,
    requiresRemediation: true, remediationNote: 'Trực nhật bổ sung 1 ngày', remediationDays: 1,
  },
  {
    code: 'V04', kind: 'VE_SINH', name: 'Cố tình làm bẩn bàn ghế', points: -10, allowedRoles: CAN_BO_LOP,
    requiresRemediation: true, remediationNote: 'Tẩy sạch, hoàn trả tình trạng ban đầu', remediationDays: 1,
  },
  {
    code: 'V05', kind: 'VE_SINH', name: 'Phá hoại tài sản nhà trường, bạn bè', points: -20, allowedRoles: CHI_GVCN,
    requiresRemediation: true, remediationNote: 'Đền bù thiệt hại', remediationDays: 7,
  },
  { code: 'V06', kind: 'VE_SINH', name: 'Ăn trong lớp', points: -10, allowedRoles: CAN_BO_LOP },
  { code: 'V07', kind: 'VE_SINH', name: 'Mang đồ ăn vào trong lớp', points: -5, allowedRoles: CAN_BO_LOP },

  // ------------------------- HỌC TẬP (H01–H13) -----------------------------
  { code: 'H01', kind: 'HOC_TAP', name: 'Nói chuyện riêng trong giờ học', points: -5, allowedRoles: CAN_BO_LOP },
  { code: 'H02', kind: 'HOC_TAP', name: 'Không làm bài tập về nhà', points: -10, allowedRoles: CAN_BO_LOP },
  { code: 'H03', kind: 'HOC_TAP', name: 'Không chuẩn bị bài', points: -10, allowedRoles: CAN_BO_LOP },
  { code: 'H04', kind: 'HOC_TAP', name: 'Không mang sách vở, đồ dùng học tập', points: -5, allowedRoles: CAN_BO_LOP },
  { code: 'H05', kind: 'HOC_TAP', name: 'Không chép bài', points: -5, allowedRoles: CAN_BO_LOP },
  { code: 'H06', kind: 'HOC_TAP', name: 'Làm việc riêng trong giờ học', points: -10, allowedRoles: CAN_BO_LOP },
  { code: 'H07', kind: 'HOC_TAP', name: 'Tự ý ra khỏi chỗ trong giờ học', points: -10, allowedRoles: CAN_BO_LOP },
  { code: 'H08', kind: 'HOC_TAP', name: 'Ngủ trong lớp', points: -10, allowedRoles: CAN_BO_LOP },
  {
    code: 'H09', kind: 'HOC_TAP', name: 'Giờ học xếp loại Khá', points: -15, allowedRoles: CAN_BO_LOP,
    note: 'CHỐT Q5: lỗi cá nhân — ghi cho học sinh gây ra, không trừ cả lớp.',
  },
  {
    code: 'H10', kind: 'HOC_TAP', name: 'Giờ học xếp loại Trung bình', points: -25, allowedRoles: CAN_BO_LOP,
    requiresRemediation: true, remediationNote: 'Viết bản kiểm điểm', remediationDays: 3,
    note: 'CHỐT Q5: lỗi cá nhân — ghi cho học sinh gây ra, không trừ cả lớp.',
  },
  { code: 'H11', kind: 'HOC_TAP', name: 'Gian lận trong kiểm tra', points: -30, allowedRoles: CHI_GVCN, note: 'R11: hạ 1 bậc ngay.' },
  { code: 'H12', kind: 'HOC_TAP', name: 'Thái độ không lễ phép với giáo viên', points: -20, allowedRoles: CAN_BO_LOP },
  { code: 'H13', kind: 'HOC_TAP', name: 'Gây mất trật tự trong giờ học', points: -15, allowedRoles: CAN_BO_LOP },

  // ---------------- GHI NHẬN TĂNG BẬC (không tính điểm) --------------------
  {
    code: 'B01', kind: 'KHEN_THUONG', name: 'Tiến bộ về nề nếp, được GV và bạn bè công nhận', points: 0,
    countsForScore: false, allowedRoles: CHI_GVCN, maxPerWeek: 1,
    note: 'R12: tăng 1 bậc hạnh kiểm học kỳ. Không cộng điểm.',
  },
  {
    code: 'B02', kind: 'KHEN_THUONG', name: 'Thường xuyên giúp đỡ nhà trường, giáo viên, bạn bè', points: 0,
    countsForScore: false, allowedRoles: CHI_GVCN, maxPerWeek: 1,
    note: 'R13: tăng 1 bậc hạnh kiểm học kỳ. Không cộng điểm.',
  },
];

// ============================================================================
// QUY TẮC HẠ / TĂNG BẬC  (R01–R14)
//
// CHỐT Q3 + Q4: app KHÔNG tự hạ hay tăng bậc hạnh kiểm.
// Rule engine chỉ PHÁT HIỆN và sinh ConductAlert để nhắc.
// Việc hạ bậc do cuộc họp BGH – GVCN – phụ huynh quyết định,
// và được ghi vào ConductAdjustment kèm số biên bản.
// Vì vậy hai quy tắc cùng kích hoạt cũng không "cộng dồn" thành 2 bậc —
// chúng chỉ là hai cảnh báo đưa vào cùng một cuộc họp.
//
// Ngưỡng ghi trong quy chế là "quá N lần" → operator GT.
// ============================================================================

export interface SeedConductRule {
  code: string;
  name: string;
  ruleType: 'IMMEDIATE' | 'THRESHOLD' | 'REMEDIATION_TIMEOUT' | 'MANUAL';
  violationCodes: string[];
  operator?: 'GT' | 'GTE' | null;
  thresholdValue?: number | null;
  periodScope: 'WEEK' | 'TERM' | 'YEAR';
  /** Chỉ là ĐỀ XUẤT đưa ra cuộc họp, app không tự áp dụng. */
  effect: 'DOWNGRADE' | 'UPGRADE' | 'WARNING';
  effectLevels?: number;
  note?: string;
}

export const CONDUCT_RULES: SeedConductRule[] = [
  {
    code: 'R01', name: 'Đi học trễ quá 10 lần trong học kỳ',
    ruleType: 'THRESHOLD', violationCodes: ['N01'], operator: 'GT', thresholdValue: 10,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R02', name: 'Vắng không phép hoặc trốn tiết quá 3 lần trong học kỳ',
    ruleType: 'THRESHOLD', violationCodes: ['N02', 'N03'], operator: 'GT', thresholdValue: 3,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
    note: 'Đếm gộp N02 và N03 vào chung một ngưỡng.',
  },
  {
    code: 'R03', name: 'Đồng phục sai quy định quá 10 lần trong học kỳ',
    ruleType: 'THRESHOLD', violationCodes: ['N07'], operator: 'GT', thresholdValue: 10,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R04', name: 'Tóc sai quy định, không khắc phục sau 3 ngày',
    ruleType: 'REMEDIATION_TIMEOUT', violationCodes: ['N08'], operator: null, thresholdValue: null,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
    note: 'Kích hoạt khi bản ghi N08 có remediationStatus = OVERDUE.',
  },
  {
    code: 'R05', name: 'Chửi tục quá 5 lần trong học kỳ',
    ruleType: 'THRESHOLD', violationCodes: ['N10'], operator: 'GT', thresholdValue: 5,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R06', name: 'Đánh nhau',
    ruleType: 'IMMEDIATE', violationCodes: ['N12'], periodScope: 'TERM',
    effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R07', name: 'Mang dao, vũ khí sắc nhọn đến trường',
    ruleType: 'IMMEDIATE', violationCodes: ['N13'], periodScope: 'TERM',
    effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R08', name: 'Mang điện thoại đến trường quá 3 lần trong học kỳ',
    ruleType: 'THRESHOLD', violationCodes: ['N14'], operator: 'GT', thresholdValue: 3,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R09', name: 'Vi phạm an toàn giao thông quá 3 lần trong học kỳ',
    ruleType: 'THRESHOLD', violationCodes: ['N15'], operator: 'GT', thresholdValue: 3,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R10', name: 'Bỏ trực nhật quá 5 lần trong học kỳ',
    ruleType: 'THRESHOLD', violationCodes: ['V01'], operator: 'GT', thresholdValue: 5,
    periodScope: 'TERM', effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R11', name: 'Gian lận trong kiểm tra',
    ruleType: 'IMMEDIATE', violationCodes: ['H11'], periodScope: 'TERM',
    effect: 'DOWNGRADE', effectLevels: 1,
  },
  {
    code: 'R12', name: 'Tiến bộ về nề nếp được công nhận',
    ruleType: 'MANUAL', violationCodes: ['B01'], periodScope: 'TERM',
    effect: 'UPGRADE', effectLevels: 1,
  },
  {
    code: 'R13', name: 'Thường xuyên giúp đỡ nhà trường, giáo viên, bạn bè',
    ruleType: 'MANUAL', violationCodes: ['B02'], periodScope: 'TERM',
    effect: 'UPGRADE', effectLevels: 1,
  },
  {
    code: 'R14', name: 'Vắng quá 45 buổi trong năm học',
    ruleType: 'THRESHOLD', violationCodes: ['N02', 'N03', 'N04'], operator: 'GT', thresholdValue: 45,
    periodScope: 'YEAR', effect: 'WARNING', effectLevels: 0,
    note: 'TT22/2021 — cảnh báo ở lại lớp. Không ảnh hưởng xếp loại hạnh kiểm.',
  },
];

// ============================================================================
// KIỂM TRA TÍNH TOÀN VẸN — chạy được không cần database
// ============================================================================

export function validateSeedData(): string[] {
  const errors: string[] = [];
  const codes = new Set<string>();

  for (const t of VIOLATION_TYPES) {
    if (codes.has(t.code)) errors.push(`Mã lỗi trùng: ${t.code}`);
    codes.add(t.code);
    if (t.countsForScore !== false && t.points === 0) {
      errors.push(`${t.code}: tính điểm nhưng points = 0`);
    }
    if (t.kind === 'KHEN_THUONG' && t.points < 0) errors.push(`${t.code}: nhóm khen thưởng nhưng điểm âm`);
    if (t.kind !== 'KHEN_THUONG' && t.points > 0) errors.push(`${t.code}: nhóm vi phạm nhưng điểm dương`);
    if (t.requiresRemediation && t.remediationDays == null) {
      errors.push(`${t.code}: yêu cầu khắc phục nhưng thiếu remediationDays`);
    }
    if (t.isAutoComputed && (t.allowedRoles ?? []).length > 0) {
      errors.push(`${t.code}: hệ thống tự tính nhưng vẫn cho phép nhập tay`);
    }
  }

  const ruleCodes = new Set<string>();
  for (const r of CONDUCT_RULES) {
    if (ruleCodes.has(r.code)) errors.push(`Mã quy tắc trùng: ${r.code}`);
    ruleCodes.add(r.code);
    for (const vc of r.violationCodes) {
      if (!codes.has(vc)) errors.push(`${r.code}: tham chiếu mã lỗi không tồn tại "${vc}"`);
    }
    if (r.ruleType === 'THRESHOLD' && (r.thresholdValue == null || r.operator == null)) {
      errors.push(`${r.code}: quy tắc ngưỡng nhưng thiếu operator/thresholdValue`);
    }
  }

  // Các bậc xếp loại phải liền mạch, không hở và không chồng lấn.
  const sorted = [...CLASSIFICATION_LEVELS].sort((a, b) => a.rankOrder - b.rankOrder);
  for (let i = 0; i < sorted.length - 1; i++) {
    if (sorted[i].maxScore !== sorted[i + 1].minScore) {
      errors.push(`Ngưỡng xếp loại hở/chồng giữa ${sorted[i].code} và ${sorted[i + 1].code}`);
    }
  }
  if (sorted[0].minScore !== null) errors.push('Bậc thấp nhất phải mở về phía dưới');
  if (sorted[sorted.length - 1].maxScore !== null) errors.push('Bậc cao nhất phải mở về phía trên');

  return errors;
}
