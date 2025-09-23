using FreeSql;
using back.Entities.JZ;
using back.Entities.Setting;
using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Services
{
    /// <summary>
    /// 订单管理服务 - 处理订单的添加、删除等操作
    /// </summary>
    public class OrderManagementService : BaseService<OrderManagementService>
    {
        private readonly IBaseRepository<JzOrder> _repo_jzorder;
        private readonly ChatManagementService _chatService;

        /// <summary>
        /// 构造函数 - 初始化数据库仓储
        /// </summary>
        public OrderManagementService() : base()
        {
            // 使用基类提供的方法创建仓储
            _repo_jzorder = CreateRepository<JzOrder>();
            // 获取其他依赖服务
            _chatService = GetService<ChatManagementService>();
        }

        /// <summary>
        /// 添加订单
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="order">订单对象</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> AddOrderAsync(IDeviceSession device, JzOrder order)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 验证订单金额
                    if (order.OrderAmountRMB == 0 && order.OrderAmountUSD == 0)
                    {
                        return "";
                    }

                    await _repo_jzorder.InsertAsync(order);

                    // 记录操作日志
                    LogOperation("添加订单", additionalInfo: $"订单类型: {order.OrderType}, 金额: {order.OrderAmountRMB}RMB/{order.OrderAmountUSD}USD");

                    return "订单添加成功";
                },
                "添加订单",
                "订单添加失败，请稍后重试"
            );
        }

        /// <summary>
        /// 获取订单列表
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatId">群组ID</param>
        /// <returns>订单列表</returns>
        public async Task<List<JzOrder>> GetOrderListAsync(IDeviceSession device, long chatId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    return await _repo_jzorder.Select
                        .Where(a => a.ChatId == chatId)
                        .OrderByDescending(a => a.Id)
                        .ToListAsync();
                },
                "获取订单列表",
                new List<JzOrder>() // 默认返回空列表
            );
        }

        /// <summary>
        /// 获取格式化的订单列表字符串 - 用于在Telegram中显示
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatId">群组ID</param>
        /// <returns>格式化的订单列表字符串</returns>
        public async Task<string> GetOrderListStrAsync(IDeviceSession device, long chatId)
        {
            try
            {
                // 获取该群组的所有订单
                var orderList = await GetOrderListAsync(device, chatId);

                if (orderList == null || !orderList.Any())
                {
                    return "暂无订单记录";
                }

                // 按订单类型分组统计，只统计当天的订单
                var today = DateTime.Now.Date;
                var incomingOrders = orderList.Where(o => o.OrderType == OrderType.入款 && o.CreatedTime.Value.Date == today).OrderByDescending(o => o.Id).ToList();
                var outgoingOrders = orderList.Where(o => o.OrderType == OrderType.下发 && o.CreatedTime.Value.Date == today).OrderByDescending(o => o.Id).ToList();

                // 计算统计数据
                var chat = await _chatService.GetChatByIdAsync(device, chatId);


                var incomingCount = incomingOrders.Count;
                var outgoingCount = outgoingOrders.Count;
                var totalIncomingUSD = incomingOrders.Sum(o => o.OrderAmountUSD);
                var totalOutgoingUSD = Math.Abs(outgoingOrders.Sum(o => o.OrderAmountUSD));
                var remainingUSD = totalIncomingUSD - totalOutgoingUSD;

                // 获取当前汇率

                long chatOriginalId = chat?.ChatOriginalId ?? 0;
                var currentExchangeRate = await _chatService.GetExchangeRateAsync(device, chatOriginalId);

                // 构建显示字符串
                var result = new System.Text.StringBuilder();

                // 标题和时间
                result.AppendLine($"<b>{device.ChatTitle}</b> {today:yyyy-MM-dd}");
                result.AppendLine("--------------------------------");
                result.AppendLine($"<b>入款</b>（{incomingCount}笔）：");
                foreach (var order in incomingOrders.Take(3))
                {
                    result.AppendLine($"{order.CreatedTime.Value.ToString("HH:mm")} {order.OrderAmountRMB:F0}/{order.ExchangeRateUSD:F2}={order.OrderAmountRMB / order.ExchangeRateUSD:F2}U");
                }

                result.AppendLine();

                result.AppendLine($"<b>下发</b>（{outgoingCount}笔）：");
                foreach (var order in outgoingOrders.Take(3))
                {
                    result.AppendLine($"{order.CreatedTime.Value.ToString("HH:mm")} {order.OrderAmountRMB:F0}/{order.ExchangeRateUSD:F2}={order.OrderAmountRMB / order.ExchangeRateUSD:F2}U");
                }

                result.AppendLine("");

                // 总入款
                result.AppendLine($"<b>总入款</b>：{totalIncomingUSD * currentExchangeRate:F0} = {totalIncomingUSD:F2}U");
                result.AppendLine($"<b>USD汇率</b>：{currentExchangeRate}");
                result.AppendLine($"<b>费率</b>：{chat.FeeRate * 100}%");
                result.AppendLine("--------------------------------");

                // 应下发、总下发、未下发
                result.AppendLine("");
                result.AppendLine($"应下发：{totalIncomingUSD * currentExchangeRate:F0} = {totalIncomingUSD:F2}U");
                result.AppendLine($"总下发：{totalOutgoingUSD * currentExchangeRate:F0} = {totalOutgoingUSD:F2}U ");
                result.AppendLine($"<b>未下发</b>：{remainingUSD * currentExchangeRate * (1 - chat.FeeRate):F0} = {remainingUSD * (1 - chat.FeeRate):F2}U ");

                // 添加时间戳
                // result.Append($"{DateTime.Now:HH:mm}");

                return result.ToString();
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Console.WriteLine($"获取订单列表字符串失败: {ex.Message}");
                return "获取订单列表失败，请稍后重试";
            }
        }

        public async Task ClearTodayOrderAsync(IDeviceSession device, long chatId)
        {
            try
            {
                // 获取今天的日期
                var today = DateTime.Now.Date;

                // 获取聊天ID
                var chat = await _chatService.GetChatAsync(device, chatId);
                if (chat == null)
                {
                    throw new Exception("未找到对应的聊天记录");
                }

                // 删除今天的订单记录
                await _repo_jzorder.DeleteAsync(a => a.ChatId == chat.Id && a.CreatedTime > today);
            }
            catch (Exception ex)
            {
                // 记录错误日志
                _logger.LogError($"清除今日订单失败: {ex.Message}");
                throw new Exception("清除今日订单失败");
            }
        }
    }
}