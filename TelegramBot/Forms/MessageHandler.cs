using TelegramBotBase.Args;
using TelegramBotBase.Interfaces;
using Telegram.Bot.Types.Enums;
using FreeSql;
using TelegramBotBase.Base;
using back.Entities.JZ;
using back.TelegramBot.Services;
using TelegramBotBase.Form;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;
using System.Data;
using Flurl;
using Flurl.Http;
using System.Text.Json;
using back.TelegramBot.Forms.Commands;
using back.TelegramBot.Utils;

namespace back.TelegramBot.Forms
{
    /// <summary>
    /// 消息处理服务 - 统一处理各种类型的消息
    /// </summary>
    public class MessageHandler
    {
        private readonly MemberManagementService _memberService;
        private readonly ChatManagementService _chatService;
        private readonly OrderManagementService _orderService;
        private readonly MessageManagementService _messageService;
        private readonly CommandConfigService _commandConfigService;
        private readonly ILogger<MessageHandler> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 构造函数 - 初始化依赖的服务
        /// </summary>
        public MessageHandler()
        {
            _memberService = ServiceLocator.ServiceProvider?.GetService<MemberManagementService>();
            _chatService = ServiceLocator.ServiceProvider?.GetService<ChatManagementService>();
            _orderService = ServiceLocator.ServiceProvider?.GetService<OrderManagementService>();
            _messageService = ServiceLocator.ServiceProvider?.GetService<MessageManagementService>();
            _commandConfigService = ServiceLocator.ServiceProvider?.GetService<CommandConfigService>();
            _logger = ServiceLocator.ServiceProvider?.GetService<ILogger<MessageHandler>>();
            _configuration = ServiceLocator.ServiceProvider?.GetService<IConfiguration>();
        }

        /// <summary>
        /// 处理普通消息
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="messageResult">消息结果对象</param>
        /// <returns>处理结果</returns>
        public async Task<bool> HandleMessageAsync(IDeviceSession device, MessageResult messageResult)
        {
            try
            {
                var message = messageResult.Message;
                var messageText = message.Text;

                // 判断是否在群组中
                if (!message.Chat.Id.ToString().StartsWith("-"))
                {
                    await DeviceHelper.SendTempMessageAsync(device, "请在群组中使用");
                    return true;
                }

                // 检查并删除违规内容,直接返回，不继续判断其他命令
                if (await _chatService.CheckAndDeleteViolationAsync(device, message))
                {
                    return true;
                }

                // 处理命令 - 使用配置化的方式
                if (!string.IsNullOrEmpty(messageText))
                {
                    // 使用命令配置服务匹配命令
                    var commandConfig = _commandConfigService?.MatchCommand(messageText);
                    if (commandConfig != null)
                    {
                        // 检查是否需要管理员权限
                        if (commandConfig.RequireAdmin)
                        {
                            if (!await _chatService.IsUserAdminAsync(device, message.From.Id, message.Chat.Id))
                            {
                                await DeviceHelper.SendTempMessageAsync(device, "您的权限不足，请使用<code>/help</code>查看帮助，<code>/id</code>查看用户信息");
                                return true;
                            }
                        }

                        // 使用命令工厂获取并执行对应的命令处理器
                        var result = await ExecuteCommandHandlerAsync(commandConfig.Handler, device, message);
                        if (result)
                        {
                            return true;
                        }
                    }

                    // 处理计算器指令
                    if (messageText.Contains("+") || messageText.Contains("-") || messageText.Contains("*") || messageText.Contains("/"))
                    {
                        // 检查是否是纯数字计算表达式
                        var expression = messageText.Trim();
                        if (Regex.IsMatch(expression, @"^[\d\+\-\*\/\s\.]+$"))
                        {
                            try
                            {
                                // 使用DataTable.Compute方法计算表达式
                                var dt = new DataTable();
                                var result = dt.Compute(expression, "");
                                await DeviceHelper.SendTempMessageAsync(device, $"{expression} = {result:F4}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"计算表达式失败: {ex.Message}");
                                await DeviceHelper.SendTempMessageAsync(device, "计算表达式失败，请检查格式是否正确");
                                return true;
                            }
                        }
                    }
                }

                try
                {
                    if (!message.From.IsBot)
                    {
                        // 记录消息
                        var jzmessage = new JzMessage();

                        // 使用空条件运算符防止空异常，并记录警告
                        var chat = await _chatService.GetChatAsync(device, message.Chat.Id);
                        var member = await _memberService.GetMemberAsync(device, message.From.Id);

                        jzmessage.ChatId = chat?.Id ?? 0;
                        jzmessage.MemberId = member?.Id ?? 0;

                        // 如果关键数据缺失，记录警告但继续处理
                        if (chat == null || member == null)
                        {
                            _logger.LogWarning($"消息记录数据不完整 - Chat: {chat?.Id ?? 0}, Member: {member?.Id ?? 0}");
                        }

                        jzmessage.MessageText = messageText;
                        jzmessage.Type = message.Type;
                        jzmessage.MessageId = message.MessageId;
                        jzmessage.CreatedTime = DateTime.Now;
                        jzmessage.PhotoFile = await _messageService.DownloadPhotoAsync(device, message.Photo?.LastOrDefault()?.FileId);
                        await _messageService.AddMessageAsync(device, jzmessage);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"记录消息失败: {ex.Message}");
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理消息失败: {ex.Message}");
                await DeviceHelper.SendTempMessageAsync(device, "处理消息时出现错误，请稍后重试");
                return false;
            }
        }

        /// <summary>
        /// 执行命令处理方法 - 使用命令工厂模式替代字典查找
        /// </summary>
        /// <param name="handlerName">处理方法名</param>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        /// <returns>处理结果</returns>
        private async Task<bool> ExecuteCommandHandlerAsync(string handlerName, IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                // 使用命令工厂获取对应的命令处理器
                var command = CommandFactory.GetCommand(handlerName);
                if (command != null)
                {
                    // 执行命令处理
                    await command.ExecuteAsync(device, message);
                    return true;
                }
                else
                {
                    // 如果找不到对应的处理方法，记录警告
                    _logger.LogWarning($"未找到命令处理方法: {handlerName}");
                    await DeviceHelper.SendTempMessageAsync(device, "未知命令，请检查输入");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"执行命令处理方法失败 {handlerName}: {ex.Message}");
                await DeviceHelper.SendTempMessageAsync(device, "命令处理失败，请稍后重试");
                return false;
            }
        }
    }
}