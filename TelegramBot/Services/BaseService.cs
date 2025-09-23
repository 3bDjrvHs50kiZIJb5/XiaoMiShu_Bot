using FreeSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Services
{
    /// <summary>
    /// 基础服务类 - 提供所有服务的通用功能
    /// 包括依赖注入、日志记录、异常处理等基础功能
    /// </summary>
    /// <typeparam name="T">具体服务类型，用于日志记录</typeparam>
    public abstract class BaseService<T> where T : class
    {
        /// <summary>
        /// FreeSql 数据库云实例
        /// </summary>
        protected readonly FreeSqlCloud _fsql;

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger<T> _logger;

        /// <summary>
        /// 配置服务
        /// </summary>
        protected readonly IConfiguration _configuration;

        /// <summary>
        /// 构造函数 - 初始化基础依赖项
        /// </summary>
        protected BaseService()
        {
            // 从服务定位器获取基础依赖项
            _fsql = ServiceLocator.ServiceProvider?.GetService<FreeSqlCloud>()
                ?? throw new InvalidOperationException("无法获取 FreeSqlCloud 服务");

            _logger = ServiceLocator.ServiceProvider?.GetService<ILogger<T>>()
                ?? throw new InvalidOperationException($"无法获取 ILogger<{typeof(T).Name}> 服务");

            _configuration = ServiceLocator.ServiceProvider?.GetService<IConfiguration>()
                ?? throw new InvalidOperationException("无法获取 IConfiguration 服务");
        }

        /// <summary>
        /// 创建并配置仓储实例
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="enableCascadeSave">是否启用级联保存，默认为true</param>
        /// <returns>配置好的仓储实例</returns>
        protected IBaseRepository<TEntity> CreateRepository<TEntity>(bool enableCascadeSave = true)
            where TEntity : class
        {
            var repository = _fsql.GetRepository<TEntity>();
            repository.DbContextOptions.EnableCascadeSave = enableCascadeSave;
            return repository;
        }

        /// <summary>
        /// 安全执行异步操作 - 统一的异常处理和日志记录
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称，用于日志记录</param>
        /// <param name="defaultResult">操作失败时的默认返回值</param>
        /// <returns>操作结果</returns>
        protected async Task<TResult> SafeExecuteAsync<TResult>(
            Func<Task<TResult>> operation,
            string operationName,
            TResult defaultResult = default(TResult))
        {
            try
            {
                _logger.LogDebug($"开始执行操作: {operationName}");
                var result = await operation();
                _logger.LogDebug($"操作执行成功: {operationName}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"操作执行失败: {operationName} - {ex.Message}");
                return defaultResult;
            }
        }

        /// <summary>
        /// 安全执行异步操作并返回操作结果消息
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="successMessage">成功消息</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>操作结果消息</returns>
        protected async Task<string> SafeExecuteWithMessageAsync(
            Func<Task> operation,
            string operationName,
            string successMessage,
            string errorMessage = null)
        {
            try
            {
                _logger.LogDebug($"开始执行操作: {operationName}");
                await operation();
                _logger.LogInformation($"操作执行成功: {operationName}");
                return successMessage;
            }
            catch (Exception ex)
            {
                var finalErrorMessage = errorMessage ?? $"{operationName}失败，请稍后重试";
                _logger.LogError(ex, $"操作执行失败: {operationName} - {ex.Message}");
                return finalErrorMessage;
            }
        }

        /// <summary>
        /// 验证用户ID格式
        /// </summary>
        /// <param name="userIdText">用户ID文本</param>
        /// <param name="userId">解析后的用户ID</param>
        /// <returns>验证是否成功</returns>
        protected bool ValidateUserId(string userIdText, out long userId)
        {
            return long.TryParse(userIdText, out userId);
        }

        /// <summary>
        /// 验证必需参数是否为空
        /// </summary>
        /// <param name="value">要验证的值</param>
        /// <param name="parameterName">参数名称</param>
        /// <returns>验证是否通过</returns>
        protected bool ValidateRequired(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogWarning($"必需参数为空: {parameterName}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="userId">用户ID</param>
        /// <param name="chatId">群组ID</param>
        /// <param name="additionalInfo">附加信息</param>
        protected void LogOperation(string operationName, long? userId = null, long? chatId = null, string additionalInfo = null)
        {
            var logMessage = $"执行操作: {operationName}";

            if (userId.HasValue)
                logMessage += $", 用户ID: {userId}";

            if (chatId.HasValue)
                logMessage += $", 群组ID: {chatId}";

            if (!string.IsNullOrEmpty(additionalInfo))
                logMessage += $", 附加信息: {additionalInfo}";

            _logger.LogInformation(logMessage);
        }

        /// <summary>
        /// 获取服务实例 - 简化依赖获取
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns>服务实例</returns>
        protected TService GetService<TService>() where TService : class
        {
            return ServiceLocator.ServiceProvider?.GetService<TService>()
                ?? throw new InvalidOperationException($"无法获取 {typeof(TService).Name} 服务");
        }

        /// <summary>
        /// 尝试获取服务实例 - 不抛出异常的版本
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <returns>服务实例，如果获取失败则返回null</returns>
        protected TService TryGetService<TService>() where TService : class
        {
            return ServiceLocator.ServiceProvider?.GetService<TService>();
        }
    }
}