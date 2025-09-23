using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace back.TelegramBot.Services
{
    /// <summary>
    /// 命令配置服务 - 负责加载和管理命令配置
    /// </summary>
    public class CommandConfigService
    {
        private readonly ILogger<CommandConfigService> _logger;
        private Dictionary<string, CommandConfig> _commands;
        private readonly string _configPath;

        /// <summary>
        /// 构造函数 - 初始化命令配置服务
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public CommandConfigService(ILogger<CommandConfigService> logger)
        {
            _logger = logger;
            _configPath = Path.Combine(AppContext.BaseDirectory, "cmd.json");
            _commands = new Dictionary<string, CommandConfig>();
            LoadCommandConfig();
        }

        /// <summary>
        /// 加载命令配置文件
        /// </summary>
        private void LoadCommandConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    _logger.LogWarning($"命令配置文件不存在: {_configPath}");
                    return;
                }

                var jsonContent = File.ReadAllText(_configPath);
                var configRoot = JsonSerializer.Deserialize<CommandConfigRoot>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (configRoot?.Commands != null)
                {
                    _commands = configRoot.Commands;
                    _logger.LogInformation($"成功加载 {_commands.Count} 个命令配置");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"加载命令配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据消息文本匹配命令
        /// </summary>
        /// <param name="messageText">消息文本</param>
        /// <returns>匹配的命令配置，如果没有匹配则返回null</returns>
        public CommandConfig? MatchCommand(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
                return null;

            foreach (var command in _commands.Values)
            {
                foreach (var trigger in command.Triggers)
                {
                    // 对于特殊处理的命令（如订单添加），使用StartsWith匹配
                    if (command.SpecialHandling)
                    {
                        if (messageText.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
                        {
                            return command;
                        }
                    }
                    else
                    {
                        // 普通命令使用精确匹配或StartsWith匹配
                        if (messageText.Equals(trigger, StringComparison.OrdinalIgnoreCase) ||
                            messageText.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
                        {
                            return command;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取所有命令配置
        /// </summary>
        /// <returns>命令配置字典</returns>
        public Dictionary<string, CommandConfig> GetAllCommands()
        {
            return _commands;
        }

        /// <summary>
        /// 重新加载配置文件
        /// </summary>
        public void ReloadConfig()
        {
            LoadCommandConfig();
        }
    }

    /// <summary>
    /// 命令配置根对象
    /// </summary>
    public class CommandConfigRoot
    {
        public Dictionary<string, CommandConfig> Commands { get; set; } = new();
    }

    /// <summary>
    /// 单个命令配置
    /// </summary>
    public class CommandConfig
    {
        /// <summary>
        /// 触发词列表
        /// </summary>
        public List<string> Triggers { get; set; } = new();

        /// <summary>
        /// 处理方法名
        /// </summary>
        public string Handler { get; set; } = string.Empty;

        /// <summary>
        /// 是否需要管理员权限
        /// </summary>
        public bool RequireAdmin { get; set; } = false;

        /// <summary>
        /// 命令描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否需要特殊处理（如订单添加命令）
        /// </summary>
        public bool SpecialHandling { get; set; } = false;
    }
}