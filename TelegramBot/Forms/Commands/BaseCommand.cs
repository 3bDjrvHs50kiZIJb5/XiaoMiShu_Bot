using TelegramBotBase.Interfaces;
using back.TelegramBot.Services;
using TelegramBotBase.Form;
using TelegramBotBase.Base;
using Telegram.Bot.Types.Enums;
using back.TelegramBot.Utils;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 基础命令抽象类 - 提供所有命令处理类的通用功能
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        // 受保护的服务字段，供子类使用
        protected readonly MemberManagementService _memberService;
        protected readonly ChatManagementService _chatService;
        protected readonly OrderManagementService _orderService;
        protected readonly MessageManagementService _messageService;
        protected readonly CommandConfigService _commandConfigService;
        protected readonly ILogger<BaseCommand> _logger;
        protected readonly IConfiguration _configuration;

        /// <summary>
        /// 构造函数 - 初始化所有依赖的服务
        /// </summary>
        protected BaseCommand()
        {
            _memberService = ServiceLocator.ServiceProvider?.GetService<MemberManagementService>();
            _chatService = ServiceLocator.ServiceProvider?.GetService<ChatManagementService>();
            _orderService = ServiceLocator.ServiceProvider?.GetService<OrderManagementService>();
            _messageService = ServiceLocator.ServiceProvider?.GetService<MessageManagementService>();
            _commandConfigService = ServiceLocator.ServiceProvider?.GetService<CommandConfigService>();
            _logger = ServiceLocator.ServiceProvider?.GetService<ILogger<BaseCommand>>();
            _configuration = ServiceLocator.ServiceProvider?.GetService<IConfiguration>();
        }

        /// <summary>
        /// 抽象方法 - 子类必须实现具体的命令处理逻辑
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        public abstract Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message);

        /// <summary>
        /// 创建通用按钮表单 - 包含帮助和邀请到群组按钮
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <returns>配置好的按钮表单</returns>
        protected Task<ButtonForm> CreateCommonButtonFormAsync(IDeviceSession device)
        {
            var bf = new ButtonForm();

            // 动态获取机器人用户名
            var botUsername = _configuration["TelegramBot:BotName"];
            try
            {
                var startParameter = "start";
                string inviteToGroupUrl = $"https://t.me/{botUsername}?startgroup={startParameter}";

                bf.AddButtonRow(
                    new ButtonBase("帮助", "help"),
                    new ButtonBase("邀请到群组", "invite_to_group", inviteToGroupUrl),
                    new ButtonBase("返回首页", "start")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取机器人信息失败: {ex.Message}");
                // 如果获取失败，只添加帮助按钮
                // bf.AddButtonRow(new ButtonBase("帮助", "help"));
                
            }

            // bf.AddButtonRow(
            //     new ButtonBase("广告位1", "ad1", $"https://t.me/{botUsername}"),
            //     new ButtonBase("广告位2", "ad2", $"https://t.me/{botUsername}")
            // );
            return Task.FromResult(bf);
        }


        /// <summary>
        /// 发送临时消息 - 发送后延迟1秒自动删除
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">要发送的消息内容</param>
        /// <returns>发送的消息对象</returns>
        protected async Task SendTempMessageAsync(IDeviceSession device, string message)
        {
            var buttonForm = await CreateCommonButtonFormAsync(device);
            await DeviceHelper.SendTempMessageAsync(device, message, buttonForm);
        }
    }
}