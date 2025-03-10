using AmongUs.GameOptions;
using Epic.OnlineServices.Inventory;
using Hazel;
using System.Linq;
using TONEX.Roles.Core;
using TONEX.Roles.Core.Interfaces;
using static TONEX.Translator;

namespace TONEX.Roles.Neutral;

public sealed class SplitPersonality : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
       SimpleRoleInfo.Create(
            typeof(SplitPersonality),
            player => new SplitPersonality(player),
            CustomRoles.SplitPersonality,
         () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            94_1_4_0400,
           null,
            "sp",
            "#960F0F",
           true
#if RELEASE
            ,
            ctop: true
#endif

        );
    public SplitPersonality(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {

    }
    public static bool InCrew = false;
    public static bool InImp = false;
    public static bool InNeu = true;
    public int Think;
    public int Times;
    public override void Add()
    {
        InCrew = false;
     InImp = false;
        InNeu = true;
        Think = IRandom.Instance.Next(10, 60);
        Times = 0;
     }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Times++;
        if(Times == Think) 
        {
         switch(IRandom.Instance.Next(0, 2))
            {
                case 0:
                    InCrew = true;
                    InImp = false;
                    InNeu = false ;
                    break;
                case 1:
                    InCrew = false;
                    InImp = true;
                    InNeu = false;
                    break;
                case 2:
                    InCrew = false;
                    InImp = false;
                    InNeu = true ;
                    break;
            }
            Times = 0;
            Think = IRandom.Instance.Next(10, 60);
            Player.RpcProtectedMurderPlayer();
            Utils.NotifyRoles(Player);
        }
    }

    public bool CheckWin(ref CustomRoles winnerRole, ref CountTypes winnerCountType)
    {
        if(!Player.IsAlive()) return false;
        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && InCrew)
            return true ;
       else if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && InImp)
             return true ;
        else if (InNeu) return true ;
        else return false ;
    }
    public override string GetProgressText(bool comms = false) => GetString(InCrew ? "TeamCrewmate" : InImp ? "TeamImpostor" : "TeamNeutral");
}