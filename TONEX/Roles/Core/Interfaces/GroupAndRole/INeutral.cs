using TONEX.Roles.Neutral;

namespace TONEX.Roles.Core.Interfaces.GroupAndRole;

/// <summary>
/// 中立阵营的接口
/// </summary>
public interface INeutral
{
    
    /// <summary>
    /// 是独立阵营
    /// </summary>
    public bool IsNE => true;
}

/// <summary>
/// 带有击杀按钮的中立职业的接口
/// </summary>
public interface INeutralKiller : INeutral, IKiller, ISchrodingerCatOwner
{
    SchrodingerCat.TeamType ISchrodingerCatOwner.SchrodingerCatChangeTo => SchrodingerCat.TeamType.None;

    /// <summary>
    /// 是否中立杀手
    /// 默认：返回 true
    /// </summary>
    /// <returns>FALSE：否</returns>
    public bool IsNK => false;

}
