using System.Collections;
using ICVR.Dots.Admin;
using ICVR.Dots.Admin.Systems;
using NUnit.Framework;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace ICVR.Tests
{
    [TestFixture]
    public class AdminWebServerCommandsTest : EcsTestFixture
    {
        protected AdminWebServerSystem AdminWebServerSystem;
        private static readonly AdminWebServer.Config Config = AdminWebServer.DefaultConfig;

        [SetUp]
        public void InitServer()
        {
            // init admin web server
            var serverConfig = AdminWebServer.DefaultConfig;
            AdminWebServerSystem = new AdminWebServerSystem(serverConfig);
            DefaultWorld.AddSystem(AdminWebServerSystem);
            // don't forget to call AdminWebServerSystem.Update() to start the server
        }

        [TearDown]
        public void StopServer()
        {
            // cleanup
            DefaultWorld.DestroySystem(AdminWebServerSystem);
        }
        
        [UnityTest]
        public IEnumerator ListPlayersCommandSystemTest()
        {
            // add commands
            DefaultWorld.AddSystem(new ListPlayersCommandSystem());
            
            // add a couple of "players"
            DefaultManager.AddComponentData(DefaultManager.CreateEntity(), new NetworkIdComponent
            {
                Value = 73313
            });
            
            DefaultManager.AddComponentData(DefaultManager.CreateEntity(), new NetworkIdComponent
            {
                Value = 7683
            });
            
            AdminWebServerSystem.Update(); // update once to start web server
            yield return null;
            
            var url = AdminWebServer.BuildCommandUrl(Config, ListPlayersCommandSystem.CommandId);
            var client = UnityWebRequest.Get(url);
            client.SendWebRequest();
            while (!client.isDone)
            {
                yield return null;
            }
            Debug.Log($"Request complete with: {client.responseCode} {client.result}");
            Assert.AreEqual(200, client.responseCode);
            Assert.AreEqual(UnityWebRequest.Result.Success, client.result);
            Assert.IsFalse(string.IsNullOrEmpty(client.downloadHandler.text));
            
            var reply = JsonUtility.FromJson<ListPlayersMessage>(client.downloadHandler.text);
            Assert.IsNotNull(reply.players);
            Assert.AreEqual(2, reply.players.Length);
            Assert.AreEqual(73313, reply.players[0].id);
            Assert.AreEqual(7683, reply.players[1].id);
        }
    }
}