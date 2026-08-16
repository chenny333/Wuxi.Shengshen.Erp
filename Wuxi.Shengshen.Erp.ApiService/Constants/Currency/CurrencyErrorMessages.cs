namespace Wuxi.Shengshen.Erp.ApiService.Constants.Currency;

/// <summary>
/// 币种管理模块错误消息常量（经业务异常信封返回，前端 toast 直接展示；措辞与 Java 端保持一致）。
/// </summary>
public static class CurrencyErrorMessages
{
    /// <summary>按 ID 未找到币种记录（编辑/切换启用/详情查询共用）。</summary>
    public const string NotFound = "币种管理不存在";
}
