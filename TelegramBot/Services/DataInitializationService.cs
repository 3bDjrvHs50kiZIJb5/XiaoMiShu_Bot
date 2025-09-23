using FreeSql;
using back.Entities.JZ;
using back.Entities.Setting;
using TelegramBotBase.Interfaces;
using Telegram.Bot.Types.Enums;
using back.TelegramBot.Utils;



namespace back.TelegramBot.Services
{
    /// <summary>
    /// 数据初始化服务 - 处理群组和用户的初始化工作
    /// </summary>
    public class DataInitializationService : BaseService<DataInitializationService>
    {
        private readonly IBaseRepository<JzChat> _repo_jzchat;
        private readonly IBaseRepository<Member> _repo_member;

        /// <summary>
        /// 构造函数 - 初始化数据库仓储
        /// </summary>
        public DataInitializationService() : base()
        {
            // 使用基类提供的方法创建仓储
            _repo_jzchat = CreateRepository<JzChat>();
            _repo_member = CreateRepository<Member>();
        }

        /// <summary>
        /// 初始化群组信息 - 如果群组不存在则创建
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        /// <returns>初始化后的群组对象</returns>
        public async Task<JzChat> InitializeGroupAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 查询群组是否已存在
                    var existingChat = await _repo_jzchat.Select
                        .IncludeMany(a => a.Members)
                        .Where(a => a.ChatOriginalId == message.Chat.Id)
                        .FirstAsync();

                    // 如果群组已存在，直接返回
                    if (existingChat != null)
                    {
                        return existingChat;
                    }

                    // 创建新的群组记录
                    var newChat = new JzChat
                    {
                        ChatOriginalId = message.Chat.Id,
                        Title = message.Chat.Title,
                        CreatedTime = DateTime.Now,
                        Members = new List<Member>() // 初始化空的管理员列表
                    };

                    // 保存到数据库
                    await _repo_jzchat.InsertAsync(newChat);

                    // 记录操作日志
                    LogOperation("初始化群组", chatId: message.Chat.Id, additionalInfo: $"群组名称: {newChat.Title}");

                    return newChat;
                },
                "初始化群组",
                null // 如果失败返回null，调用方需要处理
            );
        }

        /// <summary>
        /// 初始化用户信息 - 如果用户不存在则创建
        /// </summary>
        /// <param name="user">Telegram用户对象</param>
        /// <returns>初始化后的用户对象</returns>
        public async Task<Member> InitializeMemberAsync(Telegram.Bot.Types.User user)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 查询用户是否已存在
                    var existingMember = await _repo_member.Select
                        .Where(a => a.FormId == user.Id)
                        .FirstAsync();

                    // 如果用户已存在，更新用户信息并返回
                    if (existingMember != null)
                    {
                        // 更新用户信息（用户可能修改了用户名等信息）
                        existingMember.Username = user.Username;
                        existingMember.FirstName = user.FirstName;
                        existingMember.LastName = user.LastName;
                        existingMember.LanguageCode = user.LanguageCode;
                        existingMember.IsBot = user.IsBot;

                        await _repo_member.UpdateAsync(existingMember);

                        // 记录操作日志
                        LogOperation("更新用户信息", user.Id, additionalInfo: $"用户名: {user.Username}");

                        return existingMember;
                    }

                    // 创建新的用户记录
                    var newMember = new Member
                    {
                        FormId = user.Id,
                        Username = user.Username,
                        Password = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        LanguageCode = user.LanguageCode,
                        IsBot = user.IsBot,
                        CreatedTime = DateTime.Now
                        // TODO: 需要根据实际项目配置角色系统
                        // Roles = await _fsql.Select<SysRole>().Where(a => a.Name == "普通用户").ToListAsync()
                    };

                    // 保存到数据库
                    await _repo_member.InsertAsync(newMember);

                    // 记录操作日志
                    LogOperation("初始化新用户", user.Id, additionalInfo: $"用户名: {user.Username}, 姓名: {user.FirstName} {user.LastName}");

                    return newMember;
                },
                "初始化用户",
                null // 如果失败返回null，调用方需要处理
            );
        }

        /// <summary>
        /// 生成管理员设置提示消息
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chat">群组对象</param>
        /// <param name="user">当前用户对象</param>
        /// <returns>提示消息内容</returns>
        public string GenerateAdminSetupMessage(IDeviceSession device, JzChat chat, Telegram.Bot.Types.User user)
        {
            try
            {
                // todo  这个地方需要读取数据库？？？

                // 如果群组已有管理员，不显示设置提示
                if (chat.Members.Any())
                {
                    return "";
                }

                // 构建管理员设置提示消息
                var message = "该群组暂未设置管理员，请按以下步骤设置：\n\n";
                message += $"<code>添加操作员 @{user.Username}</code>";

                // 记录操作日志
                LogOperation("生成管理员设置提示", user.Id, chat.ChatOriginalId);

                return message;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                _logger.LogError(ex, "生成管理员设置消息失败");
                return "生成管理员设置提示失败";
            }
        }

        /// <summary>
        /// 完整的初始化流程 - 初始化群组和用户，并发送必要的提示
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        /// <returns>初始化是否成功</returns>
        public async Task<bool> PerformFullInitializationAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 初始化群组
                    var chat = await InitializeGroupAsync(device, message);
                    if (chat == null)
                    {
                        await DeviceHelper.SendTempMessageAsync(device, "群组初始化失败，请稍后重试");
                        return false;
                    }

                    // 初始化用户
                    var member = await InitializeMemberAsync(message.From);
                    if (member == null)
                    {
                        await DeviceHelper.SendTempMessageAsync(device, "用户初始化失败，请稍后重试");
                        return false;
                    }

                    // 如果群组没有管理员，发送设置提示
                    var setupMessage = GenerateAdminSetupMessage(device, chat, message.From);
                    if (!string.IsNullOrEmpty(setupMessage))
                    {
                        await DeviceHelper.SendTempMessageAsync(device, setupMessage);
                    }
                    //更新配置文件中的Domain 到数据表 SysTenant中
                    // 从配置文件中获取Domain，并提取域名部分（如 jz.bot123.cc）
                    var domainConfig = _configuration["Domain"];
                    string domain = "";
                    if (!string.IsNullOrEmpty(domainConfig))
                    {
                        try { domain = new Uri(domainConfig).Host; } catch { }
                    }
                    
                    _fsql.Update<BootstrapBlazor.Components.SysTenant>().Set(a => a.Host, domain).ExecuteAffrows();
                    LogOperation("更新域名",message.From.Id, message.Chat.Id, domain);

                    // 记录操作日志
                    LogOperation("完整初始化", message.From.Id, message.Chat.Id, "初始化成功");

                    return true;
                },
                "完整初始化流程",
                false // 默认返回失败
            );
        }
    }
}