/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TONEX.GameMode
{
    internal class THT
    {
        bool THTBeingWatched()
        {
            foreach (var player in Main.AllAlivePlayerControls)
            {
                var playerPos = player.transform.position;
                var THTPos = Player.transform.position;

                var dir = (THTPos - playerPos).normalized;

                var playerDir = player.transform.forward;
                var dotProduct = Vector3.Dot(dir, playerDir);


                if (dotProduct > 0.995)
                {
                    return true;
                }
            }
        }
    }
}*/
