using TelegramBotBase.Interfaces;
using Telegram.Bot.Types.Enums;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 设置费率命令处理类
    /// </summary>
    public class SetFeeRateCommand : BaseCommand
    {
        /// <summary>
        /// 执行设置费率命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                // 检查消息格式：应该是 "设置费率1%" 这样的格式
                var text = message.Text.Trim();

                // 检查是否以"设置费率"开头
                if (!text.StartsWith("设置费率"))
                {
                    await SendTempMessageAsync(device, "格式错误，正确格式：\n\n<code>设置费率1%</code>");
                    return;
                }

                // 提取费率部分（去掉"设置费率"前缀）
                var feeRateText = text.Substring(4); // "设置费率"是4个字符

                // 检查费率部分是否为空
                if (string.IsNullOrWhiteSpace(feeRateText))
                {
                    await SendTempMessageAsync(device, "格式错误，正确格式：\n\n<code>设置费率1%</code>");
                    return;
                }

                decimal feeRate = 0;

                // 解析费率值
                if (feeRateText.EndsWith("%"))
                {
                    // 如果以%结尾，去掉%符号并转换为小数
                    var percentText = feeRateText.Substring(0, feeRateText.Length - 1);
                    if (decimal.TryParse(percentText, out var percentValue))
                    {
                        feeRate = percentValue / 100; // 转换为小数形式
                    }
                    else
                    {
                        await SendTempMessageAsync(device, "费率格式错误，请输入有效的数字，正确格式：\n\n<code>设置费率1%</code>");
                        return;
                    }
                }
                else
                {
                    // 如果不以%结尾，直接解析为小数
                    if (decimal.TryParse(feeRateText, out var decimalValue))
                    {
                        feeRate = decimalValue;
                    }
                    else
                    {
                        await SendTempMessageAsync(device, "费率格式错误，请输入有效的数字，正确格式：\n\n<code>设置费率1%</code>");
                        return;
                    }
                }

                // 调用服务设置费率
                var result = await _chatService.SetFeeRateAsync(device, message.Chat.Id, feeRate);
                await SendTempMessageAsync(device, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理设置费率命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "设置费率命令处理失败");
            }
        }
    }
}