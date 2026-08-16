using KingV.Core.Web;
using Wuxi.Shengshen.Erp.ApiService.Data.Requests.User;
using Wuxi.Shengshen.Erp.ApiService.Data.Responses.User;

namespace Wuxi.Shengshen.Erp.ApiService.Service.Interfaces;

/// <summary>
/// 用户服务（对应 Java UserServiceImpl，补全批次）。
/// enable 与实体 is_disable 恒为取反关系；
/// 密码写入侧统一走 <c>PasswordUtil.Encode(id, plaintext)</c> BCrypt 哈希（创建路径使用系统默认密码 <c>qwer1234</c>）；
/// 角色绑定走 Java 侧语义（按角色批量绑定/解绑用户，对齐 <c>userBingRole</c> / <c>userUnbindRole</c>）。
/// </summary>
public interface IUserService
{
    /// <summary>创建用户。</summary>
    /// <param name="request">创建请求（角色 ID 列表同时批量绑定到该用户）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新用户雪花 ID。</returns>
    Task<long> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>编辑用户（角色 ID 列表走"先清后插"全量替换）。</summary>
    /// <param name="request">编辑请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task EditAsync(EditUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>逻辑删除用户。</summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task RemoveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>启用/禁用切换。</summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ToggleEnabledAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>管理员修改用户密码（不做旧密码校验）。</summary>
    /// <param name="request">修改密码请求（userId + 新明文密码）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task EditUserPasswordAsync(EditUserPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>当前登录用户重置自己的密码（校验新密码不能与当前密码相同，对齐 Java resetCurrentUserPassword）。</summary>
    /// <param name="userId">当前登录用户 ID。</param>
    /// <param name="request">重置请求（仅含新明文密码）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ResetCurrentUserPasswordAsync(long userId, ResetCurrentUserPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>按 ID 查询详情。</summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户详情（Password 不回传，含已绑定角色 ID 列表）。</returns>
    Task<UserDetailResponse> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>查询当前登录用户的详情（角色列表与按钮 tag 列表；依赖 Role / Resource 模块迁移）。</summary>
    /// <param name="userId">当前登录用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前用户信息。</returns>
    Task<GetCurrentUserResponse> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>分页查询用户列表（含角色名聚合）。</summary>
    /// <param name="request">列表请求（分页参数 + 姓名/账号/邮箱 模糊 + 部门精确）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页结果。</returns>
    Task<PageResult<UserListItemResponse>> GetListAsync(
        GetUserListRequest request, CancellationToken cancellationToken = default);

    /// <summary>按角色分页查询用户列表（含"是否绑定当前角色"标志，对齐 Java getUserListByRole）。</summary>
    /// <param name="request">列表请求（角色 ID 必填 + 姓名/部门过滤 + 是否绑定过滤）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页结果。</returns>
    Task<PageResult<GetUserListByRoleItemResponse>> GetUserListByRoleAsync(
        GetUserListByRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>下拉列表（仅启用中的用户全量返回，按 create_time 倒序）。</summary>
    /// <param name="request">下拉请求（可为 null，对齐 Java body 可选）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下拉项列表。</returns>
    Task<List<UserDownListItemResponse>> GetDownListAsync(GetUserDownListRequest? request, CancellationToken cancellationToken = default);

    /// <summary>将一个角色批量绑定到多个用户。</summary>
    /// <param name="request">绑定请求（roleId + userIds）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task BindRoleToUsersAsync(UserBingRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>将一个角色从多个用户批量解绑。</summary>
    /// <param name="request">解绑请求（roleId + userIds）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task UnbindRoleFromUsersAsync(UserBingRoleRequest request, CancellationToken cancellationToken = default);
}
