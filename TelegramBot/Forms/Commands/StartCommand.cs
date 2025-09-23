using TelegramBotBase.Interfaces;
using Telegram.Bot.Types.Enums;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 开始命令处理类
    /// </summary>
    public class StartCommand : BaseCommand
    {
        /// <summary>
        /// 执行开始命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                var startMessage = $"狗狗查查记账机器人（永久免费）";
                await SendTempMessageAsync(device, startMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理开始命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "开始命令处理失败");
            }
        }
    }
}