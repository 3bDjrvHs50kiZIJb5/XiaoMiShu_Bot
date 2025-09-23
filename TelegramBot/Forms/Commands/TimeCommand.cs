using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 时间查询命令处理类
    /// </summary>
    public class TimeCommand : BaseCommand
    {
        /// <summary>
        /// 执行时间查询命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                var time = DateTime.Now.ToString();
                await SendTempMessageAsync(device, time);
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理时间查询命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "获取时间失败，请稍后重试");
            }
        }
    }
}