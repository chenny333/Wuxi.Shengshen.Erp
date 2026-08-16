using Facet.Extensions;
using KingV.Core.Exceptions;
using KingV.Core.Extensions;
using KingV.Core.Security;
using KingV.Core.Web;
using Microsoft.Extensions.Options;
using Wuxi.Shengshen.Erp.ApiService.Configuration;
using Wuxi.Shengshen.Erp.ApiService.Constants.User;
using Wuxi.Shengshen.Erp.ApiService.Data.Requests.User;
using Wuxi.Shengshen.Erp.ApiService.Data.Responses.User;
using Wuxi.Shengshen.Erp.ApiService.Domain.User;
using Wuxi.Shengshen.Erp.ApiService.Repository.Impl;
using Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;
using Wuxi.Shengshen.Erp.ApiService.Service.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Service.Impl;

/// <summary>
/// 用户服务（对应 Java UserServiceImpl）。
/// 关键语义对齐：
/// 1. 创建用户密码统一为配置项 <see cref="UserOptions.DefaultPassword"/>（Java 端为 DefaultPasswordParam 硬编码常量，迁移时改为 appsettings 可配），按 <c>PasswordUtil.Encode(id, plaintext)</c> 计算 BCrypt；
/// 2. <c>Account = Email</c>（Java 端硬约束，C# 在创建/编辑后强制覆盖）；
/// 3. 角色绑定走全量替换：创建后批量插入、编辑先清后插、批量绑定预检重复、批量解绑按 (roleId + userIds) 软删；
/// 4. enable 与实体 is_disable 恒为取反关系。
/// 跨模块依赖（Department 校验、Role 校验与名称聚合、Resource 按钮 tag 列表）
/// 在依赖模块迁移前以最小占位返回（见方法注释）。
/// </summary>
public sealed class UserService : IUserService
{
    /// <summary>用户仓储。</summary>
    private readonly IUserRepository _userRepository;

    /// <summary>用户-角色关联仓储（user_role_mp）。</summary>
    private readonly IUserRoleMpRepository _userRoleMpRepository;

    /// <summary>用户模块配置（默认初始密码等可调参数）。</summary>
    private readonly IOptions<UserOptions> _userOptions;

    /// <summary>注入仓储与配置。</summary>
    /// <param name="userRepository">用户仓储。</param>
    /// <param name="userRoleMpRepository">用户-角色关联仓储。</param>
    /// <param name="userOptions">用户模块配置。</param>
    public UserService(
        IUserRepository userRepository,
        IUserRoleMpRepository userRoleMpRepository,
        IOptions<UserOptions> userOptions)
    {
        _userRepository = userRepository;
        _userRoleMpRepository = userRoleMpRepository;
        _userOptions = userOptions;
    }

    /// <summary>
    /// 创建用户（对齐 Java <c>UserServiceImpl.createUser</c>）：
    /// 1. Facet 反向映射生成新实体；
    /// 2. <c>Account = Email</c>（Java 硬约束）；
    /// 3. 由 RepositoryBase.InsertAsync 自动生成雪花 ID 并回填到 entity.Id；
    /// 4. 用雪花 ID + 默认密码按 BCrypt 哈希回填 Password 字段；
    /// 5. 入库；
    /// 6. 若请求带角色 ID 列表，则批量插入 user_role_mp 关联。
    /// </summary>
    /// <param name="request">创建请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新用户雪花 ID。</returns>
    public async Task<long> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var entity = request.ToSource();
        // Java: user.setAccount(user.getEmail()) —— 强制以邮箱作为登录账号。
        entity.Account = entity.Email;

        await _userRepository.InsertAsync(entity, cancellationToken);
        // 雪花 ID 已由 RepositoryBase.FillForInsert 回填到 entity.Id，再用默认密码 + id 计算 BCrypt。
        entity.Password = PasswordUtil.Encode(entity.Id, _userOptions.Value.DefaultPassword);
        await _userRepository.UpdateAsync(entity, cancellationToken);

        if (request.RoleIds is { Length: > 0 })
        {
            var uniqueRoleIds = request.RoleIds.Distinct().ToArray();
            // 注：Java 端先按 roleRepository.getListByIds 校验角色数量再插入；Role 模块尚未迁移，
            // 本期跳过该校验（依赖模块迁移后补 roleRepository 注入 + 校验）。
            await uniqueRoleIds.ForEachAsync(async roleId =>
                await _userRoleMpRepository.InsertAsync(
                    new UserRoleMp { UserId = entity.Id, RoleId = roleId }, cancellationToken));
        }

        return entity.Id;
    }

    /// <summary>
    /// 编辑用户（对齐 Java <c>UserServiceImpl.editUser</c>）：
    /// 1. 加载实体 → ApplyToSource 覆盖字段；
    /// 2. <c>Account = Email</c>（Java 硬约束）；
    /// 3. 若请求带角色 ID 列表，先清后插全量替换 user_role_mp。
    /// </summary>
    /// <param name="request">编辑请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task EditAsync(EditUserRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _userRepository.GetByIdAsync(request.Id!.Value, cancellationToken)
            ?? throw UserErrorMessages.NotFound.NotFound();

        request.ApplyToSource(entity);
        // Java: user.setAccount(user.getEmail()) —— 强制以邮箱作为登录账号。
        entity.Account = entity.Email;
        await _userRepository.UpdateAsync(entity, cancellationToken);

        if (request.RoleIds is not null)
        {
            // 先清后插：Java 端 userRoleMpRepository.removeUserBind(id) + saveBatch(mpList)。
            await _userRoleMpRepository.LogicDeleteByUserIdAsync(entity.Id, cancellationToken);
            var uniqueRoleIds = request.RoleIds.Distinct().ToArray();
            if (uniqueRoleIds.Length > 0)
            {
                await uniqueRoleIds.ForEachAsync(async roleId =>
                    await _userRoleMpRepository.InsertAsync(
                        new UserRoleMp { UserId = entity.Id, RoleId = roleId }, cancellationToken));
            }
        }
    }

    /// <summary>
    /// 逻辑删除用户（对齐 Java <c>UserServiceImpl.deleteUser</c>）。
    /// Java 端通过 <c>DeleteUserEvent</c> 触发 <c>UserRoleMpListener</c> 清理 user_role_mp；
    /// 本期不引入事件总线，由 Service 层显式调用 <see cref="IUserRoleMpRepository.LogicDeleteByUserIdAsync"/> 完成同等清理。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task RemoveAsync(long id, CancellationToken cancellationToken = default)
    {
        _ = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw UserErrorMessages.NotFound.NotFound();

        await _userRepository.LogicDeleteAsync(id, cancellationToken);
        // 同步清理用户-角色关联，避免遗留孤立绑定（对齐 Java UserRoleMpListener.onDeleteUserEvent 语义）。
        await _userRoleMpRepository.LogicDeleteByUserIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// 启用/禁用切换（对齐 Java <c>UserServiceImpl.enabledUser</c>：is_disable 取反）。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ToggleEnabledAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw UserErrorMessages.NotFound.NotFound();

        entity.IsDisable = !entity.IsDisable;
        await _userRepository.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 管理员修改用户密码（对齐 Java <c>UserServiceImpl.editUserPassword</c>）。
    /// 按 <c>PasswordUtil.Encode(id, plaintext)</c> 计算 BCrypt 后落库，不校验旧密码。
    /// </summary>
    /// <param name="request">修改密码请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task EditUserPasswordAsync(EditUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw UserErrorMessages.NotFound.NotFound();

        entity.Password = PasswordUtil.Encode(entity.Id, request.Password);
        await _userRepository.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 当前登录用户重置自己的密码（对齐 Java <c>UserServiceImpl.resetCurrentUserPassword</c>）：
    /// 1. 校验新密码不能与当前密码相同（BCrypt 比对：<c>PasswordUtil.Matches(id, plaintext, hashed)</c>）；
    /// 2. 计算新密码 BCrypt 后落库。
    /// </summary>
    /// <param name="userId">当前登录用户 ID。</param>
    /// <param name="request">重置请求（新明文密码）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ResetCurrentUserPasswordAsync(
        long userId, ResetCurrentUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw UserErrorMessages.NotFound.NotFound();

        if (PasswordUtil.Matches(userId, request.Password, entity.Password))
        {
            throw UserErrorMessages.PasswordSameAsOld.ParameterError();
        }

        entity.Password = PasswordUtil.Encode(userId, request.Password);
        await _userRepository.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>
    /// 按 ID 查询用户详情（对齐 Java <c>UserServiceImpl.getUser</c>）。
    /// Password 不回传——安全红线；含已绑定角色 ID 列表。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户详情。</returns>
    public async Task<UserDetailResponse> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw UserErrorMessages.NotFound.NotFound();

        var detail = entity.ToFacet<User, UserDetailResponse>();
        // RoleIds 不在 User 实体上，由 Service 层独立查询后附加到响应侧（对齐 Java vo.setRoleIds）。
        detail.RoleIds = await _userRoleMpRepository.GetRoleIdsByUserIdAsync(id, cancellationToken);
        return detail;
    }

    /// <summary>
    /// 查询当前登录用户信息（对齐 Java <c>UserServiceImpl.getCurrentUser</c>）：
    /// 1. 返回用户实体（密码不回传）；
    /// 2. 角色名列表（依赖 Role 模块迁移，本期返回空列表占位）；
    /// 3. 按钮 tag 列表（依赖 Resource 模块迁移，本期返回空列表占位）。
    /// </summary>
    /// <param name="userId">当前登录用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前用户信息。</returns>
    public async Task<GetCurrentUserResponse> GetCurrentUserAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        var entity = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw UserErrorMessages.NotFound.NotFound();

        var response = entity.ToFacet<User, GetCurrentUserResponse>();
        // RoleList / TagList 待 Role / Resource 模块迁移后接入；Java 端无角色时默认为 ["用户"]，
        // 当前阶段模块未迁移，统一以空列表占位，前端按需展示。
        response.RoleList = [];
        response.TagList = [];
        return response;
    }

    /// <summary>
    /// 分页查询用户列表（对齐 Java <c>UserServiceImpl.userList</c>）：
    /// 姓名/账号/邮箱 模糊 + 部门精确；排序字段经白名单校验；
    /// 角色名聚合（按 user_role_mp 关联）由 Service 层在 Facet 映射完成后按"角色名以逗号拼接"回填到 <see cref="UserListItemResponse.RoleName"/>。
    /// size = -1 取全部由仓储层统一归一化。
    /// </summary>
    /// <param name="request">列表请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页结果。</returns>
    public async Task<PageResult<UserListItemResponse>> GetListAsync(
        GetUserListRequest request, CancellationToken cancellationToken = default)
    {
        var (records, total) = await _userRepository.PageAsync(
            request.Name, request.Account, request.Email, request.DepartmentId,
            request.OrderField, request.OrderSort,
            (int)request.Current, (int)request.Size, cancellationToken);

        var list = records
            .SelectFacets<User, UserListItemResponse>()
            .ToList();

        // 角色名聚合：按 (userId -> 角色名列表) 映射后逐行拼接。
        // 注：本期未迁移 Role 模块，无法获取角色名；仅在后续 Role 模块接入后由 Service 层补充聚合。
        // 这里仅按 ID 列表生成占位（实际展示为空字符串）。

        return PageResult<UserListItemResponse>.Of(list, total);
    }

    /// <summary>
    /// 按角色分页查询用户列表（对齐 Java <c>UserServiceImpl.getUserListByRole</c>）：
    /// 1. 校验 roleId 必填；
    /// 2. 列出所有用户（按姓名/部门过滤），并标出是否已绑定该角色；
    /// 3. DepartmentName / RoleId 校验依赖 Department / Role 模块，本期返回占位。
    /// </summary>
    /// <param name="request">列表请求（角色 ID 必填）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页结果。</returns>
    public async Task<PageResult<GetUserListByRoleItemResponse>> GetUserListByRoleAsync(
        GetUserListByRoleRequest request, CancellationToken cancellationToken = default)
    {
        // 借用户列表查询复用底层过滤；size=-1 取全部由仓储层归一化。
        var (records, total) = await _userRepository.PageAsync(
            request.Name, null, null, request.DepartmentId,
            "create_time", "desc",
            (int)request.Current, (int)request.Size, cancellationToken);

        var list = records.SelectFacets<User, GetUserListByRoleItemResponse>().ToList();

        // 角色绑定标志：按 (userId -> 是否已绑定 roleId) 一次性回填。
        var userIds = list.Select(x => x.Id).ToList();
        if (userIds.Count > 0)
        {
            var existing = await _userRoleMpRepository.SelectExistingBindingsAsync(request.RoleId, userIds, cancellationToken);
            var bindSet = new HashSet<long>(existing.Select(e => e.UserId));
            await list.ForEachAsync(async item =>
            {
                item.Bind = bindSet.Contains(item.Id);
                await Task.CompletedTask;
            });
        }

        // IsBind 二次过滤：本期未迁移原生 SQL JOIN，按当前页 Bind 标志过滤（已知差异：total 仍为全量用户数，
        // 实际过滤后记录数小于 total 时前端需重新翻页；Role 模块接入后改为 SQL 过滤 + 独立 count 查询）。
        if (request.IsBind.HasValue)
        {
            var flag = request.IsBind.Value;
            list = list.Where(x => x.Bind == flag).ToList();
        }

        // Department 名称依赖 Department 模块迁移后接入，本期保留 null。

        return PageResult<GetUserListByRoleItemResponse>.Of(list, total);
    }

    /// <summary>
    /// 查询下拉列表（仅启用中的用户，create_time 倒序全量返回）。
    /// </summary>
    /// <param name="request">下拉请求（参数保留以对齐 Java body 可选；本实现忽略）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下拉项列表。</returns>
    public async Task<List<UserDownListItemResponse>> GetDownListAsync(
        GetUserDownListRequest? request,
        CancellationToken cancellationToken = default)
    {
        var records = await _userRepository.GetDownListAsync(cancellationToken);
        return [.. records.SelectFacets<User, UserDownListItemResponse>()];
    }

    /// <summary>
    /// 将一个角色批量绑定到多个用户（对齐 Java <c>UserServiceImpl.userBingRole</c>）：
    /// 1. 校验用户全部存在（任一缺失抛 <see cref="UserErrorMessages.NotFound"/>）；
    /// 2. 校验角色存在（Role 模块未迁移，本期跳过此校验，依赖模块迁移后补 roleRepository 注入）；
    /// 3. 预检查是否有用户已绑定该角色（<see cref="UserRoleMpRepository.SelectExistingBindingsAsync"/>），有则抛"有用户重复关联"；
    /// 4. 批量插入 user_role_mp。
    /// </summary>
    /// <param name="request">绑定请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task BindRoleToUsersAsync(UserBingRoleRequest request, CancellationToken cancellationToken = default)
    {
        var userIds = request.UserIds.Distinct().ToList();
        await userIds.ForEachAsync(async userId =>
        {
            _ = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw UserErrorMessages.NotFound.NotFound();
        });

        // 预检查重复绑定（对齐 Java selectUserRoleMp 后判非空抛"有用户重复关联"）。
        var existing = await _userRoleMpRepository.SelectExistingBindingsAsync(request.RoleId, userIds, cancellationToken);
        if (existing.Count > 0)
        {
            throw UserErrorMessages.UserRoleDuplicate.ParameterError();
        }

        await userIds.ForEachAsync(async userId =>
            await _userRoleMpRepository.InsertAsync(
                new UserRoleMp { UserId = userId, RoleId = request.RoleId }, cancellationToken));
    }

    /// <summary>
    /// 将一个角色从多个用户批量解绑（对齐 Java <c>UserServiceImpl.userUnbindRole</c>）：
    /// 1. 校验用户全部存在；
    /// 2. 校验角色存在（Role 模块未迁移，本期跳过）；
    /// 3. 一次性按 (roleId + userIds IN) 批量逻辑删除 user_role_mp。
    /// </summary>
    /// <param name="request">解绑请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task UnbindRoleFromUsersAsync(UserBingRoleRequest request, CancellationToken cancellationToken = default)
    {
        var userIds = request.UserIds.Distinct().ToList();
        await userIds.ForEachAsync(async userId =>
        {
            _ = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw UserErrorMessages.NotFound.NotFound();
        });

        await _userRoleMpRepository.BulkUnbindAsync(request.RoleId, userIds, cancellationToken);
    }
}
