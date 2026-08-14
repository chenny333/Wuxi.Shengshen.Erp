namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 标记端点匿名可访问（对应 Java @AllowAnonymous），置于 Minimal API 端点元数据。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AllowAnonymousAttribute : Attribute;