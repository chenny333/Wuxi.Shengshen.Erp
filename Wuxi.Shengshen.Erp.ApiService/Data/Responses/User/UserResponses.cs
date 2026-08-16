using Facet;
using KingV.Core.Web;

// 注：本文件 DTO 的命名空间 Wuxi.Shengshen.Erp.ApiService.Data.Responses.User 与实体类型
// Wuxi.Shengshen.Erp.ApiService.Domain.User.User 存在同名冲突（namespace "User" vs type "User"）。
// C# 编译器在 typeof() / nameof() 中优先解析到当前命名空间，导致 Facet 源生成器把 User 当成命名空间
// 处理，生成 using Wuxi.Shengshen.Erp.ApiService.Data.Responses; 等错误代码并触发 CS0234。
// 解决方案：用 global:: 全限定实体类型，使 [Facet(typeof(...))] 与 nameof(...) 都能稳定指向实体。
// 本文件内统一以 UserEntity 这个 alias 指向实体，既避免 namespace 冲突又保持可读性。
using UserEntity = global::Wuxi.Shengshen.Erp.ApiService.Domain.User.User;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Responses.User;

/// <summary>
/// 用户详情（对应 Java GetUserVo）。
/// Id / Enable / 审计字段由 <see cref="EnableResponse"/> 承载（Facet 自动映射，不重复生成）；
/// Password 字段被排除（密码不回传——安全红线）；
/// Enable 取反见 <see cref="UserDetailResponseMapper"/>。
/// RoleIds 由 Service 层在 Facet 映射完成后单独附加（不属于实体字段，对齐 Java 端 vo.setRoleIds(roleIds)）。
/// </summary>
[Facet(typeof(UserEntity),
    nameof(UserEntity.Password),
    nameof(UserEntity.IsDisable), nameof(UserEntity.IsDelete),
    Configuration = typeof(UserDetailResponseMapper))]
public partial class UserDetailResponse : EnableResponse
{
    /// <summary>
    /// 用户已绑定的角色 ID 列表（由 Service 层附加，对齐 Java vo.roleIds）。
    /// 字段名 RoleIds 在 User 实体上无对应列（瞬态），加 <see cref="MapFromAttribute"/> 会指向不存在的属性
    /// 触发 Facet 校验失败；故保留无 [MapFrom] 的手工声明，依赖 Facet 仅生成映射代码、不重复生成属性的特性
    /// （参照 README 硬性规定第 13 条）。
    /// 字段类型 <see cref="List{T}"/> 不适合 <see cref="TableHeaderAttribute"/> 单格展示，故省略表头声明。
    /// </summary>
    public List<long> RoleIds { get; set; } = [];
}

/// <summary>
/// 用户列表行（对应 Java UserListVo）。
/// Id / Enable / 审计字段由 <see cref="EnableResponse"/> 承载（CreateTime / Enable 自带表头列）；
/// Password 字段被排除（密码不回传）；
/// 需要表头的字段手工声明并标 [MapFrom] 声明为"用户自声明成员"，Facet 只做映射不重复生成属性；
/// Enable 取反见 <see cref="UserListItemResponseMapper"/>。
/// </summary>
[Facet(typeof(UserEntity),
    nameof(UserEntity.Password),
    nameof(UserEntity.IsDisable), nameof(UserEntity.IsDelete),
    Configuration = typeof(UserListItemResponseMapper))]
public partial class UserListItemResponse : EnableResponse
{
    /// <summary>姓名。</summary>
    [MapFrom(nameof(UserEntity.Name))]
    [TableHeader("姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>账号。</summary>
    [MapFrom(nameof(UserEntity.Account))]
    [TableHeader("账号")]
    public string Account { get; set; } = string.Empty;

    /// <summary>英文名。</summary>
    [MapFrom(nameof(UserEntity.EnglishName))]
    [TableHeader("英文名", Sortable = SortMode.False)]
    public string? EnglishName { get; set; }

    /// <summary>手机号。</summary>
    [MapFrom(nameof(UserEntity.Phone))]
    [TableHeader("手机号", Sortable = SortMode.False)]
    public string? Phone { get; set; }

    /// <summary>邮箱。</summary>
    [MapFrom(nameof(UserEntity.Email))]
    [TableHeader("邮箱")]
    public string Email { get; set; } = string.Empty;

    /// <summary>所属部门 ID。</summary>
    [MapFrom(nameof(UserEntity.DepartmentId))]
    [TableHeader("部门ID", Sortable = SortMode.False)]
    public long? DepartmentId { get; set; }

    /// <summary>是否系统内置。</summary>
    [MapFrom(nameof(UserEntity.IsSystem))]
    [TableHeader("是否系统用户", Sortable = SortMode.False)]
    public bool? IsSystem { get; set; }

    /// <summary>
    /// 角色名聚合（按 user_role_mp 关联查询后由 Service 层回填，对齐 Java UserListVo.roleName）。
    /// 多角色以英文逗号拼接（与 Java Collectors.joining(",") 保持一致）。
    /// </summary>
    public string? RoleName { get; set; }
}

/// <summary>
/// 用户下拉项（对应 Java GetUserDownListVo）。
/// Id / Enable 由 <see cref="EnableResponse"/> 承载（启用过滤：下拉只展示启用中的账号，便于业务分配）；
/// Password / 审计字段全部排除（最小化载荷）；
/// 需要表头的字段手工声明并标 [MapFrom]。
/// </summary>
[Facet(typeof(UserEntity),
    nameof(UserEntity.Password),
    nameof(UserEntity.IsDisable), nameof(UserEntity.IsDelete),
    nameof(UserEntity.Creator), nameof(UserEntity.CreateBy), nameof(UserEntity.CreateTime),
    nameof(UserEntity.Updater), nameof(UserEntity.UpdateBy), nameof(UserEntity.UpdateTime),
    nameof(UserEntity.TenantId),
    Configuration = typeof(UserDownListItemResponseMapper))]
public partial class UserDownListItemResponse : EnableResponse
{
    /// <summary>姓名。</summary>
    [MapFrom(nameof(UserEntity.Name))]
    [TableHeader("姓名", Sortable = SortMode.False)]
    public string Name { get; set; } = string.Empty;

    /// <summary>账号。</summary>
    [MapFrom(nameof(UserEntity.Account))]
    [TableHeader("账号", Sortable = SortMode.False)]
    public string Account { get; set; } = string.Empty;

    /// <summary>英文名。</summary>
    [MapFrom(nameof(UserEntity.EnglishName))]
    [TableHeader("英文名", Sortable = SortMode.False)]
    public string? EnglishName { get; set; }
}

/// <summary>
/// 当前登录用户信息（对应 Java GetCurrentUserVo）。
/// Id / Enable / 审计字段由 <see cref="EnableResponse"/> 承载；Password 被排除；
/// RoleList / TagList 由 Service 层在 Facet 映射完成后单独附加
/// （Role 模块与 Resource 模块尚未迁移，本期返回空列表占位，待依赖模块迁移后接入）。
/// </summary>
[Facet(typeof(UserEntity),
    nameof(UserEntity.Password),
    nameof(UserEntity.IsDisable), nameof(UserEntity.IsDelete),
    Configuration = typeof(UserCurrentResponseMapper))]
public partial class GetCurrentUserResponse : EnableResponse
{
    /// <summary>角色名集合（Service 层附加，对齐 Java GetCurrentUserVo.roleList）。</summary>
    public List<string> RoleList { get; set; } = [];

    /// <summary>按钮资源 tag 集合（Service 层附加，对齐 Java GetCurrentUserVo.tagList）。</summary>
    public List<string> TagList { get; set; } = [];
}

/// <summary>
/// 按角色查询用户列表行（对应 Java GetUserListByRoleVo）：
/// 字段对齐 UserMapper.xml getUserRoleBindList 查询：id / 姓名 / 手机 / 英文名 / 部门名 / 工号 / 是否绑定。
/// 由于 Department / Role 模块尚未迁移，本期只把 User 实体的字段映射出来，Bind 标志由 Service 层逐行附加，
/// DepartmentName 字段预留为空字符串占位（依赖模块迁移后接入）。
/// </summary>
[Facet(typeof(UserEntity),
    nameof(UserEntity.Password),
    nameof(UserEntity.Account), nameof(UserEntity.IsSystem),
    nameof(UserEntity.IsDisable), nameof(UserEntity.IsDelete),
    nameof(UserEntity.Sex), nameof(UserEntity.BirthDay),
    nameof(UserEntity.Creator), nameof(UserEntity.CreateBy), nameof(UserEntity.CreateTime),
    nameof(UserEntity.Updater), nameof(UserEntity.UpdateBy), nameof(UserEntity.UpdateTime),
    nameof(UserEntity.TenantId),
    nameof(UserEntity.DepartmentId),
    Configuration = typeof(GetUserListByRoleItemResponseMapper))]
public partial class GetUserListByRoleItemResponse : IdResponse
{
    /// <summary>姓名。</summary>
    [MapFrom(nameof(UserEntity.Name))]
    [TableHeader("姓名", Sortable = SortMode.False)]
    public string Name { get; set; } = string.Empty;

    /// <summary>手机号。</summary>
    [MapFrom(nameof(UserEntity.Phone))]
    [TableHeader("手机号", Sortable = SortMode.False)]
    public string? Phone { get; set; }

    /// <summary>英文名。</summary>
    [MapFrom(nameof(UserEntity.EnglishName))]
    [TableHeader("英文名", Sortable = SortMode.False)]
    public string? EnglishName { get; set; }

    /// <summary>工号。</summary>
    [MapFrom(nameof(UserEntity.Email))]
    [TableHeader("账号")]
    public string Email { get; set; } = string.Empty;

    /// <summary>所属部门名称（依赖 Department 模块迁移后接入，本期占位空字符串）。</summary>
    [TableHeader("部门", Sortable = SortMode.False)]
    public string? Department { get; set; }

    /// <summary>是否已绑定当前角色（Service 层逐行附加，对齐 Java SQL ur.role_id IS NOT NULL bind）。</summary>
    [TableHeader("是否绑定", Sortable = SortMode.False)]
    public bool Bind { get; set; }
}
