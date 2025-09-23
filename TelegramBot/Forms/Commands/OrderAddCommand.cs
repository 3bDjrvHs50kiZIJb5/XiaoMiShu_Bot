using TelegramBotBase.Interfaces;
using back.Entities.JZ;
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 入款订单命令处理类 - 专门处理以"+"开头的入款订单
    /// </summary>
    public class OrderAddCommand : BaseCommand
    {
        /// <summary>
        /// 执行入款订单命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                var messageText = message.Text;

                // 从配置文件中获取入款订单的触发词
                var orderAddConfig = _commandConfigService?.GetAllCommands()
                    .FirstOrDefault(c => c.Key == "orderAdd").Value;

                // 如果配置不存在，使用默认触发词作为备用
                var triggers = orderAddConfig?.Triggers ?? new List<string> { "+", "入款", "上压" };

                // 检查消息是否以入款订单的触发词开头
                foreach (var trigger in triggers)
                {
                    if (messageText.StartsWith(trigger))
                    {
                        messageText = messageText.Replace(trigger, "+");
                    }
                }

                // 判断输入的格式是否正确，否则就提示错误,返回
                // +1000.1 或 +100.1u 或 +100 或 +100u
                if (!Regex.IsMatch(messageText.ToLower(), @"^\+\d+(\.\d+)?u?$"))
                {
                    // await SendTempMessageAsync(device, "格式错误，请输入正确的数字格式\n\n示例：<code>-1000</code> 或 <code>-100u</code>");
                    return;
                }

                // 获取当前群组的汇率
                var exchangeRate = await _chatService.GetExchangeRateAsync(device, message.Chat.Id);

                // 创建新的订单对象
                var order = new JzOrder();
                order.ExchangeRateUSD = exchangeRate;
                order.ChatId = (await _chatService.GetChatAsync(device, message.Chat.Id))?.Id ?? 0;
                order.MemberId = (await _memberService.GetMemberAsync(device, message.From.Id))?.Id ?? 0;

                // 设置订单类型为入款
                order.OrderType = OrderType.入款;

                // 解析金额 - 判断输入的信息中是否包含u，如果是u那么以USD为单位，否则以CNY为单位
                if (messageText.ToLower().EndsWith("u"))
                {
                    // 去掉开头的"+"和末尾的"u"字符，获取数字部分
                    var amountStr = messageText.Substring(1, messageText.Length - 2);
                    order.OrderAmountUSD = decimal.Parse(amountStr);
                    order.OrderAmountRMB = order.OrderAmountUSD * order.ExchangeRateUSD;
                }
                else
                {
                    // 去掉开头的"+"字符，获取数字部分
                    var amountStr = messageText.Substring(1);
                    order.OrderAmountRMB = decimal.Parse(amountStr);
                    order.OrderAmountUSD = order.OrderAmountRMB / order.ExchangeRateUSD;
                }

                // 设置创建时间
                order.CreatedTime = DateTime.Now;

                // 调用订单服务处理订单添加
                var result = await _orderService.AddOrderAsync(device, order);

                // 获取并发送订单列表
                var orderliststr = await _orderService.GetOrderListStrAsync(device, order.ChatId);
                await SendTempMessageAsync(device, orderliststr);
            }
            catch (FormatException ex)
            {
                _logger.LogError($"入款订单金额格式错误: {ex.Message}");
                await SendTempMessageAsync(device, "金额格式错误，请输入正确的数字格式\n\n示例：<code>+1000</code> 或 <code>+100u</code>");
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理入款订单命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "入款订单处理失败，请稍后重试");
            }
        }
    }
}