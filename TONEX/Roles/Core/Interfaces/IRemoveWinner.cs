using System.Collections.Generic;

namespace TONEX.Roles.Core.Interfaces;

public interface IRemoveWinner
{
    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds);
}

