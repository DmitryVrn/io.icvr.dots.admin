using ICVR.Dots.Admin;
using ICVR.Dots.Admin.Systems;
using NUnit.Framework;
using Unity.Entities;

namespace ICVR.Tests
{
    [TestFixture]
    public class AdminWebServerSystemTests : EcsTestFixture
    {
        private struct FooComponent : IComponentData
        {
            public int Value;
        }

        private class FooSystem : ComponentSystem
        {
            protected override void OnUpdate()
            {
                Entities.ForEach((ref FooComponent foo) =>
                {
                    foo.Value += 1;
                });
            }
        }
        
        [Test]
        public void CreateSystemAndEntityTest()
        {
            // arrange
            var entity = DefaultManager.CreateEntity(typeof(FooComponent));
            DefaultManager.SetComponentData(entity, new FooComponent { Value = 73313});
            
            // act
            DefaultWorld.CreateSystem<FooSystem>().Update();
            
            // assert
            Assert.AreEqual(73314, DefaultManager.GetComponentData<FooComponent>(entity).Value);
        }

        [Test]
        public void StartStopAdminWebServerTest()
        {
            var serverConfig = AdminWebServer.DefaultConfig;
            
            var serverSystem = new AdminWebServerSystem(serverConfig);
            DefaultWorld.AddSystem(serverSystem);
            
            // update to start the server
            serverSystem.Update();

            Assert.IsTrue(serverSystem.IsRunning);
            
            DefaultWorld.DestroySystem(serverSystem);

            Assert.IsFalse(serverSystem.IsRunning);
        }
    }
}