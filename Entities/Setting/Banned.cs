

using back.Entities.JZ;
using FreeSql.DataAnnotations;

namespace back.Entities.Setting;
/// <summary>
/// 违禁词
/// </summary>
[Table(Name = "setting_banned")]
public partial class Banned : EntityCreated
{
    /// <summary>
    /// 违禁词
    /// </summary>
    [Column(StringLength = 50)]
    public string BannedWord { get; set; }
}

public partial class Banned
{

    public long ChatId { get; set; }

    /// <summary>
    /// 群组
    /// </summary>
    [Navigate(nameof(ChatId))]
    public JzChat Chat { get; set; }
}