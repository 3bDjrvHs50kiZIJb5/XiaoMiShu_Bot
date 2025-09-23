using back.Entities.Setting;
using FreeSql.DataAnnotations;
using LinCms.Entities.Blog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace back.Entities.JZ;

/// <summary>
/// 聊天管理
/// </summary>
[Table(Name = "jz_chat")]
public partial class JzChat : EntityModified
{
    public long ChatOriginalId { get; set; } 

    /// <summary>
    /// 聊天标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 聊天用户名
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// 聊天简介
    /// </summary>
    public string Bio { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public bool Description { get; set; }

    /// <summary>
    /// 聊天邀请链接
    /// </summary>
    public string InviteLink { get; set; }

    /// <summary>
    /// 汇率
    /// </summary>
    public decimal ExchangeRate { get; set; } = 7.3m;

    /// <summary>
    /// 费率
    /// </summary>
    public decimal FeeRate { get; set; } = 0;

    /// <summary>
    /// 管理员
    /// </summary>
    [Navigate(ManyToMany = typeof(JzChatMember))]
    public List<Member> Members { get; set; } = new List<Member>();


    
}

[Table(Name = "jz_chat_member")]
public class JzChatMember
{

    public long JzChatId { get; set; }
    public long MemberId { get; set; }

    public JzChat JzChat { get; set; }
    public Member Member { get; set; }


}