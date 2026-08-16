using Facet.Mapping;

// 注：本文件的命名空间 Wuxi.Shengshen.Erp.ApiService.Data.Requests.User 与实体类型
// Wuxi.Shengshen.Erp.ApiService.Domain.User.User 存在同名冲突（namespace "User" vs type "User"）。
// 用 global:: 全限定实体类型，避免被解析成当前命名空间。
using UserEntity = global::Wuxi.Shengshen.Erp.ApiService.Domain.User.User;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Requests.User;

/// <summary>
/// 创建用户请求 → 实体反向映射补充：
/// 1. Enable 取反落 is_disable（Enable 缺省视为启用）；
/// 2. IsSystem 缺省视为 false；
/// 3. Password / Account 不参与 Facet 自动映射——
///    Password 由 Service 层用雪花 ID 按 <c>PasswordUtil.Encode(id, plaintext)</c> 计算 BCrypt 后写入；
///    Account 由 Service 层强制覆盖为 Email（对齐 Java <c>user.setAccount(user.getEmail())</c>）。
/// RoleIds 不映射到实体，由 Service 层在创建用户后批量插入 user_role_mp。
/// </summary>
public sealed class CreateUserToSourceMapper
    : IFacetToSourceConfiguration<CreateUserRequest, UserEntity>
{
    /// <summary>补充反向映射（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">创建请求。</param>
    /// <param name="target">用户实体。</param>
    public static void Map(CreateUserRequest source, UserEntity target)
    {
        target.IsDisable = !(source.Enable ?? true);
        target.IsSystem ??= false;
    }
}

/// <summary>
/// 编辑用户请求 → 实体反向映射补充：
/// 1. Enable 取反落 is_disable（Enable 缺省视为启用）。
/// Account / Password / RoleIds 全部在 Service 层单独处理：
///   Account 强制覆盖为 Email；
///   Password 字段已从 CreateUserRequest 移除，编辑走独立 <see cref="EditUserPasswordRequest"/> 端点；
///   RoleIds 走"先清后插"全量替换（对齐 Java UserServiceImpl.editUser）。
/// IsSystem 不在编辑请求里覆盖（系统属性一般不在编辑场景修改）。
/// </summary>
public sealed class EditUserToSourceMapper
    : IFacetToSourceConfiguration<EditUserRequest, UserEntity>
{
    /// <summary>补充反向映射（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">编辑请求。</param>
    /// <param name="target">用户实体（已加载的持久化实体，ApplyToSource 覆盖其上）。</param>
    public static void Map(EditUserRequest source, UserEntity target) =>
        target.IsDisable = !(source.Enable ?? true);
}
