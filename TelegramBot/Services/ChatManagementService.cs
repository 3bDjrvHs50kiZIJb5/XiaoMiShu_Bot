using System.Diagnostics;
using back.Entities.JZ;
using back.Entities.Setting;
using back.TelegramBot.Utils;
using BootstrapBlazor.Components;
using FreeSql;
using Telegram.Bot.Types.Enums;
using TelegramBotBase;
using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Services
{
    /// <summary>
    /// 群组管理服务 - 处理群组的上课下课等管理功能
    /// </summary>
    public class ChatManagementService : BaseService<ChatManagementService>
    {
        private readonly MemberManagementService _memberService;
        private readonly IBaseRepository<JzChat> _repo_chat;
        private readonly IBaseRepository<Banned> _rep_banned;

        /// <summary>
        /// 构造函数 - 初始化依赖的服务
        /// </summary>
        public ChatManagementService() : base()
        {
            // 使用基类提供的方法创建仓储
            _repo_chat = CreateRepository<JzChat>();
            _rep_banned = CreateRepository<Banned>();

            // 获取其他依赖服务
            _memberService = GetService<MemberManagementService>();
        }

        /// <summary>
        /// 判断用户是否为群管理员
        /// </summary>
        /// <param name="device">设备会话接口</param>
        /// <param name="userId">用户ID</param>
        /// <param name="chatId">群组ID</param>
        /// <returns>如果是管理员返回true，否则返回false</returns>
        public async Task<bool> IsUserAdminAsync(IDeviceSession device, long userId, long chatId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 使用Device.GetChatUser方法获取用户在群组中的信息
                    var chatMember = await device.GetChatUser(userId);

                    // 从数据库中获取操作人员列表
                    var adminList = await _memberService.GetAdminListAsync(chatId);

                    var isAdmin = chatMember.Status == ChatMemberStatus.Administrator ||
                           chatMember.Status == ChatMemberStatus.Creator ||
                           adminList.Any(a => a.FormId == userId);

                    // 记录操作日志
                    LogOperation("检查用户管理员权限", userId, chatId, $"是否管理员: {isAdmin}");

                    return isAdmin;
                },
                "检查用户管理员权限",
                false // 默认返回false
            );
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="device">设备会话接口</param>
        /// <param name="message">消息对象</param>
        /// <returns>用户信息字符串</returns>
        public async Task<string> GetUserInfoAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 构建用户信息提示消息
                    var msg = "🔧 用户信息\n\n";
                    msg += $"• 用户名：{message.From.Username}\n";
                    msg += $"• 姓名：{message.From.FirstName} {message.From.LastName}\n";
                    msg += $"• 用户ID：{message.From.Id}\n\n";

                    // 获取用户在群组中的状态
                    var chatMember = await device.GetChatUser(message.From.Id);
                    msg += $"• 群组状态：{chatMember.Status}\n";

                    // 获取用户是否是管理员
                    var isAdmin = await IsUserAdminAsync(device, message.From.Id, message.Chat.Id);
                    msg += $"• 是否管理员：{(isAdmin ? "是" : "否")}\n";

                    // 记录操作日志
                    LogOperation("获取用户信息", message.From.Id, message.Chat.Id, $"用户名: {message.From.Username}");

                    return msg;
                },
                "获取用户信息",
                "获取用户信息失败，请稍后重试"
            );
        }


        /// <summary>
        /// 设置群组为上课模式 - 允许所有基本交流功能
        /// </summary>
        /// <param name="device">设备会话接口</param>
        /// <param name="chatType">聊天类型</param>
        /// <returns>操作结果消息</returns>
        private async Task<string> SetClassStartModeAsync(IDeviceSession device, ChatType chatType)
        {
            // 检查是否为私聊
            if (chatType == ChatType.Private)
            {
                return "私聊无法使用上课功能";
            }

            try
            {
                // 创建完整的权限对象，允许所有基本交流功能
                await device.SetChatPermissionsAsync(new Telegram.Bot.Types.ChatPermissions()
                {
                    // 基础消息权限
                    CanSendMessages = true, // 发送文本消息、联系人、位置等
                    CanSendAudios = true, // 发送音频文件
                    CanSendDocuments = true, // 发送文档
                    CanSendPhotos = true, // 发送图片
                    CanSendVideos = true, // 发送视频
                    CanSendVideoNotes = true, // 发送视频笔记
                    CanSendVoiceNotes = true, // 发送语音消息
                    CanSendPolls = true, // 发送投票
                    CanSendOtherMessages = true, // 发送动画、游戏、贴纸等
                    CanAddWebPagePreviews = true, // 添加网页预览
                    CanChangeInfo = false, // 不允许修改群组信息
                    CanInviteUsers = false, // 不允许邀请用户
                    CanPinMessages = false, // 不允许置顶消息
                    CanManageTopics = false // 不允许管理话题（论坛群组）
                }, useIndependentChatPermissions: false);

                return "上课了！大家可以自由交流学习内容";
            }
            catch (Exception ex)
            {
                _logger.LogError($"设置上课模式失败: {ex.Message}");
                return "设置上课模式失败，请稍后重试";
            }
        }

        /// <summary>
        /// 设置群组为下课模式 - 禁用所有发送功能
        /// </summary>
        /// <param name="device">设备会话接口</param>
        /// <param name="chatType">聊天类型</param>
        /// <returns>操作结果消息</returns>
        private async Task<string> SetClassEndModeAsync(IDeviceSession device, ChatType chatType)
        {
            // 检查是否为私聊
            if (chatType == ChatType.Private)
            {
                return "私聊无法使用下课功能";
            }

            try
            {
                _logger.LogInformation("下课");
                // 创建完整的权限对象，禁用所有发送功能
                await device.SetChatPermissionsAsync(new Telegram.Bot.Types.ChatPermissions()
                {
                    // 禁用所有消息发送权限
                    CanSendMessages = false, // 禁止发送文本消息
                    CanSendAudios = false, // 禁止发送音频
                    CanSendDocuments = false, // 禁止发送文档
                    CanSendPhotos = false, // 禁止发送图片
                    CanSendVideos = false, // 禁止发送视频
                    CanSendVideoNotes = false, // 禁止发送视频笔记
                    CanSendVoiceNotes = false, // 禁止发送语音
                    CanSendPolls = false, // 禁止发送投票
                    CanSendOtherMessages = false, // 禁止发送其他消息
                    CanAddWebPagePreviews = false, // 禁止添加网页预览
                    CanChangeInfo = false, // 禁止修改群组信息
                    CanInviteUsers = false, // 禁止邀请用户
                    CanPinMessages = false, // 禁止置顶消息
                    CanManageTopics = false // 禁止管理话题
                }, useIndependentChatPermissions: false);

                return "下课了！";
            }
            catch (Exception ex)
            {
                _logger.LogError($"设置下课模式失败: {ex.Message}");
                return "设置下课模式失败，请稍后重试";
            }
        }

        /// <summary>
        /// 处理上课命令 - 设置群组为上课模式
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatId">群组ID</param>
        /// <param name="chatType">聊天类型</param>
        /// <param name="userId">发送命令的用户ID</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> StartClassAsync(IDeviceSession device, long chatId, ChatType chatType, long userId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 调用内部方法设置上课模式
                    var result = await SetClassStartModeAsync(device, chatType);

                    // 记录操作日志
                    LogOperation("开始上课", userId, chatId, "设置群组为上课模式");

                    return result;
                },
                "开始上课",
                "上课操作失败，请稍后重试"
            );
        }

        /// <summary>
        /// 处理下课命令 - 设置群组为下课模式
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatId">群组ID</param>
        /// <param name="chatType">聊天类型</param>
        /// <param name="userId">发送命令的用户ID</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> EndClassAsync(IDeviceSession device, long chatId, ChatType chatType, long userId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 调用内部方法设置下课模式
                    var result = await SetClassEndModeAsync(device, chatType);

                    // 记录操作日志
                    LogOperation("结束上课", userId, chatId, "设置群组为下课模式");

                    return result;
                },
                "结束上课",
                "下课操作失败，请稍后重试"
            );
        }


        /// <summary>
        /// 检查并删除违规内容
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息</param>
        /// <returns>如果检测到违规内容返回true，否则返回false</returns>
        public async Task<bool> CheckAndDeleteViolationAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 检查消息文本是否为空
                    if (string.IsNullOrEmpty(message.Text))
                    {
                        return false;
                    }

                    // 修复空引用问题：确保 message.Text 不为 null 后再进行查询
                    var bannedList = await _rep_banned.Select
                        .Include(a => a.Chat)
                        .Where(a => a.Chat.ChatOriginalId == message.Chat.Id &&
                                   !string.IsNullOrEmpty(message.Text) &&
                                   message.Text.ToUpper().Contains(a.BannedWord.ToUpper().Trim()))
                        .ToListAsync();

                    // 检查是否包含违规关键词
                    if (bannedList.Any())
                    {
                        await device.DeleteMessage(message.MessageId);
                        await DeviceHelper.SendTempMessageAsync(device, "检测到违规内容，已删除");

                        // 记录操作日志
                        LogOperation("删除违规消息", message.From.Id, message.Chat.Id, $"消息ID: {message.MessageId}, 违规关键词数量: {bannedList.Count}");

                        return true;
                    }

                    return false;
                },
                "检查违规内容",
                false // 默认返回false
            );
        }

        /// <summary>
        /// 处理群成员变化事件
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="members">变化的成员列表</param>
        /// <returns>欢迎或告别消息</returns>
        public async Task<string> HandleMemberChangesAsync(IDeviceSession device, MessageType messageType, List<Telegram.Bot.Types.User> members)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    string message = "";

                    if (messageType == MessageType.ChatMembersAdded)
                    {
                        // 构建欢迎新成员的消息
                        var memberNames = members.Select(a => $"{a.FirstName} {a.LastName}").ToList();
                        message = "欢迎新成员加入！\r\n\r\n" + string.Join("\r\n", memberNames);

                        // 记录操作日志
                        LogOperation("新成员加入", additionalInfo: $"成员: {string.Join(", ", memberNames)}");
                    }
                    else if (messageType == MessageType.ChatMemberLeft)
                    {
                        // 构建成员离开的消息
                        var memberNames = members.Select(a => $"{a.FirstName} {a.LastName}").ToList();
                        message = string.Join(" 和 ", memberNames) + " 已离开群组";

                        // 记录操作日志
                        LogOperation("成员离开", additionalInfo: $"成员: {string.Join(", ", memberNames)}");
                    }

                    // 发送消息
                    if (!string.IsNullOrEmpty(message))
                    {
                        await DeviceHelper.SendTempMessageAsync(device, message);
                    }

                    return message;
                },
                "处理成员变化",
                "处理成员变化时出现错误"
            );
        }

        /// <summary>
        /// 设置汇率
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="ChatOriginalId">群组ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="exchangeRate">汇率</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> SetExchangeRateAsync(IDeviceSession device, long ChatOriginalId, long userId, decimal exchangeRate)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 验证汇率参数
                    if (exchangeRate <= 0)
                    {
                        return "汇率必须大于0";
                    }

                    // 设置汇率
                    await _repo_chat.Orm.Update<JzChat>().Set(a => a.ExchangeRate, exchangeRate).Where(a => a.ChatOriginalId == ChatOriginalId).ExecuteAffrowsAsync();


                    // 记录操作日志
                    LogOperation("设置汇率", userId, ChatOriginalId, $"汇率: {exchangeRate}");

                    return $"设置汇率成功，当前汇率：{exchangeRate}";
                },
                "设置汇率",
                "设置汇率失败，请稍后重试"
            );
        }

        /// <summary>
        /// 获取群组汇率
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatOriginalId">群组ID</param>
        /// <returns>返回群组设置的汇率，如果未找到则返回默认值7.3</returns>
        public async Task<decimal> GetExchangeRateAsync(IDeviceSession device, long chatOriginalId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 尝试从数据库获取聊天记录
                    var chat = await _repo_chat.Select
                        .Where(a => a.ChatOriginalId == chatOriginalId)
                        .FirstAsync();

                    if (chat == null)
                    {
                        return 7.3m; // 默认汇率
                    }

                    // 如果汇率为0或未设置，返回默认汇率7.3
                    var exchangeRate = chat.ExchangeRate > 0 ? chat.ExchangeRate : 7.3m;

                    // 记录操作日志
                    LogOperation("获取汇率", chatId: chatOriginalId, additionalInfo: $"汇率: {exchangeRate}");

                    return exchangeRate;
                },
                "获取汇率",
                7.3m // 默认汇率
            );
        }

        /// <summary>
        /// 根据聊天原始ID获取聊天对象
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatOriginalId">群组原始ID</param>
        /// <returns>聊天对象</returns>
        public async Task<JzChat> GetChatAsync(IDeviceSession device, long chatOriginalId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    var chat = await _repo_chat.Select
                        .Where(a => a.ChatOriginalId == chatOriginalId)
                        .FirstAsync();

                    return chat;
                },
                "根据原始ID获取聊天",
                null // 默认返回null
            );
        }

        /// <summary>
        /// 根据聊天数据库ID获取聊天对象
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatId">聊天数据库ID</param>
        /// <returns>聊天对象</returns>
        public async Task<JzChat> GetChatByIdAsync(IDeviceSession device, long chatId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    var chat = await _repo_chat.Select
                        .Where(a => a.Id == chatId)
                        .FirstAsync();

                    return chat;
                },
                "根据数据库ID获取聊天",
                null // 默认返回null
            );
        }

        /// <summary>
        /// 设置费率
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="ChatOriginalId">群组ID</param>
        /// <param name="feeRate">费率</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> SetFeeRateAsync(IDeviceSession device, long ChatOriginalId, decimal feeRate)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 验证汇率参数
                    if (feeRate < 0 || feeRate > 1)
                    {
                        return "费率必须在0到1之间";
                    }

                    // 设置汇率
                    // var chat = await _repo_chat.Select
                    //     .IncludeMany(a => a.Members)
                    //     .Where(a => a.ChatOriginalId == ChatOriginalId)
                    //     .FirstAsync();

                    // if (chat == null)
                    // {
                    //     return "群组不存在，请先初始化群组";
                    // }

                    // chat.FeeRate = feeRate;
                    // await _repo_chat.UpdateAsync(chat);
                    await _repo_chat.Orm.Update<JzChat>().Set(a => a.FeeRate, feeRate).Where(a => a.ChatOriginalId == ChatOriginalId).ExecuteAffrowsAsync();

                    // 记录操作日志
                    LogOperation("设置费率", null, ChatOriginalId, $"费率: {feeRate}");

                    return $"设置费率成功，当前费率：{feeRate}";
                },
                "设置费率",
                "设置费率失败，请稍后重试"
            );
        }
    }
}