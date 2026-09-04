namespace NeNep.Domain.Enums;

/// <summary>Kind of action recorded in the audit log.</summary>
public enum AuditAction
{
    CREATE,
    UPDATE,
    DELETE,
    APPROVE,
    REJECT,
    EXPIRE,
    LOCK_WEEK,
    UNLOCK_WEEK,
    PUBLISH,
    GRANT_ACCOUNT,
    REVOKE_ACCOUNT,
    RESET_PASSWORD,
    ISSUE_PARENT_CODE,
    REVOKE_PARENT_CODE,
    RAISE_ALERT,
    RESOLVE_ALERT,
    ADJUST_LEVEL,
    OVERRIDE_CATALOG,
    CONFIG_CHANGE,
    LOGIN,
    LOGIN_FAILED,
}
