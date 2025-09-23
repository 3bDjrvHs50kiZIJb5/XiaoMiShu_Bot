using TelegramBotBase.Interfaces;
using Telegram.Bot.Types.Enums;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 帮助命令处理类 - 显示机器人使用说明
    /// </summary>
    public class HelpCommand : BaseCommand
    {
        /// <summary>
        /// 执行帮助命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            var helpMessage = "";
            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "README_BOT_USAGE.md");

                if (File.Exists(filePath))
                {
                    helpMessage = await File.ReadAllTextAsync(filePath);
                }
                else
                {
                    helpMessage = "README_BOT_USAGE.md 文件未找到";
                }

            }
            catch (Exception ex)
            {
                helpMessage = $"读取文件时出错: {ex.Message}";
                _logger.LogError($"处理帮助命令失败: {ex.Message}");
            }

            await SendTempMessageAsync(device, helpMessage);
        }
    }
}