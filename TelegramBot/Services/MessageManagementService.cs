using FreeSql;
using back.Entities.JZ;
using back.Entities.Setting;
using TelegramBotBase.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration; // 添加配置服务引用


namespace back.TelegramBot.Services
{
    /// <summary>
    /// 消息管理服务 - 处理消息的记录、图片下载等操作
    /// </summary>
    public class MessageManagementService : BaseService<MessageManagementService>
    {
        private readonly IBaseRepository<JzMessage> _repo_jzmessage;

        /// <summary>
        /// 构造函数 - 初始化数据库仓储
        /// </summary>
        public MessageManagementService() : base()
        {
            // 使用基类提供的方法创建仓储
            _repo_jzmessage = CreateRepository<JzMessage>();
        }

        /// <summary>
        /// 添加消息记录
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="message">消息对象</param>
        /// <returns>操作结果消息</returns>
        public async Task<string> AddMessageAsync(IDeviceSession device, JzMessage message)
        {
            return await SafeExecuteWithMessageAsync(
                async () => await _repo_jzmessage.InsertAsync(message),
                "添加消息记录",
                "消息记录成功",
                "消息记录失败，请稍后重试"
            );
        }

        /// <summary>
        /// 下载图片的具体实现
        /// </summary>
        /// <param name="device">Telegram设备会话</param>
        /// <param name="fileId">文件ID</param>
        /// <returns>下载结果</returns>
        public async Task<string> DownloadPhotoAsync(IDeviceSession device, string fileId)
        {
            return await SafeExecuteAsync(
                async () =>
                {
                    // 验证必需参数
                    if (!ValidateRequired(fileId, nameof(fileId)))
                    {
                        return "文件ID不能为空";
                    }

                    Telegram.Bot.Types.File fileInfo = null;

                    // 通过回调函数获取文件信息
                    await device.Api(async api =>
                    {
                        fileInfo = await api.GetFileAsync(fileId);
                    });

                    if (fileInfo == null)
                    {
                        return "无法获取文件信息";
                    }

                    // 创建下载目录
                    var downloadDir = Path.Combine(AppContext.BaseDirectory, "wwwroot","uploads");
                    Directory.CreateDirectory(downloadDir);

                    // 生成文件名
                    var fileName = $"{fileId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                    var filePath = Path.Combine(downloadDir, fileName);

                    // 通过回调函数下载文件
                    await device.Api(async api =>
                    {
                        using var fileStream = new FileStream(filePath, FileMode.Create);
                        await api.DownloadFileAsync(fileInfo.FilePath, fileStream);
                    });

                    // 记录操作日志
                    LogOperation("下载图片", additionalInfo: $"文件ID: {fileId}, 文件名: {fileName}");

                    return $"{_configuration["Domain"]}/uploads/{fileName}";
                },
                "下载图片",
                "下载失败，请稍后重试"
            );
        }


    }
}