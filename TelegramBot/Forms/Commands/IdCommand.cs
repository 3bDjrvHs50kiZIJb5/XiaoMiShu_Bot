using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// ID查询命令处理类
    /// </summary>
    public class IdCommand : BaseCommand
    {
        /// <summary>
        /// 执行ID查询命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                var msg = await _chatService.GetUserInfoAsync(device, message);
                await SendTempMessageAsync(device, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理ID查询命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "获取用户信息失败，请稍后重试");
            }
        }
    }
}