using System;
using System.Collections.Generic;
using ICVR.Dots.Admin.Commands;
using ICVR.Dots.Admin.Messages;
using Unity.Entities;
using Unity.NetCode;

namespace ICVR.Dots.Admin.Systems
{
    [Serializable]
    public struct ListPlayersMessage : IServerMessage
    {
        public PlayerInfoMessage[] players;
    }

    [Serializable]
    public struct PlayerInfoMessage : IServerMessage
    {
        public int id;
        public string name;
    }
    
    public class ListPlayersCommandSystem : AdminServerCommandSystemBase
    {
        public static readonly string CommandId = "listPlayers";
        public override string Id => CommandId;

        public override IServerMessage Process(string data)
        {
            var players = new List<PlayerInfoMessage>();
            
            Entities
                .ForEach((ref Entity entity, ref NetworkIdComponent playerConnection) =>
                {
                    players.Add(new PlayerInfoMessage
                    {
                        id = playerConnection.Value,
                        name = "Player #" + playerConnection.Value
                    });
                })
                .WithoutBurst()
                .Run();
            
            return new ListPlayersMessage {players = players.ToArray()};
        }
    }
}