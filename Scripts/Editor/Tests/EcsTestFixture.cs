using NUnit.Framework;
using Unity.Entities;

namespace ICVR.Tests
{
    [SetUpFixture]
    public class EcsTestFixture
    {
        public static World DefaultWorld;
        public static EntityManager DefaultManager => DefaultWorld.EntityManager;

        private static readonly string DefaultWorldName = "TestWorld";
            
        public static void SetUpDefaultWorld()
        {
            if (!(DefaultWorld is null)) return;

            foreach (var world in World.All)
            {
                if (world.Name != DefaultWorldName) continue;
                DefaultWorld = world;
                break;
            }

            if (DefaultWorld is null)
            {
                DefaultWorld = new World(DefaultWorldName, WorldFlags.Editor);
            }

            World.DefaultGameObjectInjectionWorld = DefaultWorld;
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            SetUpDefaultWorld();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            for (var i = World.All.Count - 1; i >= 0; i--)
                if ((World.All[i].Name.StartsWith("Test") || World.All[i].Name == DefaultWorldName) &&
                    World.All[i] is World world && world.IsCreated)
                    world.Dispose();

            DefaultWorld = null;
        }
    }
}