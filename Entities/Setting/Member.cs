using back.Entities.JZ;
using FreeSql.DataAnnotations;
using LinCms.Entities.Blog;

namespace back.Entities.Setting;

/// <summary>
/// 系统用户管理
/// </summary>
[Table(Name = "SysUser")]
public partial class Member : SysUser
{
    /// <summary>
    /// TG的会员ID
    /// </summary>
    public long FormId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// 语言代码
    /// </summary>
    public string LanguageCode { get; set; }

    /// <summary>
    /// 是否为机器人
    /// </summary>
    public bool IsBot { get; set; }

    /// <summary>
    /// 是否为付费用户
    /// </summary>
    public bool IsPremium { get; set; }


    /// <summary>
    /// 群组
    /// </summary>
    [Navigate(ManyToMany = typeof(JzChatMember))]
    public List<JzChat> JzChats { get; set; }
    

}
