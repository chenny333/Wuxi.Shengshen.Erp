using KingV.Core.Security;
using KingV.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Wuxi.Shengshen.Erp.ApiService.Data.Requests.User;
using Wuxi.Shengshen.Erp.ApiService.Service.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Endpoint;

/// <summary>
/// 用户端点（对应 Java UserWebController，路由与 HTTP 方法逐一对齐；前端契约零改动）。
/// 全部需登录访问（除显式标注 AllowAnonymous 的接口）；响应由 /api 分组的 ApiResponseEndpointFilter 自动包信封。
/// 路径与 HTTP 方法严格对齐 Java <c>UserWebController</c>：createUser / editUser / getUser /
/// getCurrentUser / deleteUser / id（enabledUser）/ userList / userListByRole / userBingRole /
/// userUnbindRole / getUserDownList / resetCurrentUserPassword / editUserPassword。
/// </summary>
public static class UserEndpoint
{
    /// <summary>
    /// 映射用户端点（挂 /api 前缀）。
    /// </summary>
    public static RouteGroupBuilder MapUserEndpoint(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/user/web").WithTags("用户管理");

        group.MapPost("/createUser", Create)
            .WithName("CreateUser")
            .WithSummary("创建用户");

        group.MapPut("/editUser", Edit)
            .WithName("EditUser")
            .WithSummary("修改用户");

        group.MapDelete("/deleteUser", Remove)
            .WithName("DeleteUser")
            .WithSummary("删除用户");

        group.MapGet("/getUser", Get)
            .WithName("GetUser")
            .WithSummary("用户详情");

        group.MapGet("/getCurrentUser", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("当前登录用户信息");

        group.MapGet("/id", ToggleEnabled)
            .WithName("EnabledUser")
            .WithSummary("启用/禁用用户");

        group.MapPost("/userList", GetList)
            .WithName("GetUserList")
            .WithSummary("用户分页列表");

        group.MapPost("/userListByRole", GetUserListByRole)
            .WithName("GetUserListByRole")
            .WithSummary("按角色分页查询用户列表");

        group.MapPost("/userBingRole", BindRoleToUsers)
            .WithName("UserBingRole")
            .WithSummary("将指定角色绑定到一批用户");

        group.MapPut("/userUnbindRole", UnbindRoleFromUsers)
            .WithName("UserUnbindRole")
            .WithSummary("将指定角色从一批用户解绑");

        group.MapPost("/getUserDownList", GetDownList)
            .WithName("GetUserDownList")
            .WithSummary("用户下拉列表");

        group.MapPost("/resetCurrentUserPassword", ResetCurrentUserPassword)
            .WithName("ResetCurrentUserPassword")
            .WithSummary("当前登录用户重置自己的密码");

        group.MapPost("/editUserPassword", EditUserPassword)
            .WithName("EditUserPassword")
            .WithSummary("管理员修改用户密码");

        return group;
    }

    /// <summary>
    /// 无数据成功响应：直接返回无参，
    /// 信封过滤器会把 2xx 空结果包装为 { status:"200", message:"OK", data:null, requestId:"..." }（对齐 Java 端格式）。
    /// </summary>
    private static IResult EmptyOk() => Results.Ok();

    /// <summary>创建用户（POST createUser）。</summary>
    private static async Task<IResult> Create(
        [FromBody] CreateUserRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        var id = await service.CreateAsync(request, cancellationToken);
        return Results.Ok(id);
    }

    /// <summary>修改用户（PUT editUser）。</summary>
    private static async Task<IResult> Edit(
        [FromBody] EditUserRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        await service.EditAsync(request, cancellationToken);
        return EmptyOk();
    }

    /// <summary>删除用户（DELETE deleteUser?id=），逻辑删除。</summary>
    private static async Task<IResult> Remove(
        [FromQuery] long id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        await service.RemoveAsync(id, cancellationToken);
        return EmptyOk();
    }

    /// <summary>用户详情（GET getUser?id=）。</summary>
    private static async Task<IResult> Get(
        [FromQuery] long id,
        IUserService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetAsync(id, cancellationToken));

    /// <summary>当前登录用户信息（GET getCurrentUser，无参）。</summary>
    private static async Task<IResult> GetCurrentUser(
        IUserService service,
        CancellationToken cancellationToken)
    {
        var userId = UserContext.Current!.Id;
        return Results.Ok(await service.GetCurrentUserAsync(userId, cancellationToken));
    }

    /// <summary>启用/禁用切换（GET id?id=，对齐 Java 端路径"/id"——前端契约要求）。</summary>
    private static async Task<IResult> ToggleEnabled(
        [FromQuery] long id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        await service.ToggleEnabledAsync(id, cancellationToken);
        return EmptyOk();
    }

    /// <summary>用户分页列表（POST userList）。</summary>
    private static async Task<IResult> GetList(
        [FromBody] GetUserListRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        return Results.Ok(await service.GetListAsync(request, cancellationToken));
    }

    /// <summary>按角色分页查询用户列表（POST userListByRole）。</summary>
    private static async Task<IResult> GetUserListByRole(
        [FromBody] GetUserListByRoleRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        return Results.Ok(await service.GetUserListByRoleAsync(request, cancellationToken));
    }

    /// <summary>将指定角色绑定到一批用户（POST userBingRole）。</summary>
    private static async Task<IResult> BindRoleToUsers(
        [FromBody] UserBingRoleRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        await service.BindRoleToUsersAsync(request, cancellationToken);
        return EmptyOk();
    }

    /// <summary>将指定角色从一批用户解绑（PUT userUnbindRole）。</summary>
    private static async Task<IResult> UnbindRoleFromUsers(
        [FromBody] UserBingRoleRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        await service.UnbindRoleFromUsersAsync(request, cancellationToken);
        return EmptyOk();
    }

    /// <summary>用户下拉列表（POST getUserDownList，body 可选）。</summary>
    private static async Task<IResult> GetDownList(
        [FromBody] GetUserDownListRequest? request,
        IUserService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetDownListAsync(request, cancellationToken));

    /// <summary>当前登录用户重置自己的密码（POST resetCurrentUserPassword）。</summary>
    private static async Task<IResult> ResetCurrentUserPassword(
        [FromBody] ResetCurrentUserPasswordRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        var userId = UserContext.Current!.Id;
        await service.ResetCurrentUserPasswordAsync(userId, request, cancellationToken);
        return EmptyOk();
    }

    /// <summary>管理员修改用户密码（POST editUserPassword）。</summary>
    private static async Task<IResult> EditUserPassword(
        [FromBody] EditUserPasswordRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        await service.EditUserPasswordAsync(request, cancellationToken);
        return EmptyOk();
    }
}
