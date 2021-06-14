using System;
using System.Collections;
using ICVR.Dots.Admin;
using ICVR.Dots.Admin.Commands;
using ICVR.Dots.Admin.Messages;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace ICVR.Tests
{
    public class AdminWebServerBaseTests
    {
        private CompositeDisposable _disposables;

        [SetUp]
        public void Init()
        {
            _disposables = new CompositeDisposable();
        }

        [TearDown]
        public void Cleanup()
        {
            _disposables.Dispose();
        }

        [Test]
        public void DefaultBuilder()
        {
            var server = AdminWebServerBuilder
                .Default()
                .Build()
                .AddTo(_disposables);

            Assert.IsNotNull(server);
        }

        [Test]
        public void PrefixUrlBuilder()
        {
            var buildPrefixUrl =
                AdminWebServer.BuildCommandUrl(AdminWebServer.SchemeType.Http, "example.com", 7898, "foo");

            Assert.IsNotNull(buildPrefixUrl);
            Assert.AreEqual("http://example.com:7898/foo/",
                buildPrefixUrl);
        }

        [UnityTest]
        public IEnumerator ServerStartStop()
        {
            Debug.Log("Starting server");

            var server = AdminWebServerBuilder
                .Default()
                .Build()
                .AddTo(_disposables);

            Assert.DoesNotThrow(() => server.Start());
            
            yield return null;

            Assert.IsTrue(server.IsRunning);

            Debug.Log("Stopping server");

            server.Dispose();

            yield return null;

            Assert.IsFalse(server.IsRunning);
        }

        [UnityTest]
        public IEnumerator InvalidPortTest()
        {
            /*
            Unity doesn't throw in async, the test will always fail :(
            AdminWebServer server = null;
            
            server = AdminWebServerBuilder
                .Default()
                .WithEndpoint(AdminWebServer.SchemeType.Http, "foo", 1000)
                .Build();
            
            yield return null;
            
            Assert.IsNotNull(server);
            Assert.IsFalse(server.IsRunning);
            
            server.Dispose();
            */
            Debug.LogWarning("Unity doesn't throw in async, the test will always fail :(");
            yield return null;
        }

        [UnityTest]
        public IEnumerator IsAliveTest()
        {
            var server = AdminWebServerBuilder
                .Default()
                .Build()
                .AddTo(_disposables);

            server.Start();
            
            yield return PerformIsAliveRequest(server);
        }

        [UnityTest]
        public IEnumerator ExceptionInCommandTest()
        {
            var faultyCommand = "exception";

            var server = AdminWebServerBuilder
                .Default()
                .WithGenericCommand(faultyCommand, data => throw new InvalidOperationException("Test"))
                .Build()
                .AddTo(_disposables);

            server.Start();
            
            Debug.Log("Checking server is alive");

            yield return PerformIsAliveRequest(server);

            Debug.Log("Performing faulty request");

            yield return PerformRequest(server, faultyCommand);

            Debug.Log("Checking server is still alive");

            yield return PerformIsAliveRequest(server);
        }

        [UnityTest]
        public IEnumerator DataReturnTest()
        {
            var fooCommand = "foo";
            var expectedResult = "bar";

            var server = AdminWebServerBuilder
                .Default()
                .WithGenericCommand(fooCommand, data => expectedResult)
                .Build()
                .AddTo(_disposables);

            server.Start();
            
            Debug.Log("Performing foo request");

            yield return PerformGetRequestWithSuccess(server, fooCommand,
                response =>
                {
                    var message = JsonUtility.FromJson<GenericStringMessage>(response);
                    return string.CompareOrdinal(expectedResult, message.response) == 0;
                });
        }
        
        [UnityTest]
        public IEnumerator DataReceivedTest()
        {
            var fooCommand = "foo";
            var expectedData = "foo=bar&baz=2";
            var dataEqual = false;
            
            var server = AdminWebServerBuilder
                .Default()
                .WithExtendedLogs()
                .WithGenericCommand(fooCommand, receivedData =>
                {
                    Debug.Log("Received data: " + receivedData);
                    receivedData = receivedData.Substring(1); // remove trailing ?
                    dataEqual = string.CompareOrdinal(expectedData, receivedData) == 0;
                    return "";
                })
                .Build()
                .AddTo(_disposables);

            server.Start();
            
            Debug.Log("Performing foo request");

            yield return PerformGetRequestWithSuccess(server, fooCommand, expectedData, _ => dataEqual);
        }
        
        [UnityTest]
        public IEnumerator CommandExecutionTest()
        {
            var fooCommand = "foo";
            var wasExecuted = false;

            var server = AdminWebServerBuilder
                .Default()
                .WithGenericCommand(fooCommand, data =>
                {
                    wasExecuted = true;
                    return "";
                })
                .Build()
                .AddTo(_disposables);

            server.Start();
            
            Debug.Log("Performing foo request");

            yield return PerformGetRequestWithSuccess(server, fooCommand, _ => wasExecuted);
        }

        private IEnumerator PerformIsAliveRequest(AdminWebServer server)
        {
            var request = UnityWebRequest.Get(server.BuildCommandUrl(GetUtcTimeCommand.Id));
            request.SendWebRequest();
            while (!request.isDone) yield return null;
            Assert.AreEqual(UnityWebRequest.Result.Success, request.result);
            Assert.AreEqual(200, request.responseCode);
            Assert.IsFalse(string.IsNullOrEmpty(request.downloadHandler.text));
        }

        private IEnumerator PerformRequest(AdminWebServer server, string requestId)
        {
            var request = UnityWebRequest.Get(server.BuildCommandUrl(requestId));
            request.SendWebRequest();
            while (!request.isDone) yield return null;
        }

        private IEnumerator PerformGetRequestWithSuccess(AdminWebServer server, string requestId, string data,
            Predicate<string> validatePredicate)
        {
            var url = server.BuildCommandUrl(requestId);
            if (!string.IsNullOrEmpty(data)) url += "?" + data;
            var request = UnityWebRequest.Get(url);
            request.SendWebRequest();
            while (!request.isDone) yield return null;
            
            Debug.Log("Result: " + request.result);
            Debug.Log("Code: " + request.responseCode);
            Debug.Log("Response: " + request.downloadHandler.text);
            
            Assert.AreEqual(UnityWebRequest.Result.Success, request.result);
            Assert.AreEqual(200, request.responseCode);
            Assert.IsTrue(validatePredicate(request.downloadHandler.text));
        }
        
        private IEnumerator PerformGetRequestWithSuccess(AdminWebServer server, string requestId,
            Predicate<string> validatePredicate)
        {
            yield return PerformGetRequestWithSuccess(server, requestId, "", validatePredicate);
        }
    }
}