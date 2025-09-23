using TelegramBotBase.Interfaces;

namespace back.TelegramBot.Forms.Commands
{
    /// <summary>
    /// 操作人员列表命令处理类
    /// </summary>
    public class AdminListCommand : BaseCommand
    {
        /// <summary>
        /// 执行操作人员列表命令
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        public override async Task ExecuteAsync(IDeviceSession device, Telegram.Bot.Types.Message message)
        {
            try
            {
                // 调用管理员服务获取操作人员列表
                var result = await _memberService.GetAdminListAsync(message.Chat.Id);
                var adminListStr = "操作人员列表：\n" + string.Join("\n", result.Select(a => $"{a.FormId}" + $"(@{a.Username})"));
                await SendTempMessageAsync(device, adminListStr);
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理操作人员列表命令失败: {ex.Message}");
                await SendTempMessageAsync(device, "获取操作人员列表失败");
            }
        }
    }
}