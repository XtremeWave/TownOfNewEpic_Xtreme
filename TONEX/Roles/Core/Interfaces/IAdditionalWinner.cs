namespace TONEX.Roles.Core.Interfaces;

public interface IAdditionalWinner
{
    public bool CheckWin(ref CustomRoles winnerRole, ref CountTypes winnerCountType);
}
