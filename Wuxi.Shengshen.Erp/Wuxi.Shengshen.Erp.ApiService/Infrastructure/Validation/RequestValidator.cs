using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Wuxi.Shengshen.Erp.ApiService.Infrastructure.Validation;

/// <summary>
/// 触发 DataAnnotations 校验（record 请求模型 init 后需手动触发，相当于 Java 的 @Valid）。
/// </summary>
public static class RequestValidator
{
    /// <summary>
    /// 校验请求对象，含复杂嵌套成员递归校验；失败抛出 <see cref="KingV.Core.Exceptions.BusinessException"/>（400）。
    /// </summary>
    /// <param name="instance">待校验对象。</param>
    public static void Validate(object? instance)
    {
        if (instance is null) return;
        ValidateObject(instance, new HashSet<object>());
    }

    private static void ValidateObject(object instance, HashSet<object> visited)
    {
        if (instance is null) return;
        var type = instance.GetType();
        if (type.IsValueType || instance is string) return;
        // 防止循环引用导致死递归
        if (!visited.Add(instance)) return;

        // 收集全部校验错误，合并为一条业务异常（400），避免被全局异常中间件当 500。
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
        {
            var message = results
                .Select(r => r.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "参数校验失败";
            throw new KingV.Core.Exceptions.BusinessException(message);
        }

        // 递归校验复杂成员（集合与自定义类型）
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
            var value = prop.GetValue(instance);
            if (value is null) continue;

            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is not null) ValidateObject(item, visited);
                }
            }
            else if (!prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
            {
                ValidateObject(value, visited);
            }
        }
    }
}