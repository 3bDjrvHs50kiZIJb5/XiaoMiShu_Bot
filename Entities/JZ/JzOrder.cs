using back.Entities.Setting;
using FreeSql.DataAnnotations;

namespace back.Entities.JZ;

/// <summary>
/// 订单类型
/// </summary>
public enum OrderType
{
    入款,
    下发
}

/// <summary>
/// 订单管理
/// </summary>
[Table(Name = "jz_order")]
public partial class JzOrder : EntityCreated
{
    /// <summary>
    /// 订单编号
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 订单名称
    /// </summary>
    public string OrderName { get; set; }

    /// <summary>
    /// 订单类型
    /// </summary>
    public OrderType OrderType { get; set; }

    /// <summary>
    /// USD汇率
    /// </summary>
    public decimal ExchangeRateUSD { get; set; }

    /// <summary>
    /// 订单金额USD
    /// </summary>
    public decimal OrderAmountUSD { get; set; }

    /// <summary>
    /// 订单金额RMB
    /// </summary>
    public decimal OrderAmountRMB { get; set; }

}

partial class JzOrder
{
    /// <summary>
    /// 发起人ID
    /// </summary>
    public long MemberId { get; set; }
    /// <summary>
    /// 发起人
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

