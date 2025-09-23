using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 清除今日订单命令处理类
    /// </summary>
    public class ClearTodayOrderCommand : BaseCommand
    {
        /// <summary>
        /// 执行清除今日订单命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                await _orderService.ClearTodayOrderAsync(device, message.Chat.Id);
                await SendTempMessageAsync(device, "今日账单已清除");
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理清除今日订单命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "清除今日订单失败，请稍后重试");
            }
        }
    }
}