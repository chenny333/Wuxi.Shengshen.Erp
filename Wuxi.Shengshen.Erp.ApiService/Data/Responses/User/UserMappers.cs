using Facet.Mapping;

// 注：本文件的命名空间 Wuxi.Shengshen.Erp.ApiService.Data.Responses.User 与实体类型
// Wuxi.Shengshen.Erp.ApiService.Domain.User.User 存在同名冲突（namespace "User" vs type "User"）。
// 用 global:: 全限定实体类型，避免被解析成当前命名空间。
using UserEntity = global::Wuxi.Shengshen.Erp.ApiService.Domain.User.User;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Responses.User;

/// <summary>
/// 用户详情映射补充：Enable 由实体 is_disable 取反得到。
/// 在 Facet 自动映射（同名属性）完成后调用，只处理取反逻辑。
/// </summary>
public sealed class UserDetailResponseMapper
    : IFacetMapConfiguration<UserEntity, UserDetailResponse>
{
    /// <summary>补充映射 Enable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">用户实体。</param>
    /// <param name="target">详情响应。</param>
    public static void Map(UserEntity source, UserDetailResponse target) =>
        target.Enable = !source.IsDisable;
}

/// <summary>
/// 用户列表行映射补充：Enable 由实体 is_disable 取反得到。
/// 在 Facet 自动映射（同名属性）完成后调用，只处理取反逻辑。
/// RoleName 由 Service 层在 Facet 映射完成后按 user_role_mp 聚合后回填（对齐 Java UserListVo.roleName）。
/// </summary>
public sealed class UserListItemResponseMapper
    : IFacetMapConfiguration<UserEntity, UserListItemResponse>
{
    /// <summary>补充映射 Enable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">用户实体。</param>
    /// <param name="target">列表行响应。</param>
    public static void Map(UserEntity source, UserListItemResponse target) =>
        target.Enable = !source.IsDisable;
}

/// <summary>
/// 用户下拉项映射补充：Enable 由实体 is_disable 取反得到。
/// </summary>
public sealed class UserDownListItemResponseMapper
    : IFacetMapConfiguration<UserEntity, UserDownListItemResponse>
{
    /// <summary>补充映射 Enable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">用户实体。</param>
    /// <param name="target">下拉项响应。</param>
    public static void Map(UserEntity source, UserDownListItemResponse target) =>
        target.Enable = !source.IsDisable;
}

/// <summary>
/// 当前用户信息映射补充：Enable 由实体 is_disable 取反得到。
/// RoleList / TagList 由 Service 层附加（依赖 Role / Resource 模块迁移后接入）。
/// </summary>
public sealed class UserCurrentResponseMapper
    : IFacetMapConfiguration<UserEntity, GetCurrentUserResponse>
{
    /// <summary>补充映射 Enable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">用户实体。</param>
    /// <param name="target">当前用户响应。</param>
    public static void Map(UserEntity source, GetCurrentUserResponse target) =>
        target.Enable = !source.IsDisable;
}

/// <summary>
/// 按角色查询用户列表行映射补充：本期仅承担"基线映射"，Bind / Department 字段由 Service 层逐行附加
/// （依赖 Role / Department 模块迁移后接入原生 SQL 查询）。
/// </summary>
public sealed class GetUserListByRoleItemResponseMapper
    : IFacetMapConfiguration<UserEntity, GetUserListByRoleItemResponse>
{
    /// <summary>占位映射（当前仅复用 Facet 默认同名映射）。</summary>
    /// <param name="source">用户实体。</param>
    /// <param name="target">按角色用户列表行响应。</param>
    public static void Map(UserEntity source, GetUserListByRoleItemResponse target)
    {
        // Bind / Department 由 Service 层附加（当前实现不直接依赖 Role / Department 模块）。
    }
}
