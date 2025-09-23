using BootstrapBlazor.Components;
using TelegramBotBase.Attributes;
using TelegramBotBase.Base;
using TelegramBotBase.Controls.Hybrid;
using TelegramBotBase.Controls.Inline;
using TelegramBotBase.Form;
using back.TelegramBot.Services;
using TelegramBotBase.Args;
using Telegram.Bot.Types.Enums;
using TelegramBotBase;
using back.Entities.JZ;
using back.Entities.Setting;
using FreeSql;
using OrderManagementService = back.TelegramBot.Services.OrderManagementService;
using back.TelegramBot.Utils;

namespace back.TelegramBot.Forms
{
    /// <summary>
    /// 机器人的开始表单 - 用户首次与机器人交互时显示
    /// 现在使用分离的服务来处理各种功能，提高代码的可维护性
    /// </summary>
    public class StartForm : GroupForm
    {
        // 各种服务实例
        private readonly ChatManagementService _chatService;
        private readonly MessageHandler _messageHandler;
        private readonly DataInitializationService _dataInitService;
        private readonly ILogger<StartForm> _logger;

        /// <summary>
        /// 构造函数 - 初始化所有需要的服务
        /// </summary>
        public StartForm()
        {
            // 通过服务定位器获取 FreeSqlCloud 实例
            var fsql = ServiceLocator.ServiceProvider?.GetService<FreeSqlCloud>();

            if (fsql == null)
            {
                throw new InvalidOperationException("无法获取 FreeSqlCloud 服务实例");
            }

            // 初始化各种服务
            _dataInitService = ServiceLocator.ServiceProvider?.GetService<DataInitializationService>();
            _messageHandler = ServiceLocator.ServiceProvider?.GetService<MessageHandler>();
            _chatService = ServiceLocator.ServiceProvider?.GetService<ChatManagementService>();
            _logger = ServiceLocator.ServiceProvider?.GetService<ILogger<StartForm>>();
        }

        /// <summary>
        /// 处理群成员变化事件（加入或离开群组）
        /// </summary>
        /// <param name="e">成员变化事件参数</param>
        public override async Task OnMemberChanges(MemberChangeEventArgs e)
        {
            try
            {
                // 使用群组管理服务处理成员变化
                await _chatService.HandleMemberChangesAsync(Device, e.Type, e.Members);
            }
            catch (Exception ex)
            {
                // 记录错误日志
                _logger.LogError($"处理成员变化事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理普通消息
        /// </summary>
        /// <param name="e">消息结果对象</param>
        public override async Task OnMessage(MessageResult e)
        {
            try
            {
                // 使用消息处理服务统一处理所有消息
                await _messageHandler.HandleMessageAsync(Device, e);
            }
            catch (Exception ex)
            {
                // 记录错误日志
                _logger.LogError($"处理消息失败: {ex.Message}");
                await DeviceHelper.SendTempMessageAsync(Device, "处理消息时出现错误，请稍后重试");
            }
        }

        /// <summary>
        /// 处理按钮点击等交互动作
        /// </summary>
        /// <param name="message">消息结果对象</param>
        public override async Task Action(MessageResult message)
        {

            await message.ConfirmAction();

            switch (message.RawData ?? "")
            {
                // 查询命令
                case "help":
                    message.Message.Text = "/help";
                    await _messageHandler.HandleMessageAsync(Device, message);
                    break;

                // 检测发布情况
                case "start":
                    message.Message.Text = "/start";
                    await _messageHandler.HandleMessageAsync(Device, message);
                    break;

                // 积分充值
                case "recharge":
                    break;

                // 个人中心
                case "user_center":
                    break;

                case "ButtonGridTagForm":
                    // var bgtf = new ButtonGridTagForm();
                    // await NavigateTo(bgtf);
                    break;
            }
        }

        /// <summary>
        /// 渲染表单 - 初始化群组和用户数据
        /// </summary>
        /// <param name="message">消息结果对象</param>
        public override async Task Render(MessageResult message)
        {
            try
            {
                // 使用数据初始化服务进行完整的初始化流程
                await _dataInitService.PerformFullInitializationAsync(Device, message.Message);

            }
            catch (Exception ex)
            {
                // 记录错误日志
                _logger.LogError($"渲染表单失败: {ex.Message}");
                await DeviceHelper.SendTempMessageAsync(Device, "初始化失败，请稍后重试");
            }
        }
    }
}