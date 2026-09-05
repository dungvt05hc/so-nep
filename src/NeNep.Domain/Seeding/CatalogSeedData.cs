using NeNep.Domain.Enums;

namespace NeNep.Domain.Seeding;

/// <summary>
/// THE SOURCE DATA — digitised from "Quy định và bảng điểm xếp hạnh kiểm lớp 9/1"
/// and ported one for one from <c>docs/seed-data.ts</c>.
/// <para>
/// This class holds DATA ONLY: no database, no dependency injection, no I/O, so the
/// scoring engine tests can use it without a database. Nothing here may be changed to
/// suit the code — every value was agreed with the school.
/// </para>
/// <para>
/// Point convention: SIGNED. A bonus is positive, a penalty is negative.
/// </para>
/// </summary>
public static class CatalogSeedData
{
    // --- Who may record a code ---

    /// <summary>The homeroom teacher and the three student officer roles.</summary>
    private static readonly Role[] ClassOfficers =
        [Role.GVCN, Role.LOP_TRUONG, Role.PHO_HOC_TAP, Role.PHO_LAO_DONG];

    /// <summary>Homeroom teacher only.</summary>
    private static readonly Role[] HomeroomOnly = [Role.GVCN];

    /// <summary>Nobody enters it by hand; the system computes it.</summary>
    private static readonly Role[] SystemOnly = [];

    /// <summary>
    /// CLASSIFICATION LEVELS — section 3.3 of the plan.
    /// The band is <c>MinScore</c> inclusive up to <c>MaxScore</c> exclusive; null means
    /// unbounded on that side.
    /// </summary>
    public static readonly IReadOnlyList<SeedClassificationLevel> ClassificationLevels =
    [
        new("TOT", "Tốt", 3, 90, null, "#2F7A56"),
        new("KHA", "Khá", 2, 60, 90, "#3B6FB0"),
        new("DAT", "Đạt", 1, 30, 60, "#B08400"),
        new("CHUA_DAT", "Chưa đạt", 0, null, 30, "#B3453E"),
    ];

    /// <summary>The four catalog groups.</summary>
    public static readonly IReadOnlyList<SeedCategory> Categories =
    [
        new(CategoryKind.KHEN_THUONG, "Khen thưởng — điểm cộng", 1),
        new(CategoryKind.NE_NEP, "Nề nếp", 2),
        new(CategoryKind.VE_SINH, "Vệ sinh", 3),
        new(CategoryKind.HOC_TAP, "Học tập", 4),
    ];

    /// <summary>
    /// THE CATALOG — the 47 codes of the regulations plus the two upgrade codes
    /// (B01, B02), 49 in total. The order of this list is the display order
    /// (<c>violation_types.ordinal</c>).
    /// </summary>
    public static readonly IReadOnlyList<SeedViolationType> ViolationTypes =
    [
        // ----------------------- BONUS POINTS (C01-C12) ------------------------
        new() { Code = "C01", Kind = CategoryKind.KHEN_THUONG, Name = "Phát biểu xây dựng bài", Points = +2, AllowedRoles = ClassOfficers },
        new() { Code = "C02", Kind = CategoryKind.KHEN_THUONG, Name = "Đạt điểm 9 (kiểm tra thường xuyên)", Points = +10, AllowedRoles = HomeroomOnly },
        new() { Code = "C03", Kind = CategoryKind.KHEN_THUONG, Name = "Đạt điểm 10 (kiểm tra thường xuyên)", Points = +20, AllowedRoles = HomeroomOnly },
        new() { Code = "C04", Kind = CategoryKind.KHEN_THUONG, Name = "Trả lại của rơi", Points = +10, AllowedRoles = ClassOfficers },
        new() { Code = "C05", Kind = CategoryKind.KHEN_THUONG, Name = "Lao động / vệ sinh tự nguyện", Points = +5, AllowedRoles = ClassOfficers },
        new()
        {
            Code = "C06", Kind = CategoryKind.KHEN_THUONG, Name = "Đi học đúng giờ cả tuần", Points = +5,
            AllowedRoles = SystemOnly, IsAutoComputed = true, MaxPerWeek = 1,
            Note = "Hệ thống tự cộng khi tuần không có bản ghi N01 nào được duyệt.",
        },
        new()
        {
            Code = "C07", Kind = CategoryKind.KHEN_THUONG, Name = "Tích cực phát biểu xây dựng bài (trên 10 lần)", Points = +10,
            AllowedRoles = SystemOnly, IsAutoComputed = true, MaxPerWeek = 1,
            Note = "CHỐT Q8: hệ thống tự cộng khi tổng số lần C01 trong tuần > 10, CỘNG DỒN với C01.",
        },
        new() { Code = "C08", Kind = CategoryKind.KHEN_THUONG, Name = "Có hành động giúp đỡ GV hoặc nhà trường", Points = +10, AllowedRoles = ClassOfficers },
        new() { Code = "C09", Kind = CategoryKind.KHEN_THUONG, Name = "Giúp đỡ bạn bè trong học tập, lao động", Points = +10, AllowedRoles = ClassOfficers },
        new() { Code = "C10", Kind = CategoryKind.KHEN_THUONG, Name = "Có hành động tốt được GV và bạn bè công nhận", Points = +20, AllowedRoles = HomeroomOnly },
        new() { Code = "C11", Kind = CategoryKind.KHEN_THUONG, Name = "Tham gia văn nghệ, các hoạt động trường lớp", Points = +20, AllowedRoles = ClassOfficers },
        new() { Code = "C12", Kind = CategoryKind.KHEN_THUONG, Name = "Cán bộ lớp hoàn thành tốt nhiệm vụ", Points = +20, AllowedRoles = HomeroomOnly, MaxPerWeek = 1 },

        // ----------------------- CONDUCT (N01-N15) -----------------------------
        new() { Code = "N01", Kind = CategoryKind.NE_NEP, Name = "Đi học trễ, vào lớp trễ sau giờ ra chơi", Points = -5, AllowedRoles = ClassOfficers, Note = "R01: quá 10 lần/học kỳ → hạ 1 bậc." },
        new() { Code = "N02", Kind = CategoryKind.NE_NEP, Name = "Vắng học không phép", Points = -20, AllowedRoles = ClassOfficers, Note = "R02: (N02+N03) quá 3 lần/học kỳ → hạ 1 bậc." },
        new() { Code = "N03", Kind = CategoryKind.NE_NEP, Name = "Trốn tiết", Points = -20, AllowedRoles = ClassOfficers, Note = "R02: gộp ngưỡng với N02." },
        new()
        {
            Code = "N04", Kind = CategoryKind.NE_NEP, Name = "Vắng có phép", Points = 0,
            CountsForScore = false, AllowedRoles = ClassOfficers,
            Note = "Không trừ điểm. R14: quá 45 buổi/năm → cảnh báo ở lại lớp (TT22/2021).",
        },
        new() { Code = "N05", Kind = CategoryKind.NE_NEP, Name = "Không tập trung chào cờ, thể dục giữa giờ", Points = -10, AllowedRoles = ClassOfficers },
        new() { Code = "N06", Kind = CategoryKind.NE_NEP, Name = "Tập trung chào cờ, thể dục giữa giờ chậm", Points = -5, AllowedRoles = ClassOfficers },
        new() { Code = "N07", Kind = CategoryKind.NE_NEP, Name = "Đồng phục không đúng quy định", Points = -5, AllowedRoles = ClassOfficers, Note = "R03: quá 10 lần/học kỳ → hạ 1 bậc." },
        new()
        {
            Code = "N08", Kind = CategoryKind.NE_NEP, Name = "Tóc không đúng quy định", Points = -10, AllowedRoles = ClassOfficers,
            RequiresRemediation = true, RemediationNote = "Chỉnh sửa tóc đúng quy định", RemediationDays = 3,
            Note = "R04: sau 3 ngày chưa khắc phục → hạ 1 bậc.",
        },
        new()
        {
            Code = "N09", Kind = CategoryKind.NE_NEP, Name = "Trang điểm không phù hợp", Points = -10, AllowedRoles = ClassOfficers,
            RequiresRemediation = true, RemediationNote = "Tẩy trang ngay", RemediationDays = 0,
        },
        new() { Code = "N10", Kind = CategoryKind.NE_NEP, Name = "Chửi tục", Points = -20, AllowedRoles = ClassOfficers, Note = "R05: quá 5 lần/học kỳ → hạ 1 bậc." },
        new() { Code = "N11", Kind = CategoryKind.NE_NEP, Name = "Sử dụng mạng xã hội để xúc phạm người khác", Points = -20, AllowedRoles = HomeroomOnly },
        new() { Code = "N12", Kind = CategoryKind.NE_NEP, Name = "Đánh nhau", Points = -30, AllowedRoles = HomeroomOnly, Note = "R06: hạ 1 bậc ngay." },
        new() { Code = "N13", Kind = CategoryKind.NE_NEP, Name = "Mang dao, vũ khí sắc nhọn đến trường", Points = -30, AllowedRoles = HomeroomOnly, Note = "R07: hạ 1 bậc ngay." },
        new() { Code = "N14", Kind = CategoryKind.NE_NEP, Name = "Mang điện thoại có kết nối mạng đến trường", Points = -20, AllowedRoles = ClassOfficers, Note = "R08: quá 3 lần/học kỳ → hạ 1 bậc." },
        new() { Code = "N15", Kind = CategoryKind.NE_NEP, Name = "Vi phạm an toàn giao thông", Points = -20, AllowedRoles = ClassOfficers, Note = "R09: quá 3 lần/học kỳ → hạ 1 bậc." },

        // ----------------------- HOUSEKEEPING (V01-V07) ------------------------
        new()
        {
            Code = "V01", Kind = CategoryKind.VE_SINH, Name = "Không trực nhật lớp", Points = -10, AllowedRoles = ClassOfficers,
            RequiresRemediation = true, RemediationNote = "Trực nhật bổ sung 1 tuần", RemediationDays = 7,
            Note = "R10: quá 5 lần/học kỳ → hạ 1 bậc.",
        },
        new()
        {
            Code = "V02", Kind = CategoryKind.VE_SINH, Name = "Xả rác", Points = -10, AllowedRoles = ClassOfficers,
            RequiresRemediation = true, RemediationNote = "Trực nhật bổ sung 1 ngày", RemediationDays = 1,
        },
        new()
        {
            Code = "V03", Kind = CategoryKind.VE_SINH, Name = "Trực nhật không đầy đủ", Points = -5, AllowedRoles = ClassOfficers,
            RequiresRemediation = true, RemediationNote = "Trực nhật bổ sung 1 ngày", RemediationDays = 1,
        },
        new()
        {
            Code = "V04", Kind = CategoryKind.VE_SINH, Name = "Cố tình làm bẩn bàn ghế", Points = -10, AllowedRoles = ClassOfficers,
            RequiresRemediation = true, RemediationNote = "Tẩy sạch, hoàn trả tình trạng ban đầu", RemediationDays = 1,
        },
        new()
        {
            Code = "V05", Kind = CategoryKind.VE_SINH, Name = "Phá hoại tài sản nhà trường, bạn bè", Points = -20, AllowedRoles = HomeroomOnly,
            RequiresRemediation = true, RemediationNote = "Đền bù thiệt hại", RemediationDays = 7,
        },
        new() { Code = "V06", Kind = CategoryKind.VE_SINH, Name = "Ăn trong lớp", Points = -10, AllowedRoles = ClassOfficers },
        new() { Code = "V07", Kind = CategoryKind.VE_SINH, Name = "Mang đồ ăn vào trong lớp", Points = -5, AllowedRoles = ClassOfficers },

        // ----------------------- ACADEMIC WORK (H01-H13) -----------------------
        new() { Code = "H01", Kind = CategoryKind.HOC_TAP, Name = "Nói chuyện riêng trong giờ học", Points = -5, AllowedRoles = ClassOfficers },
        new() { Code = "H02", Kind = CategoryKind.HOC_TAP, Name = "Không làm bài tập về nhà", Points = -10, AllowedRoles = ClassOfficers },
        new() { Code = "H03", Kind = CategoryKind.HOC_TAP, Name = "Không chuẩn bị bài", Points = -10, AllowedRoles = ClassOfficers },
        new() { Code = "H04", Kind = CategoryKind.HOC_TAP, Name = "Không mang sách vở, đồ dùng học tập", Points = -5, AllowedRoles = ClassOfficers },
        new() { Code = "H05", Kind = CategoryKind.HOC_TAP, Name = "Không chép bài", Points = -5, AllowedRoles = ClassOfficers },
        new() { Code = "H06", Kind = CategoryKind.HOC_TAP, Name = "Làm việc riêng trong giờ học", Points = -10, AllowedRoles = ClassOfficers },
        new() { Code = "H07", Kind = CategoryKind.HOC_TAP, Name = "Tự ý ra khỏi chỗ trong giờ học", Points = -10, AllowedRoles = ClassOfficers },
        new() { Code = "H08", Kind = CategoryKind.HOC_TAP, Name = "Ngủ trong lớp", Points = -10, AllowedRoles = ClassOfficers },
        new()
        {
            Code = "H09", Kind = CategoryKind.HOC_TAP, Name = "Giờ học xếp loại Khá", Points = -15, AllowedRoles = ClassOfficers,
            Note = "CHỐT Q5: lỗi cá nhân — ghi cho học sinh gây ra, không trừ cả lớp.",
        },
        new()
        {
            Code = "H10", Kind = CategoryKind.HOC_TAP, Name = "Giờ học xếp loại Trung bình", Points = -25, AllowedRoles = ClassOfficers,
            RequiresRemediation = true, RemediationNote = "Viết bản kiểm điểm", RemediationDays = 3,
            Note = "CHỐT Q5: lỗi cá nhân — ghi cho học sinh gây ra, không trừ cả lớp.",
        },
        new() { Code = "H11", Kind = CategoryKind.HOC_TAP, Name = "Gian lận trong kiểm tra", Points = -30, AllowedRoles = HomeroomOnly, Note = "R11: hạ 1 bậc ngay." },
        new() { Code = "H12", Kind = CategoryKind.HOC_TAP, Name = "Thái độ không lễ phép với giáo viên", Points = -20, AllowedRoles = ClassOfficers },
        new() { Code = "H13", Kind = CategoryKind.HOC_TAP, Name = "Gây mất trật tự trong giờ học", Points = -15, AllowedRoles = ClassOfficers },

        // ------------------ UPGRADE RECORDS (not scored) -----------------------
        new()
        {
            Code = "B01", Kind = CategoryKind.KHEN_THUONG, Name = "Tiến bộ về nề nếp, được GV và bạn bè công nhận", Points = 0,
            CountsForScore = false, AllowedRoles = HomeroomOnly, MaxPerWeek = 1,
            Note = "R12: tăng 1 bậc hạnh kiểm học kỳ. Không cộng điểm.",
        },
        new()
        {
            Code = "B02", Kind = CategoryKind.KHEN_THUONG, Name = "Thường xuyên giúp đỡ nhà trường, giáo viên, bạn bè", Points = 0,
            CountsForScore = false, AllowedRoles = HomeroomOnly, MaxPerWeek = 1,
            Note = "R13: tăng 1 bậc hạnh kiểm học kỳ. Không cộng điểm.",
        },
    ];

    /// <summary>
    /// DOWNGRADE / UPGRADE RULES (R01-R14).
    /// <para>
    /// The thresholds in the regulations read "more than N times", which is
    /// <see cref="CompareOp.GT"/>. Storing them as data is what lets the school change a
    /// threshold without a code change.
    /// </para>
    /// </summary>
    public static readonly IReadOnlyList<SeedConductRule> ConductRules =
    [
        new()
        {
            Code = "R01", Name = "Đi học trễ quá 10 lần trong học kỳ",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["N01"], Operator = CompareOp.GT, ThresholdValue = 10,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R02", Name = "Vắng không phép hoặc trốn tiết quá 3 lần trong học kỳ",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["N02", "N03"], Operator = CompareOp.GT, ThresholdValue = 3,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
            Note = "Đếm gộp N02 và N03 vào chung một ngưỡng.",
        },
        new()
        {
            Code = "R03", Name = "Đồng phục sai quy định quá 10 lần trong học kỳ",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["N07"], Operator = CompareOp.GT, ThresholdValue = 10,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R04", Name = "Tóc sai quy định, không khắc phục sau 3 ngày",
            RuleType = RuleType.REMEDIATION_TIMEOUT, ViolationCodes = ["N08"], Operator = null, ThresholdValue = null,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
            Note = "Kích hoạt khi bản ghi N08 có remediationStatus = OVERDUE.",
        },
        new()
        {
            Code = "R05", Name = "Chửi tục quá 5 lần trong học kỳ",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["N10"], Operator = CompareOp.GT, ThresholdValue = 5,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R06", Name = "Đánh nhau",
            RuleType = RuleType.IMMEDIATE, ViolationCodes = ["N12"], PeriodScope = PeriodScope.TERM,
            Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R07", Name = "Mang dao, vũ khí sắc nhọn đến trường",
            RuleType = RuleType.IMMEDIATE, ViolationCodes = ["N13"], PeriodScope = PeriodScope.TERM,
            Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R08", Name = "Mang điện thoại đến trường quá 3 lần trong học kỳ",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["N14"], Operator = CompareOp.GT, ThresholdValue = 3,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R09", Name = "Vi phạm an toàn giao thông quá 3 lần trong học kỳ",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["N15"], Operator = CompareOp.GT, ThresholdValue = 3,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R10", Name = "Bỏ trực nhật quá 5 lần trong học kỳ",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["V01"], Operator = CompareOp.GT, ThresholdValue = 5,
            PeriodScope = PeriodScope.TERM, Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R11", Name = "Gian lận trong kiểm tra",
            RuleType = RuleType.IMMEDIATE, ViolationCodes = ["H11"], PeriodScope = PeriodScope.TERM,
            Effect = RuleEffect.DOWNGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R12", Name = "Tiến bộ về nề nếp được công nhận",
            RuleType = RuleType.MANUAL, ViolationCodes = ["B01"], PeriodScope = PeriodScope.TERM,
            Effect = RuleEffect.UPGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R13", Name = "Thường xuyên giúp đỡ nhà trường, giáo viên, bạn bè",
            RuleType = RuleType.MANUAL, ViolationCodes = ["B02"], PeriodScope = PeriodScope.TERM,
            Effect = RuleEffect.UPGRADE, EffectLevels = 1,
        },
        new()
        {
            Code = "R14", Name = "Vắng quá 45 buổi trong năm học",
            RuleType = RuleType.THRESHOLD, ViolationCodes = ["N02", "N03", "N04"], Operator = CompareOp.GT, ThresholdValue = 45,
            PeriodScope = PeriodScope.YEAR, Effect = RuleEffect.WARNING, EffectLevels = 0,
            Note = "TT22/2021 — cảnh báo ở lại lớp. Không ảnh hưởng xếp loại hạnh kiểm.",
        },
    ];

    /// <summary>
    /// The name the school is first registered under in <c>school_settings</c>; the
    /// administrator renames it from the configuration screen. Shown to users, so the
    /// value stays in Vietnamese.
    /// </summary>
    public const string DefaultSchoolName = "Trường THCS (đổi tên trong phần Cấu hình)";
}
