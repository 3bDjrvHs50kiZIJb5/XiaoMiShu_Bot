using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 费率查询命令处理类
    /// </summary>
    public class FeeRateCommand : BaseCommand
    {
        /// <summary>
        /// 执行费率查询命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                var chat = await _chatService.GetChatAsync(device, message.Chat.Id);
                await SendTempMessageAsync(device, $"当前费率：{chat.FeeRate * 100}%");
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理费率查询命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "获取费率失败，请稍后重试");
            }
        }
    }
}