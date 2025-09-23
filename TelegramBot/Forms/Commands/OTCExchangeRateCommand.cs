using TelegramBotBase.Interfaces;
using Flurl;
using Flurl.Http;
using System.Text.Json;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// OTC汇率查询命令处理类 - 从OKX获取USDT/CNY汇率
    /// </summary>
    public class OTCExchangeRateCommand : BaseCommand
    {
        /// <summary>
        /// 执行OTC汇率查询命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                // 通过HTTP请求获取OKX的C2C交易数据
                var response = await "https://www.okx.com/v3/c2c/tradingOrders/books"
                    .SetQueryParams(new
                    {
                        quoteCurrency = "CNY",
                        baseCurrency = "USDT",
                        side = "sell",
                        paymentMethod = "aliPay",
                        userType = "all",
                        showTrade = "false",
                        showFollow = "false",
                        showAlreadyTraded = "false",
                        isAbleFilter = "false",
                        receivingAds = "false"
                    })
                    .WithHeaders(new
                    {
                        accept = "application/json",
                        accept_language = "zh-CN,zh;q=0.9",
                        app_type = "web",
                        cache_control = "no-cache",
                        pragma = "no-cache",
                        priority = "u=1, i",
                        referer = "https://www.okx.com/zh-hans/p2p-markets/cny/buy-usdt",
                        sec_ch_ua = "\"Google Chrome\";v=\"137\", \"Chromium\";v=\"137\", \"Not/A)Brand\";v=\"24\"",
                        sec_ch_ua_mobile = "?0",
                        sec_ch_ua_platform = "\"macOS\"",
                        sec_fetch_dest = "empty",
                        sec_fetch_mode = "cors",
                        sec_fetch_site = "same-origin",
                        user_agent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36",
                        x_cdn = "https://www.okx.com",
                        x_locale = "zh_CN",
                        x_utc = "8"
                    })
                    .GetStringAsync();

                // 解析JSON响应
                using var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                // 检查响应状态
                if (!root.TryGetProperty("code", out var codeElement) || codeElement.GetInt32() != 0)
                {
                    await SendTempMessageAsync(device, "获取OTC汇率失败：API返回错误");
                    return;
                }

                // 获取交易订单数据
                if (!root.TryGetProperty("data", out var dataElement) ||
                    !dataElement.TryGetProperty("sell", out var sellElement) ||
                    sellElement.GetArrayLength() == 0)
                {
                    await SendTempMessageAsync(device, "获取OTC汇率失败：暂无交易数据");
                    return;
                }

                // 获取前10个卖单的价格信息（显示更多商家）
                var sellOrders = sellElement.EnumerateArray().Take(10).ToList();

                if (!sellOrders.Any())
                {
                    await SendTempMessageAsync(device, "获取OTC汇率失败：暂无卖单数据");
                    return;
                }

                // 构建返回消息 - 简洁格式
                var result = new System.Text.StringBuilder();
                result.AppendLine("<b>OTC商家实时价格</b>");
                result.AppendLine("筛选:支付宝欧易");
                result.AppendLine("--------------------------------");

                // 显示价格和商家名称
                foreach (var order in sellOrders)
                {
                    // 基础信息验证
                    if (!order.TryGetProperty("price", out var priceElement) ||
                        !order.TryGetProperty("nickName", out var nickNameElement))
                    {
                        continue; // 跳过数据不完整的订单
                    }

                    // 解析价格
                    if (!decimal.TryParse(priceElement.GetString(), out var price))
                    {
                        continue; // 跳过价格格式错误的订单
                    }

                    var nickName = nickNameElement.GetString() ?? "未知商家";

                    // 简洁显示：价格 + 商家名称
                    result.AppendLine($"{price:F2}    {nickName}");
                }
                result.AppendLine("--------------------------------");

                // 发送结果消息
                await SendTempMessageAsync(device, result.ToString());
            }
            catch (JsonException ex)
            {
                _logger.LogError($"解析OTC汇率JSON失败: {ex.Message}");
                await SendTempMessageAsync(device, "解析OTC汇率数据失败，JSON格式错误");
            }
            catch (FlurlHttpException ex)
            {
                _logger.LogError($"HTTP请求OTC汇率失败: {ex.Message} - Status: {ex.Call?.Response?.StatusCode}");
                await SendTempMessageAsync(device, $"网络请求失败，请检查网络连接后重试\n错误代码: {ex.Call?.Response?.StatusCode}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError($"OTC汇率请求超时: {ex.Message}");
                await SendTempMessageAsync(device, "请求超时，请稍后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取OTC汇率失败: {ex.Message}");
                await SendTempMessageAsync(device, "获取OTC汇率失败，请稍后重试");
            }
        }
    }
}