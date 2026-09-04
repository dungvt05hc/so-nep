using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using NeNep.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NeNep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:alert_status", "APPROACHING,TRIGGERED,ACKNOWLEDGED,IN_MEETING,RESOLVED,DISMISSED")
                .Annotation("Npgsql:Enum:audit_action", "CREATE,UPDATE,DELETE,APPROVE,REJECT,EXPIRE,LOCK_WEEK,UNLOCK_WEEK,PUBLISH,GRANT_ACCOUNT,REVOKE_ACCOUNT,RESET_PASSWORD,ISSUE_PARENT_CODE,REVOKE_PARENT_CODE,RAISE_ALERT,RESOLVE_ALERT,ADJUST_LEVEL,OVERRIDE_CATALOG,CONFIG_CHANGE,LOGIN,LOGIN_FAILED")
                .Annotation("Npgsql:Enum:category_kind", "KHEN_THUONG,NE_NEP,VE_SINH,HOC_TAP")
                .Annotation("Npgsql:Enum:compare_op", "GT,GTE")
                .Annotation("Npgsql:Enum:period_scope", "WEEK,TERM,YEAR")
                .Annotation("Npgsql:Enum:remediation_status", "NOT_REQUIRED,PENDING,DONE,OVERDUE")
                .Annotation("Npgsql:Enum:role", "ADMIN,BGH,GVCN,LOP_TRUONG,PHO_HOC_TAP,PHO_LAO_DONG")
                .Annotation("Npgsql:Enum:rule_effect", "DOWNGRADE,UPGRADE,WARNING")
                .Annotation("Npgsql:Enum:rule_type", "IMMEDIATE,THRESHOLD,REMEDIATION_TIMEOUT,MANUAL")
                .Annotation("Npgsql:Enum:violation_status", "PENDING,APPROVED,REJECTED,EXPIRED")
                .Annotation("Npgsql:Enum:week_status", "OPEN,PENDING_REVIEW,LOCKED,PUBLISHED");

            migrationBuilder.CreateTable(
                name: "academic_years",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_years", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "classification_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    rank_order = table.Column<int>(type: "integer", nullable: false),
                    min_score = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    max_score = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    color = table.Column<string>(type: "text", nullable: false, defaultValue: "#888888"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classification_levels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conduct_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    rule_type = table.Column<RuleType>(type: "rule_type", nullable: false),
                    violation_codes = table.Column<List<string>>(type: "text[]", nullable: false),
                    @operator = table.Column<CompareOp>(name: "operator", type: "compare_op", nullable: true, defaultValue: CompareOp.GT),
                    threshold_value = table.Column<int>(type: "integer", nullable: true),
                    period_scope = table.Column<PeriodScope>(type: "period_scope", nullable: false, defaultValue: PeriodScope.TERM),
                    effect = table.Column<RuleEffect>(type: "rule_effect", nullable: false),
                    effect_levels = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conduct_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    level = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "school_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    school_name = table.Column<string>(type: "text", nullable: false),
                    school_code = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    base_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    score_cap = table.Column<int>(type: "integer", nullable: true),
                    score_floor = table.Column<int>(type: "integer", nullable: true),
                    auto_apply_level_rules = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    alert_lead_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    allow_class_point_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    stack_auto_bonus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    lock_day_of_week = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lock_hour = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    grace_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 16),
                    expire_pending_on_lock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    parent_code_length = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    parent_max_failed_tries = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    parent_lock_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    parent_show_rank = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_students", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "violation_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kind = table.Column<CategoryKind>(type: "category_kind", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_violation_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "school_breaks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    year_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    shifted_weeks = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    applied_by_id = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_school_breaks", x => x.id);
                    table.ForeignKey(
                        name: "fk_school_breaks_academic_years_year_id",
                        column: x => x.year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "terms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    year_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terms", x => x.id);
                    table.ForeignKey(
                        name: "fk_terms_academic_years_year_id",
                        column: x => x.year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    student_id = table.Column<int>(type: "integer", nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_users_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "academic_weeks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    year_id = table.Column<int>(type: "integer", nullable: false),
                    term_id = table.Column<int>(type: "integer", nullable: false),
                    week_no = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    is_counted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    not_counted_reason = table.Column<string>(type: "text", nullable: true),
                    original_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<WeekStatus>(type: "week_status", nullable: false, defaultValue: WeekStatus.OPEN),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_weeks", x => x.id);
                    table.ForeignKey(
                        name: "fk_academic_weeks_academic_years_year_id",
                        column: x => x.year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_academic_weeks_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grade_id = table.Column<int>(type: "integer", nullable: false),
                    year_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    homeroom_teacher_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_classes", x => x.id);
                    table.ForeignKey(
                        name: "fk_classes_academic_years_year_id",
                        column: x => x.year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_classes_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_classes_users_homeroom_teacher_id",
                        column: x => x.homeroom_teacher_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "conduct_adjustments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    term_id = table.Column<int>(type: "integer", nullable: false),
                    level_before_id = table.Column<int>(type: "integer", nullable: false),
                    level_after_id = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    meeting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    attendees = table.Column<string>(type: "text", nullable: true),
                    minutes_ref = table.Column<string>(type: "text", nullable: true),
                    decided_by_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conduct_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_conduct_adjustments_classification_levels_level_after_id",
                        column: x => x.level_after_id,
                        principalTable: "classification_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conduct_adjustments_classification_levels_level_before_id",
                        column: x => x.level_before_id,
                        principalTable: "classification_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conduct_adjustments_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conduct_adjustments_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conduct_adjustments_users_decided_by_id",
                        column: x => x.decided_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parent_access_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    issued_by_id = table.Column<int>(type: "integer", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parent_access_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_parent_access_codes_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_parent_access_codes_users_issued_by_id",
                        column: x => x.issued_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false, defaultValueSql: "gen_random_uuid()::text"),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    refresh_hash = table.Column<string>(type: "text", nullable: false),
                    ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<Role>(type: "role", nullable: false),
                    class_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    actor_id = table.Column<int>(type: "integer", nullable: true),
                    actor_role = table.Column<Role>(type: "role", nullable: true),
                    action = table.Column<AuditAction>(type: "audit_action", nullable: false),
                    entity = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: true),
                    class_id = table.Column<int>(type: "integer", nullable: true),
                    before_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    after_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "class_officers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<Role>(type: "role", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_officers", x => x.id);
                    table.ForeignKey(
                        name: "fk_class_officers_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_class_officers_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conduct_scores",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    year_id = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<PeriodScope>(type: "period_scope", nullable: false),
                    period_key = table.Column<string>(type: "text", nullable: false),
                    week_id = table.Column<int>(type: "integer", nullable: true),
                    term_id = table.Column<int>(type: "integer", nullable: true),
                    base_score = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    bonus_total = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    penalty_total = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    raw_score = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    final_score = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    counted_weeks = table.Column<int>(type: "integer", nullable: true),
                    level_by_score_id = table.Column<int>(type: "integer", nullable: true),
                    suggested_downgrades = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    suggested_upgrades = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    final_level_id = table.Column<int>(type: "integer", nullable: true),
                    rank_in_class = table.Column<int>(type: "integer", nullable: true),
                    class_size = table.Column<int>(type: "integer", nullable: true),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conduct_scores", x => x.id);
                    table.ForeignKey(
                        name: "fk_conduct_scores_academic_weeks_week_id",
                        column: x => x.week_id,
                        principalTable: "academic_weeks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_conduct_scores_academic_years_year_id",
                        column: x => x.year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conduct_scores_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conduct_scores_classification_levels_final_level_id",
                        column: x => x.final_level_id,
                        principalTable: "classification_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_conduct_scores_classification_levels_level_by_score_id",
                        column: x => x.level_by_score_id,
                        principalTable: "classification_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_conduct_scores_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conduct_scores_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    year_id = table.Column<int>(type: "integer", nullable: false),
                    order_no = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    left_at = table.Column<DateOnly>(type: "date", nullable: true),
                    leave_note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "fk_enrollments_academic_years_year_id",
                        column: x => x.year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_enrollments_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_enrollments_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "violation_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    counts_for_score = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    class_id = table.Column<int>(type: "integer", nullable: true),
                    allowed_roles = table.Column<List<Role>>(type: "role[]", nullable: false),
                    is_bulk_capable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_auto_computed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_per_week = table.Column<int>(type: "integer", nullable: true),
                    requires_remediation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    remediation_note = table.Column<string>(type: "text", nullable: true),
                    remediation_days = table.Column<int>(type: "integer", nullable: true),
                    ordinal = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_violation_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_violation_types_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_violation_types_violation_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "violation_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "weekly_reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    week_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<WeekStatus>(type: "week_status", nullable: false, defaultValue: WeekStatus.PENDING_REVIEW),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_by_id = table.Column<int>(type: "integer", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_by_id = table.Column<int>(type: "integer", nullable: true),
                    unlock_reason = table.Column<string>(type: "text", nullable: true),
                    unlock_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    snapshot = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_weekly_reports_academic_weeks_week_id",
                        column: x => x.week_id,
                        principalTable: "academic_weeks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_weekly_reports_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conduct_alerts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    term_id = table.Column<int>(type: "integer", nullable: false),
                    rule_id = table.Column<int>(type: "integer", nullable: false),
                    rule_code_snapshot = table.Column<string>(type: "text", nullable: false),
                    rule_name_snapshot = table.Column<string>(type: "text", nullable: false),
                    trigger_count = table.Column<int>(type: "integer", nullable: false),
                    threshold_value = table.Column<int>(type: "integer", nullable: true),
                    suggested_effect = table.Column<RuleEffect>(type: "rule_effect", nullable: false),
                    suggested_levels = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    evidence_record_ids = table.Column<List<int>>(type: "integer[]", nullable: false),
                    status = table.Column<AlertStatus>(type: "alert_status", nullable: false, defaultValue: AlertStatus.TRIGGERED),
                    first_detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    acknowledged_by_id = table.Column<int>(type: "integer", nullable: true),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution_note = table.Column<string>(type: "text", nullable: true),
                    adjustment_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conduct_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_conduct_alerts_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conduct_alerts_conduct_adjustments_adjustment_id",
                        column: x => x.adjustment_id,
                        principalTable: "conduct_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_conduct_alerts_conduct_rules_rule_id",
                        column: x => x.rule_id,
                        principalTable: "conduct_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conduct_alerts_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conduct_alerts_terms_term_id",
                        column: x => x.term_id,
                        principalTable: "terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "class_violation_overrides",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    points_override = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_class_violation_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_class_violation_overrides_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_class_violation_overrides_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_class_violation_overrides_violation_types_type_id",
                        column: x => x.type_id,
                        principalTable: "violation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "violation_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    class_id = table.Column<int>(type: "integer", nullable: false),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    week_id = table.Column<int>(type: "integer", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    type_code_snapshot = table.Column<string>(type: "text", nullable: false),
                    type_name_snapshot = table.Column<string>(type: "text", nullable: false),
                    points_snapshot = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    occurred_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_no = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<ViolationStatus>(type: "violation_status", nullable: false, defaultValue: ViolationStatus.PENDING),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    is_bulk = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    bulk_group_id = table.Column<string>(type: "text", nullable: true),
                    is_flagged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    remediation_status = table.Column<RemediationStatus>(type: "remediation_status", nullable: false, defaultValue: RemediationStatus.NOT_REQUIRED),
                    remediation_note = table.Column<string>(type: "text", nullable: true),
                    remediation_deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    remediation_confirmed_by_id = table.Column<int>(type: "integer", nullable: true),
                    remediation_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reported_by_id = table.Column<int>(type: "integer", nullable: false),
                    reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reviewed_by_id = table.Column<int>(type: "integer", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_violation_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_violation_records_academic_weeks_week_id",
                        column: x => x.week_id,
                        principalTable: "academic_weeks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_violation_records_classes_class_id",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_violation_records_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_violation_records_users_remediation_confirmed_by_id",
                        column: x => x.remediation_confirmed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_violation_records_users_reported_by_id",
                        column: x => x.reported_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_violation_records_users_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_violation_records_violation_types_type_id",
                        column: x => x.type_id,
                        principalTable: "violation_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_academic_weeks_start_date_end_date",
                table: "academic_weeks",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_academic_weeks_term_id",
                table: "academic_weeks",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_academic_weeks_year_id_week_no",
                table: "academic_weeks",
                columns: new[] { "year_id", "week_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_name",
                table: "academic_years",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_id_created_at",
                table: "audit_logs",
                columns: new[] { "actor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_class_id_created_at",
                table: "audit_logs",
                columns: new[] { "class_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_entity_id",
                table: "audit_logs",
                columns: new[] { "entity", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_class_officers_class_id_role",
                table: "class_officers",
                columns: new[] { "class_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_class_officers_student_id",
                table: "class_officers",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_violation_overrides_class_id_type_id",
                table: "class_violation_overrides",
                columns: new[] { "class_id", "type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_class_violation_overrides_created_by_id",
                table: "class_violation_overrides",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_class_violation_overrides_type_id",
                table: "class_violation_overrides",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_classes_grade_id",
                table: "classes",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_classes_homeroom_teacher_id",
                table: "classes",
                column: "homeroom_teacher_id");

            migrationBuilder.CreateIndex(
                name: "ix_classes_year_id_code",
                table: "classes",
                columns: new[] { "year_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_classification_levels_code",
                table: "classification_levels",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_classification_levels_rank_order",
                table: "classification_levels",
                column: "rank_order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conduct_adjustments_decided_by_id",
                table: "conduct_adjustments",
                column: "decided_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_adjustments_level_after_id",
                table: "conduct_adjustments",
                column: "level_after_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_adjustments_level_before_id",
                table: "conduct_adjustments",
                column: "level_before_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_adjustments_student_id_term_id",
                table: "conduct_adjustments",
                columns: new[] { "student_id", "term_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conduct_adjustments_term_id",
                table: "conduct_adjustments",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_alerts_adjustment_id",
                table: "conduct_alerts",
                column: "adjustment_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_alerts_class_id_term_id_status",
                table: "conduct_alerts",
                columns: new[] { "class_id", "term_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_conduct_alerts_rule_id",
                table: "conduct_alerts",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_alerts_status_first_detected_at",
                table: "conduct_alerts",
                columns: new[] { "status", "first_detected_at" });

            migrationBuilder.CreateIndex(
                name: "ix_conduct_alerts_student_id_term_id_rule_id",
                table: "conduct_alerts",
                columns: new[] { "student_id", "term_id", "rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conduct_alerts_term_id",
                table: "conduct_alerts",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_rules_code",
                table: "conduct_rules",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conduct_scores_class_id_scope_period_key",
                table: "conduct_scores",
                columns: new[] { "class_id", "scope", "period_key" });

            migrationBuilder.CreateIndex(
                name: "ix_conduct_scores_final_level_id",
                table: "conduct_scores",
                column: "final_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_scores_level_by_score_id",
                table: "conduct_scores",
                column: "level_by_score_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_scores_student_id_scope_period_key",
                table: "conduct_scores",
                columns: new[] { "student_id", "scope", "period_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conduct_scores_term_id",
                table: "conduct_scores",
                column: "term_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_scores_week_id",
                table: "conduct_scores",
                column: "week_id");

            migrationBuilder.CreateIndex(
                name: "ix_conduct_scores_year_id",
                table: "conduct_scores",
                column: "year_id");

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_class_id_is_active",
                table: "enrollments",
                columns: new[] { "class_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_student_id_year_id",
                table: "enrollments",
                columns: new[] { "student_id", "year_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_year_id",
                table: "enrollments",
                column: "year_id");

            migrationBuilder.CreateIndex(
                name: "ix_grades_level",
                table: "grades",
                column: "level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parent_access_codes_issued_by_id",
                table: "parent_access_codes",
                column: "issued_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_parent_access_codes_student_id_revoked_at",
                table: "parent_access_codes",
                columns: new[] { "student_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_parent_access_codes_token",
                table: "parent_access_codes",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_school_breaks_year_id_start_date",
                table: "school_breaks",
                columns: new[] { "year_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_students_code",
                table: "students",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_students_full_name",
                table: "students",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_terms_year_id_code",
                table: "terms",
                columns: new[] { "year_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role",
                table: "user_roles",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id_role_class_id",
                table: "user_roles",
                columns: new[] { "user_id", "role", "class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_by_id",
                table: "users",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_student_id",
                table: "users",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_violation_categories_kind",
                table: "violation_categories",
                column: "kind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_bulk_group_id",
                table: "violation_records",
                column: "bulk_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_class_id_week_id_status",
                table: "violation_records",
                columns: new[] { "class_id", "week_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_remediation_confirmed_by_id",
                table: "violation_records",
                column: "remediation_confirmed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_remediation_status_remediation_deadline",
                table: "violation_records",
                columns: new[] { "remediation_status", "remediation_deadline" });

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_reported_by_id",
                table: "violation_records",
                column: "reported_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_reviewed_by_id",
                table: "violation_records",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_status_reported_at",
                table: "violation_records",
                columns: new[] { "status", "reported_at" });

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_student_id_week_id",
                table: "violation_records",
                columns: new[] { "student_id", "week_id" });

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_type_id",
                table: "violation_records",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_records_week_id",
                table: "violation_records",
                column: "week_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_types_category_id_is_active",
                table: "violation_types",
                columns: new[] { "category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_violation_types_class_id",
                table: "violation_types",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "ix_violation_types_code",
                table: "violation_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weekly_reports_class_id_week_id",
                table: "weekly_reports",
                columns: new[] { "class_id", "week_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_weekly_reports_week_id",
                table: "weekly_reports",
                column: "week_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "class_officers");

            migrationBuilder.DropTable(
                name: "class_violation_overrides");

            migrationBuilder.DropTable(
                name: "conduct_alerts");

            migrationBuilder.DropTable(
                name: "conduct_scores");

            migrationBuilder.DropTable(
                name: "enrollments");

            migrationBuilder.DropTable(
                name: "parent_access_codes");

            migrationBuilder.DropTable(
                name: "school_breaks");

            migrationBuilder.DropTable(
                name: "school_settings");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "violation_records");

            migrationBuilder.DropTable(
                name: "weekly_reports");

            migrationBuilder.DropTable(
                name: "conduct_adjustments");

            migrationBuilder.DropTable(
                name: "conduct_rules");

            migrationBuilder.DropTable(
                name: "violation_types");

            migrationBuilder.DropTable(
                name: "academic_weeks");

            migrationBuilder.DropTable(
                name: "classification_levels");

            migrationBuilder.DropTable(
                name: "classes");

            migrationBuilder.DropTable(
                name: "violation_categories");

            migrationBuilder.DropTable(
                name: "terms");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "academic_years");

            migrationBuilder.DropTable(
                name: "students");
        }
    }
}
