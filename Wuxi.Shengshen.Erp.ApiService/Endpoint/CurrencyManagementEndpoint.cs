using KingV.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Wuxi.Shengshen.Erp.ApiService.Data.Requests.Currency;
using Wuxi.Shengshen.Erp.ApiService.Service.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Endpoint;

/// <summary>
/// 币种管理端点（对应 Java CurrencyManagementWebController，路由与前端契约完全一致）。
/// 全部需登录访问；响应由 /api 分组的 ApiResponseEndpointFilter 自动包信封。
/// </summary>
public static class CurrencyManagementEndpoint
{
    /// <summary>
    /// 映射币种管理端点（挂 /api 前缀）。
    /// </summary>
    public static RouteGroupBuilder MapCurrencyManagementEndpoint(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/currencyManagement/web").WithTags("币种管理");

        group.MapPost("/createCurrencyManagement", Create)
            .WithName("CreateCurrencyManagement")
            .WithSummary("创建币种");

        group.MapPut("/editCurrencyManagement", Edit)
            .WithName("EditCurrencyManagement")
            .WithSummary("修改币种");

        group.MapDelete("/removeCurrencyManagement", Remove)
            .WithName("RemoveCurrencyManagement")
            .WithSummary("移除币种");

        group.MapGet("/enabledCurrencyManagement", ToggleEnabled)
            .WithName("EnabledCurrencyManagement")
            .WithSummary("启用/禁用币种");

        group.MapGet("/getCurrencyManagement", Get)
            .WithName("GetCurrencyManagement")
            .WithSummary("币种详情");

        group.MapPost("/getCurrencyManagementList", GetList)
            .WithName("GetCurrencyManagementList")
            .WithSummary("币种分页列表");

        group.MapPost("/getCurrencyManagementDownList", GetDownList)
            .WithName("GetCurrencyManagementDownList")
            .WithSummary("币种下拉列表");

        return group;
    }

    /// <summary>创建币种（POST createCurrencyManagement）。</summary>
    private static async Task<IResult> Create(
        [FromBody] CreateCurrencyManagementRequest request,
        ICurrencyManagementService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        await service.CreateAsync(request, cancellationToken);
        return EmptyOk();
    }

    /// <summary>修改币种（PUT editCurrencyManagement）。</summary>
    private static async Task<IResult> Edit(
        [FromBody] EditCurrencyManagementRequest request,
        ICurrencyManagementService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        await service.EditAsync(request, cancellationToken);
        return EmptyOk();
    }

    /// <summary>移除币种（DELETE removeCurrencyManagement?id=），逻辑删除。</summary>
    private static async Task<IResult> Remove(
        [FromQuery] long id,
        ICurrencyManagementService service,
        CancellationToken cancellationToken)
    {
        await service.RemoveAsync(id, cancellationToken);
        return EmptyOk();
    }

    /// <summary>启用/禁用切换（GET enabledCurrencyManagement?id=）。</summary>
    private static async Task<IResult> ToggleEnabled(
        [FromQuery] long id,
        ICurrencyManagementService service,
        CancellationToken cancellationToken)
    {
        await service.ToggleEnabledAsync(id, cancellationToken);
        return EmptyOk();
    }

    /// <summary>
    /// 无数据成功响应：直接返回无参，
    /// 信封过滤器会把 2xx 空结果包装为 { status:"200", message:"OK", data:null, requestId:"..." }（对齐 Java 端格式）。
    /// </summary>
    private static IResult EmptyOk() => Results.Ok();

    /// <summary>币种详情（GET getCurrencyManagement?id=）。</summary>
    private static async Task<IResult> Get(
        [FromQuery] long id,
        ICurrencyManagementService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetAsync(id, cancellationToken));

    /// <summary>币种分页列表（POST getCurrencyManagementList）。</summary>
    private static async Task<IResult> GetList(
        [FromBody] GetCurrencyManagementListRequest request,
        ICurrencyManagementService service,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);
        return Results.Ok(await service.GetListAsync(request, cancellationToken));
    }

    /// <summary>币种下拉列表（POST getCurrencyManagementDownList）。</summary>
    private static async Task<IResult> GetDownList(
        ICurrencyManagementService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetDownListAsync(cancellationToken));
}
