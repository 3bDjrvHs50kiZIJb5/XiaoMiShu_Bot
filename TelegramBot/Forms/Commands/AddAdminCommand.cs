using TelegramBotBase.Interfaces;
using Telegram.Bot.Types.Enums;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 添加管理员命令处理类
    /// </summary>
    public class AddAdminCommand : BaseCommand
    {
        /// <summary>
        /// 执行添加管理员命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {

                // 解析命令参数
                var parts = message.Text.Split(' ');
                if (parts.Length < 2)
                {
                    await SendTempMessageAsync(device, $"格式错误，正确格式：\n\n<code>添加操作员 @{message.From.Username}</code>");
                    return;
                }

                var adminId = parts[1];
                // 检查adminId是否为数字，那么就通过这个用户名，查询用户ID
                if (!long.TryParse(adminId, out _))
                {
                    var member = await _memberService.GetMemberByUsernameAsync(device, adminId.TrimStart('@'));
                    adminId = member.FormId.ToString();
                }

                // 调用管理员服务添加管理员
                var result = await _memberService.AddAdminAsync(device, message.Chat.Id, adminId);
                await SendTempMessageAsync(device, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理添加管理员命令失败: {ex.Message}");
                await SendTempMessageAsync(device, $"正确格式：\n\n<code>添加操作员 @{message.From.Username}</code>");
            }
        }
    }
}