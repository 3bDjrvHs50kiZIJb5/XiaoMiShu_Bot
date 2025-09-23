using FreeSql.DataAnnotations;
using back.Entities.JZ;
using back.Entities.Setting;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace back.Entities.JZ;

/// <summary>
/// 消息管理
/// </summary>
[Table(Name = "jz_message")]
public partial class JzMessage : EntityCreated
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public long MessageId { get; set; }

    /// <summary>
    /// 消息文本
    /// </summary>
    [Column(StringLength = -1)]
    public string MessageText { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// 消息图片
    /// </summary>
    [Column(StringLength = -1)]
    public string PhotoFile { get; set; }
}

public partial class JzMessage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public long MemberId { get; set; }

    /// <summary>
    /// 会员
    /// </summary>
    [Navigate(nameof(MemberId))]
    public Member Member { get; set; }

    /// <summary>
    /// 所属聊天ID
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    /// 所属聊天
    /// </summary>
    [Navigate(nameof(ChatId))]
    public JzChat Chat { get; set; }


}