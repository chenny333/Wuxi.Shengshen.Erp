using System.ComponentModel.DataAnnotations;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Requests.Login
{
    /// <summary>
    /// 登录请求
    /// </summary>
    public record LoginRequest
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "账号不能为空")]
        public required string Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        public required string Password { get; set; }

        /// <summary>
        /// 验证码
        /// </summary>
        [Required(ErrorMessage = "验证码不能为空")]
        public required string AuthCode { get; set; }

        /// <summary>
        /// 验证码的盐
        /// </summary>
        [Required(ErrorMessage = "盐值不能为空")]
        public required string Salt { get; set; }
    }
}
