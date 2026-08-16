namespace Wuxi.Shengshen.Erp.ApiService.Constants.User;

/// <summary>
/// 用户模块错误消息常量（经业务异常信封返回，前端 toast 直接展示；措辞与 Java 端保持一致）。
/// 字段命名/取值对齐 Java UserExceptionEnums（去掉枚举/固定错误码，按 AGENTS.md 硬性规定第 2 条）。
/// </summary>
public static class UserErrorMessages
{
    /// <summary>按 ID 未找到用户（对齐 Java UserExceptionEnums.USER_NOT_FOUND）。</summary>
    public const string NotFound = "未找到此用户数据";

    /// <summary>登录账号重复（新增/编辑时由 RepositoryBase 按 UniqueConstraint 自动查重抛出，对齐 Java UserExceptionEnums.ACCOUNT_ALREADY_EXISTS）。</summary>
    public const string AccountDuplicate = "账户该已存在";

    /// <summary>角色 ID 列表中存在不存在/已删除的角色（对齐 Java UserServiceImpl.createUser 中"角色错误"硬编码错误）。</summary>
    public const string RoleInvalid = "角色错误";

    /// <summary>重复绑定用户角色（对齐 Java UserServiceImpl.userBingRole 中"有用户重复关联"硬编码错误）。</summary>
    public const string UserRoleDuplicate = "有用户重复关联";

    /// <summary>用户已拥有此角色（对齐 Java UserExceptionEnums.USER_HAS_ROLE）。</summary>
    public const string UserHasRole = "用户已拥有此角色";

    /// <summary>新密码不能与当前密码相同（对齐 Java UserServiceImpl.resetCurrentUserPassword 中"密码不能相同"硬编码错误）。</summary>
    public const string PasswordSameAsOld = "密码不能相同";
}
