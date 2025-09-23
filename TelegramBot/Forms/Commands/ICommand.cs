using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 命令处理接口 - 所有命令处理类都需要实现此接口
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 执行命令处理
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        /// <returns>处理任务</returns>
        Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message);
    }
}