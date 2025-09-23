using TelegramBotBase.Interfaces;
using Telegram.Bot.Types.Enums;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 设置汇率命令处理类
    /// </summary>
    public class SetExchangeRateCommand : BaseCommand
    {
        /// <summary>
        /// 执行设置汇率命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                // 提取汇率数值：支持 "设置汇率7.15" 格式（无空格）
                string rateText = "";

                // 检查消息是否以"设置汇率"开头
                if (message.Text.StartsWith("设置汇率"))
                {
                    // 提取"设置汇率"后面的数字部分
                    rateText = message.Text.Substring("设置汇率".Length).Trim();
                }

                // 验证是否有汇率数值
                if (string.IsNullOrEmpty(rateText))
                {
                    await SendTempMessageAsync(device, "格式错误，正确格式：\n\n<code>设置汇率7.15</code>");
                    return;
                }

                // 尝试解析汇率数值
                if (!decimal.TryParse(rateText, out decimal rate))
                {
                    await SendTempMessageAsync(device, "汇率格式错误，请输入有效的数字，例如：\n\n<code>设置汇率7.15</code>");
                    return;
                }

                // 调用服务设置汇率
                var result = await _chatService.SetExchangeRateAsync(device, message.Chat.Id, message.From.Id, rate);
                await SendTempMessageAsync(device, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理设置汇率命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "设置汇率命令处理失败");
            }
        }
    }
}