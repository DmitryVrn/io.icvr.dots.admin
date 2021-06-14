using System.Collections.Generic;
using ICVR.Dots.Admin.Commands;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ICVR.Dots.Admin.Systems
{

    [DisableAutoCreation]
    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    public class AdminWebServerSystem : SystemBase
    {
        private AdminWebServer _server;
        private AdminWebServer.Config _config;
        private List<IServerCommand> _serverCommands;

        public bool IsRunning => _server != null && _server.IsRunning;
        
        private struct InitServer : IComponentData
        {
        }
        
        public AdminWebServerSystem(AdminWebServer.Config config, params IServerCommand[] serverCommands)
        {
            _serverCommands = new List<IServerCommand>(serverCommands);
            _config = config;
        }
        
        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(InitServer));
            RequireSingletonForUpdate<InitServer>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            Entities
                .ForEach((ref Entity initEntity, ref InitServer initServer) =>
                {
                    commandBuffer.DestroyEntity(initEntity);

                    foreach (var system in World.Systems)
                    {
                        if (system is IServerCommand
                            && system.Enabled)
                        {
                            Debug.Log($"[AdminWebServerSystem] found server command: {system}");
                            _serverCommands.Add(system as IServerCommand);
                        }
                    }
                    
                    // init
                    _server = AdminWebServerBuilder
                        .WithConfig(_config)
                        .WithCommands(_serverCommands.ToArray())
                        .Build();
                    
                    _server.Start();
                })
                .WithoutBurst()
                .Run();
            
            commandBuffer.Playback(EntityManager);
        }

        protected override void OnDestroy()
        {
            if (_server != null) _server.Dispose();
            _server = null;
        }
    }
}