using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 汇率查询命令处理类 - 从数据库获取汇率
    /// </summary>
    public class ExchangeRateCommand : BaseCommand
    {
        /// <summary>
        /// 执行汇率查询命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                var exchangeRate = await _chatService.GetExchangeRateAsync(device, message.Chat.Id);
                await SendTempMessageAsync(device, $"当前群汇率：{exchangeRate} CNY/USDT");
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理汇率查询命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "获取汇率失败，请稍后重试");
            }
        }
    }
}