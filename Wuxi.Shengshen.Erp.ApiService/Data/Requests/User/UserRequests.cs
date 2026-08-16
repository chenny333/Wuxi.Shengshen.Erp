using System.ComponentModel.DataAnnotations;
using Facet;
using KingV.Core.Web;

// 注：本文件 DTO 的命名空间 Wuxi.Shengshen.Erp.ApiService.Data.Requests.User 与实体类型
// Wuxi.Shengshen.Erp.ApiService.Domain.User.User 存在同名冲突（namespace "User" vs type "User"）。
// C# 编译器在 typeof() / nameof() 中优先解析到当前命名空间，导致 Facet 源生成器把 User 当成命名空间
// 处理，生成 using Wuxi.Shengshen.Erp.ApiService.Data.Requests; 等错误代码并触发 CS0234。
// 解决方案：用 global:: 全限定实体类型，使 [Facet(typeof(...))] 与 nameof(...) 都能稳定指向实体。
// 本文件内统一以 UserEntity 这个 alias 指向实体，既避免 namespace 冲突又保持可读性。
using UserEntity = global::Wuxi.Shengshen.Erp.ApiService.Domain.User.User;
using SexEnum = global::Wuxi.Shengshen.Erp.ApiService.Domain.User.Sex;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Requests.User;

/// <summary>
/// 创建用户请求（对应 Java CreateUserParam）。
/// 请求 → 实体由 Facet 反向映射（GenerateToSource）：手工声明的字段标 [MapFrom(Reversible = true)]
/// 声明为"用户自声明成员"——Facet 只生成映射代码、不重复生成属性，校验特性挂在手工声明上；
/// 排除字段以构造函数位置参数列出（6.6.8 的 Exclude 属性只读，不能当命名参数用）；
/// Password 字段被排除（Java 端创建用户密码统一为默认密码 <c>qwer1234</c>，BCrypt 哈希由 Service 层用雪花 ID 计算后回填）；
/// Enable / IsSystem 与实体字段命名不一致，由 <see cref="CreateUserToSourceMapper"/> 单独处理；
/// RoleIds 不映射到实体（user_role_mp 中间表由 Service 层在创建用户后批量插入）。
/// Account 由 Service 层在创建后强制覆盖为 Email（对齐 Java <c>user.setAccount(user.getEmail())</c>）。
/// </summary>
[Facet(typeof(UserEntity),
    nameof(UserEntity.Id),
    nameof(UserEntity.Creator), nameof(UserEntity.CreateBy), nameof(UserEntity.CreateTime),
    nameof(UserEntity.Updater), nameof(UserEntity.UpdateBy), nameof(UserEntity.UpdateTime),
    nameof(UserEntity.TenantId),
    nameof(UserEntity.IsDisable), nameof(UserEntity.IsDelete),
    nameof(UserEntity.Password),
    nameof(UserEntity.Account),
    GenerateToSource = true,
    ToSourceConfiguration = typeof(CreateUserToSourceMapper))]
public partial class CreateUserRequest
{
    /// <summary>姓名。</summary>
    [MapFrom(nameof(UserEntity.Name), Reversible = true)]
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(64, ErrorMessage = "姓名最大长度不能超过{0}")]
    public string Name { get; set; } = string.Empty;

    /// <summary>英文名。</summary>
    [MapFrom(nameof(UserEntity.EnglishName), Reversible = true)]
    [StringLength(64, ErrorMessage = "英文名最大长度不能超过{0}")]
    public string? EnglishName { get; set; }

    /// <summary>手机号。</summary>
    [MapFrom(nameof(UserEntity.Phone), Reversible = true)]
    [StringLength(32, ErrorMessage = "手机号最大长度不能超过{0}")]
    public string? Phone { get; set; }

    /// <summary>性别（1 男 / 2 女，枚举见 <see cref="SexEnum"/>；JSON 按数值读写，对齐 Java @JsonValue）。</summary>
    [MapFrom(nameof(UserEntity.Sex), Reversible = true)]
    public SexEnum? Sex { get; set; }

    /// <summary>邮箱。</summary>
    [MapFrom(nameof(UserEntity.Email), Reversible = true)]
    [Required(ErrorMessage = "邮箱不能为空")]
    [StringLength(128, ErrorMessage = "邮箱最大长度不能超过{0}")]
    public string Email { get; set; } = string.Empty;

    /// <summary>所属部门 ID。</summary>
    [MapFrom(nameof(UserEntity.DepartmentId), Reversible = true)]
    public long? DepartmentId { get; set; }

    /// <summary>出生日期（Unix 毫秒时间戳）。</summary>
    [MapFrom(nameof(UserEntity.BirthDay), Reversible = true)]
    public long? BirthDay { get; set; }

    /// <summary>是否系统内置（系统用户不允许删除/禁用）。</summary>
    [MapFrom(nameof(UserEntity.IsSystem), Reversible = true)]
    public bool? IsSystem { get; set; }

    /// <summary>是否启用（实体侧无同名属性，不参与自动映射；由反向配置取反落 is_disable）。</summary>
    public bool? Enable { get; set; }

    /// <summary>角色 ID 列表（创建时同时绑定；为空表示不绑定角色，对齐 Java CreateUserParam.roleIds）。</summary>
    public long[]? RoleIds { get; set; }
}

/// <summary>
/// 编辑用户请求（对应 Java EditUserParam）。
/// Id 经 <see cref="IIdRequest"/> 接口叠加（基类槽位被 Create 请求占用），
/// 且不参与 Facet 映射（Id 在排除列表；编辑走 ApplyToSource 覆盖到已加载实体，Id 由实体自身携带）。
/// Account 由 Service 层在编辑后强制覆盖为 Email（对齐 Java <c>user.setAccount(user.getEmail())</c>）；
/// RoleIds 在 Service 层做"先清后插"全量替换（对齐 Java UserServiceImpl.editUser）。
/// </summary>
[Facet(typeof(UserEntity),
    nameof(UserEntity.Id),
    nameof(UserEntity.Creator), nameof(UserEntity.CreateBy), nameof(UserEntity.CreateTime),
    nameof(UserEntity.Updater), nameof(UserEntity.UpdateBy), nameof(UserEntity.UpdateTime),
    nameof(UserEntity.TenantId),
    nameof(UserEntity.IsDisable), nameof(UserEntity.IsDelete),
    nameof(UserEntity.Password),
    nameof(UserEntity.Account),
    GenerateToSource = true,
    ToSourceConfiguration = typeof(EditUserToSourceMapper))]
public partial class EditUserRequest : CreateUserRequest, IIdRequest
{
    /// <summary>主键 ID。</summary>
    [Required(ErrorMessage = "ID不能为空")]
    public long? Id { get; set; }
}

/// <summary>
/// 管理员修改用户密码请求（对应 Java EditUserPasswordParam）。
/// </summary>
public record EditUserPasswordRequest
{
    /// <summary>目标用户 ID。</summary>
    [Required(ErrorMessage = "用户id不能为空")]
    public long UserId { get; set; }

    /// <summary>新明文密码。</summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(255, ErrorMessage = "密码最大长度不能超过{0}")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 当前登录用户重置自己的密码请求（对应 Java ResetCurrentUserPasswordParam）；
/// 实际重置逻辑在 Service 层校验"新旧密码不一致"后落库。
/// </summary>
public record ResetCurrentUserPasswordRequest
{
    /// <summary>新明文密码。</summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(255, ErrorMessage = "密码最大长度不能超过{0}")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 用户列表查询请求（对应 Java GetUserListParam）。
/// </summary>
public record GetUserListRequest : PageRequest
{
    /// <summary>姓名（模糊匹配）。</summary>
    [StringLength(64, ErrorMessage = "姓名最大长度不能超过{0}")]
    public string? Name { get; set; }

    /// <summary>账号（模糊匹配）。</summary>
    [StringLength(128, ErrorMessage = "账号最大长度不能超过{0}")]
    public string? Account { get; set; }

    /// <summary>邮箱（模糊匹配）。</summary>
    [StringLength(128, ErrorMessage = "邮箱最大长度不能超过{0}")]
    public string? Email { get; set; }

    /// <summary>所属部门 ID（精确匹配）。</summary>
    public long? DepartmentId { get; set; }
}

/// <summary>
/// 按角色查询用户列表请求（对应 Java GetUserListByRoleParam）。
/// </summary>
public record GetUserListByRoleRequest : PageRequest
{
    /// <summary>角色 ID。</summary>
    [Required(ErrorMessage = "角色id不能为空")]
    public long RoleId { get; set; }

    /// <summary>姓名（模糊匹配）。</summary>
    [StringLength(64, ErrorMessage = "姓名最大长度不能超过{0}")]
    public string? Name { get; set; }

    /// <summary>部门 ID（精确匹配）。</summary>
    public long? DepartmentId { get; set; }

    /// <summary>是否已绑定该角色（true 仅查已绑定 / false 仅查未绑定 / null 全部）。</summary>
    public bool? IsBind { get; set; }
}

/// <summary>
/// 用户下拉列表请求（对应 Java GetUserDownListParam，body 可选）。
/// </summary>
public record GetUserDownListRequest;

/// <summary>
/// 角色绑定到一批用户请求（对应 Java UserBingRoleParam）：
/// 给定一个 roleId + 多个 userIds，把这个角色批量绑定到这些用户。
/// </summary>
public record UserBingRoleRequest
{
    /// <summary>角色 ID。</summary>
    [Required(ErrorMessage = "角色id不能为空")]
    public long RoleId { get; set; }

    /// <summary>用户 ID 列表（非空）。</summary>
    [Required(ErrorMessage = "用户id列表不能为空")]
    [MinLength(1, ErrorMessage = "用户id列表不能为空")]
    public long[] UserIds { get; set; } = [];
}
