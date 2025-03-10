//using System;
//using System.Collections.Generic;
//using System.Linq;
//using AmongUs.GameOptions;
//using Il2CppSystem.CodeDom;
//using UnityEngine;
//using static TONEX.Options;
//using Hazel;
//using static TONEX.Translator;
//using TONEX.Modules.SoundInterface;
//using TONEX.Roles.Core;
//using TONEX.Roles.Core.Interfaces;
//using TONEX.Roles.Core.Interfaces.GroupAndRole;
//using InnerNet;
//using TONEX.Modules;
//using TONEX.Roles.Neutral;

//using static TONEX.Modules.HazelExtensions;


//namespace TONEX.Roles.Impostor;
//public sealed class AbyssBringer : RoleBase, IImpostor
//{
//    public static readonly SimpleRoleInfo RoleInfo =
//        SimpleRoleInfo.Create(
//            typeof(AbyssBringer),
//            player => new AbyssBringer(player),
//            CustomRoles.AbyssBringer,
//            () => RoleTypes.Shapeshifter,
//            CustomRoleTypes.Impostor,
//            94_1_4_2500,
//            SetupOptionItem,
//            "prx|借刀杀人",
//             ctop: true
//        );
//    public AbyssBringer(PlayerControl player)
//    : base(
//        RoleInfo,
//        player
//    )
//    {

//    }
//    enum OptionName
//    {
//        UseSKillResetCooldown
//    }
//     static OptionItem BlackHolePlaceCooldown;
//     static OptionItem BlackHoleDespawnMode;
//    static readonly string[] charmedCountMode =
//    {
//        "HoleCountMode.None",
//        "HoleCountMode.AfterTime",
//        "HoleCountMode.After1PlayerEaten",
//        "HoleCountMode.AfterMeeting",
//    };
//    private static void SetupOptionItem()
//    {

//        BlackHoleDespawnMode = StringOptionItem.Create(RoleInfo, 15, GeneralOption.SkillLimit, charmedCountMode, 0, false);
//    }
//    public override void ApplyGameOptions(IGameOptions opt)
//    {
//        AURoleOptions.ShapeshifterCooldown = 10f;
//        AURoleOptions.ShapeshifterDuration = 1f;
//    }
//    public override bool GetAbilityButtonText(out string text)
//    {
//        text = GetString("ProxyButtonText");
//        return true;
//    }
//    private readonly List<BlackHoleData> BlackHoles = [];
//    public override void OnFixedUpdate(PlayerControl pc)
//    {
//        var abyssbringer = Player;
//        int count = BlackHoles.Count;
//        for (int i = 0; i < count; i++)
//        {
//            var blackHole = BlackHoles[i];

//            var despawnMode = BlackHoleDespawnMode.GetInt();
//            switch (despawnMode)
//            {
//                case 1 when Utils.TimeStamp - blackHole.PlaceTimeStamp > 10:
//                case 2 when Utils.TimeStamp - blackHole.PlaceTimeStamp > 10:
//                    RemoveBlackHole();
//                    continue;
//                case 3 when GameStates.IsMeeting:
//                    RemoveBlackHole();
//                    continue;
//            }

//            var nearestPlayer = Main.AllAlivePlayerControls.Where(x => x != pc).MinBy(x => Vector2.Distance(x.GetTruePosition(), blackHole.Position));
//            if (nearestPlayer != null)
//            {
//                var pos = nearestPlayer.GetTruePosition();

//                if (GameStates.IsInTask && !ExileController.Instance)
//                {
//                    var direction = (pos - blackHole.Position).normalized;
//                    var newPosition = blackHole.Position + direction * 1f * Time.fixedDeltaTime;
//                    blackHole.NetObject.TP(newPosition);
//                    blackHole.Position = newPosition;
//                }

//                if (Vector2.Distance(pos, blackHole.Position) <= 3f)
//                {
//                    nearestPlayer.RpcExileV2();
//                    blackHole.PlayersConsumed++;
//                    Utils.SendRPC(CustomRPC.CustomRoleSync, Player, 2, i);
//                    Notify();
                    
//                    var state = PlayerState.GetByPlayerId(nearestPlayer.PlayerId);
//                    state.RealKiller = (DateTime.Now, Player.PlayerId);
//                    state.SetDead();

//                    if (despawnMode == 2)
//                    {
//                        RemoveBlackHole();
//                    }
//                }
//            }

//            continue;

//            void RemoveBlackHole()
//            {
//                BlackHoles.RemoveAt(i);
//                blackHole.NetObject.Despawn();
//                Utils.SendRPC(CustomRPC.CustomRoleSync, Player, 3, i);
//                Notify();
//            }

//            void Notify() => Utils.NotifyRoles(SpecifySeer: abyssbringer);
//        }
//    }
//    public override void ReceiveRPC(MessageReader reader)
//    {
//        switch (reader.ReadPackedInt32())
//        {
//            case 1:
//                var pos = reader.ReadVector2();
//                var roomName = reader.ReadString();
//                BlackHoles.Add(new(new(pos, Player.PlayerId), Utils.TimeStamp, pos, roomName, 0));
//                break;
//            case 2:
//                var blackHole = BlackHoles[reader.ReadPackedInt32()];
//                blackHole.PlayersConsumed++;
//                break;
//            case 3:
//                BlackHoles.RemoveAt(reader.ReadPackedInt32());
//                break;
//        }
//    }



//    enum DespawnMode
//    {
//        None,
//        AfterTime,
//        After1PlayerEaten,
//        AfterMeeting
//    }
//    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
//    {

//        if (!AmongUsClient.Instance.AmHost) return false;
//        animate = false;
//        var pos = Player.GetTruePosition();
//        var room = Player.GetPlainShipRoom();
//        var roomName = room == null ? string.Empty : Translator.GetString($"{room.RoomId}");
//        BlackHoles.Add(new(new(pos, Player.PlayerId), Utils.TimeStamp, pos, roomName, 0));
//        Logger.Info("创建黑洞","Hole");
//        Utils.SendRPC(CustomRPC.CustomRoleSync, Player, 1, pos, roomName);
//        return false;
//    }
//    class BlackHoleData(BlackHole NetObject, long PlaceTimeStamp, Vector2 Position, string RoomName, int PlayersConsumed)
//    {
//        public BlackHole NetObject { get; } = NetObject;
//        public long PlaceTimeStamp { get; } = PlaceTimeStamp;
//        public Vector2 Position { get; set; } = Position;
//        public string RoomName { get; } = RoomName;
//        public int PlayersConsumed { get; set; } = PlayersConsumed;
//    }
//}

