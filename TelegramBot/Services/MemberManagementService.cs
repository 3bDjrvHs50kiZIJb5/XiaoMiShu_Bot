using FreeSql;
using back.Entities.JZ;
using back.Entities.Setting;
using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Services
{
    /// <summary>
    /// 成员管理服务 - 处理群组成员和管理员的添加、删除等操作
    /// </summary>
    public class MemberManagementService : BaseService<MemberManagementService>
    {
        private readonly IBaseRepository<JzChat> _repo_jzchat;
        private readonly IBaseRepository<Member> _repo_member;

        /// <summary>
        /// 构造函数 - 初始化数据库仓储
        /// </summary>
        public MemberManagementService() : base()
        {
            // 使用基类提供的方法创建仓储
            _repo_jzchat = CreateRepository<JzChat>();
            _repo_member = CreateRepository<Member>();
        }

        /// <summary>
        /// 添加群组管理员
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatId">群组ID</param>
        /// <param name="adminIdText">要添加的管理员ID（文本格式）</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> AddAdminAsync(IDeviceSession device, long chatId, string adminIdText)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 验证管理员ID格式是否正确
                    if (!ValidateUserId(adminIdText, out long adminId))
                    {
                        return "管理员ID格式不正确，请输入数字ID";
                    }

                    // 查询群组信息，包含现有操作人员列表
                    var chat = await _repo_jzchat.Select
                        .Where(a => a.ChatOriginalId == chatId)
                        .IncludeMany(a => a.Members)
                        .FirstAsync();

                    // 检查群组是否存在
                    if (chat == null)
                    {
                        return "群组不存在，请先初始化群组";
                    }

                    // 查询要添加的成员信息
                    var member = await _repo_member.Select
                        .Where(a => a.FormId == adminId)
                        .FirstAsync();

                    // 检查成员是否存在
                    if (member == null)
                    {
                        return "群成员ID不存在，请让该用户先在群里发言以注册信息";
                    }

                    // 检查该用户是否已经是操作人员
                    if (chat.Members.Any(a => a.FormId == adminId))
                    {
                        return "该用户已经是管理员，无需重复添加";
                    }

                    // 将新操作人员添加到群组操作人员列表中
                    chat.Members = new List<Member>(chat.Members) { member };

                    // 更新数据库中的群组信息
                    await _repo_jzchat.UpdateAsync(chat);

                    // 记录操作日志
                    LogOperation("添加操作人员", adminId, chatId, $"操作人员: {member.FirstName} {member.LastName}");

                    return $"操作人员添加成功！用户 {member.FirstName} {member.LastName} 现在是群组操作人员";
                },
                "添加操作人员",
                "添加操作人员失败，请稍后重试"
            );
        }

        /// <summary>
        /// 移除群组管理员
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="chatId">群组ID</param>
        /// <param name="adminIdText">要移除的管理员ID（文本格式）</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> RemoveAdminAsync(IDeviceSession device, long chatId, string adminIdText)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 验证管理员ID格式是否正确
                    if (!ValidateUserId(adminIdText, out long adminId))
                    {
                        return "管理员ID格式不正确，请输入数字ID";
                    }

                    // 查询群组信息，包含现有操作人员列表
                    var chat = await _repo_jzchat.Select
                        .Where(a => a.ChatOriginalId == chatId)
                        .IncludeMany(a => a.Members)
                        .FirstAsync();

                    // 检查群组是否存在
                    if (chat == null)
                    {
                        return "群组不存在";
                    }

                    // 查找要移除的操作人员
                    var adminToRemove = chat.Members.FirstOrDefault(a => a.FormId == adminId);
                    if (adminToRemove == null)
                    {
                        return "该用户不是群组管理员";
                    }

                    // 从操作人员列表中移除
                    chat.Members = chat.Members.Where(a => a.FormId != adminId).ToList();

                    // 更新数据库中的群组信息
                    await _repo_jzchat.UpdateAsync(chat);

                    // 记录操作日志
                    LogOperation("移除操作人员", adminId, chatId, $"操作人员: {adminToRemove.FirstName} {adminToRemove.LastName}");

                    return $"操作人员移除成功！用户 {adminToRemove.FirstName} {adminToRemove.LastName} 不再是群组操作人员";
                },
                "移除操作人员",
                "移除操作人员失败，请稍后重试"
            );
        }


        /// <summary>
        /// 获取群组操作人员列表
        /// </summary>
        /// <param name="ChatOriginalId">群组ID</param>
        /// <returns>操作人员列表信息</returns>
        public async Task<List<Member>> GetAdminListAsync(long ChatOriginalId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 查询群组信息，包含操作人员列表
                    var chat = await _repo_jzchat.Select
                        .Where(a => a.ChatOriginalId == ChatOriginalId)
                        .IncludeMany(a => a.Members)
                        .FirstAsync();

                    if (chat == null)
                    {
                        return new List<Member>();
                    }

                    if (!chat.Members.Any())
                    {
                        return new List<Member>();
                    }

                    return chat.Members;
                },
                "获取操作人员列表",
                new List<Member>()
            );
        }

        /// <summary>
        /// 根据用户ID获取成员对象
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="userId">用户ID</param>
        /// <returns>成员对象</returns>
        public async Task<Member> GetMemberAsync(IDeviceSession device, long userId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    var member = await _repo_member.Select
                        .Where(a => a.FormId == userId)
                        .FirstAsync();

                    return member;
                },
                "获取成员信息",
                null // 默认返回null
            );
        }

        /// <summary>
        /// 根据用户名获取成员对象
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="username">用户名</param>
        /// <returns>成员对象</returns>
        public async Task<Member> GetMemberByUsernameAsync(IDeviceSession device, string username)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 验证必需参数
                    if (!ValidateRequired(username, nameof(username)))
                    {
                        return null;
                    }

                    var member = await _repo_member.Select
                        .Where(a => a.Username == username)
                        .FirstAsync();

                    return member;
                },
                "根据用户名获取成员",
                null // 默认返回null
            );
        }
    }
}