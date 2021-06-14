using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICVR.Dots.Admin.Commands;
using ICVR.Dots.Admin.Messages;
using UnityEngine;

namespace ICVR.Dots.Admin
{
    public class AdminWebServer : IDisposable
    {
        public enum SchemeType
        {
            Http,
            Https
        }

        internal const string DefaultLogTag = "AdminWebServer";

        public static readonly Config DefaultConfig = new Config
        {
            Scheme = SchemeType.Http,
            ServerAddress = "127.0.0.1",
            ServerPort = 8090,
            Logger = Debug.unityLogger,
            LogTag = DefaultLogTag
        };

        private readonly IServerCommand[] _commands;

        private readonly Config _config;

        private CancellationTokenSource _cancellationToken;
        private HttpListener _listener;

        public AdminWebServer() : this(DefaultConfig)
        {
        }

        public AdminWebServer(Config config, params IServerCommand[] commands)
        {
            _commands = commands.Prepend(new GetUtcTimeCommand()).ToArray();
            _config = config;
        }

        public IEnumerable<string> Commands => _commands.Select(x => x.Id);

        public bool IsRunning => _listener != null
                                 && _listener.IsListening
                                 && _cancellationToken != null
                                 && !_cancellationToken.IsCancellationRequested;

        public void Dispose()
        {
            Stop();
        }

        public async void Start()
        {
            if (!ValidateConfig(_config)) throw new InvalidOperationException("Config is invalid");

            _listener = new HttpListener();
            foreach (var prefix in _commands)
            {
                var url = BuildCommandUrl(_config, prefix.Id);
                LogInfo($"Adding prefix: {prefix.Id} => {url}");
                _listener.Prefixes.Add(url);
            }

            _listener.Start();
            LogInfo("Staring listener...");

            _cancellationToken = new CancellationTokenSource();

            try
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync().WaitOrCancel(_cancellationToken.Token);
                    if (_listener.IsListening && !_cancellationToken.IsCancellationRequested)
                        ProcessRequest(context.Request, context.Response);
                }
            }
            catch (OperationCanceledException)
            {
                LogInfo("Listener stopped on request");
            }
            catch (Exception exception)
            {
                LogError($"Listener exception: {exception.Message}");
            }

            _listener.Close();
        }

        private void ProcessRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            LogInfo($"Processing request: {request.RawUrl}");
            if (_config.ExtendedLogs) LogInfo($"Request headers: {request.Headers}");
            var commandId = request.Url.LocalPath.Trim('/');
            var command = GetFirstCommand(commandId);
            if (command == null)
            {
                WriteJsonResponse(response,
                    HttpStatusCode.NotImplemented,
                    new ErrorMessage
                    {
                        errorCode = (int) ErrorCodes.MethodNotImplemented,
                        errorMessage = $"Invalid command: {commandId}"
                    });
                return;
            }

            try
            {
                var commandResponse = command.Process(request.Url.Query);
                WriteJsonResponse(response, HttpStatusCode.OK, commandResponse);
            }
            catch (Exception exception)
            {
                WriteJsonResponse(response,
                    HttpStatusCode.BadRequest,
                    new ErrorMessage
                    {
                        errorCode = exception.HResult,
                        errorMessage = $"Command exception: {exception.Message}",
                        stackTrace = exception.StackTrace
                    });
            }
        }

        private IServerCommand GetFirstCommand(string commandId)
        {
            foreach (var command in _commands)
                if (string.Compare(commandId, command.Id, StringComparison.OrdinalIgnoreCase) == 0)
                    return command;
            return null;
        }

        private void WriteResponse(HttpListenerResponse response, HttpStatusCode code, string message)
        {
            response.StatusCode = (int) code;
            response.Headers.Add("Content-Type", "text/plain");

            var stream = response.OutputStream;
            var buffer = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.Length;
            stream.Write(buffer, 0, buffer.Length);
            stream.Close();
        }

        private void WriteJsonResponse<TMessage>(HttpListenerResponse response, HttpStatusCode code,
            TMessage responseJson)
            where TMessage : IServerMessage
        {
            response.StatusCode = (int) code;
            response.Headers.Add("Content-Type", "application/json");

            var stream = response.OutputStream;
            var buffer = Encoding.UTF8.GetBytes(JsonUtility.ToJson(responseJson));
            response.ContentLength64 = buffer.Length;
            stream.Write(buffer, 0, buffer.Length);
            stream.Close();
        }


        private bool ValidateConfig(Config config)
        {
            if (config.Scheme == SchemeType.Https)
                throw new NotImplementedException("HTTPS scheme is not implemented");

            if (config.ServerPort < 1024)
                throw new ArgumentException("Port should be non-privileged,. e.g. above 1024");

            return !string.IsNullOrEmpty(config.ServerAddress) && config.ServerPort < 65535;
        }

        public static string BuildCommandUrl(SchemeType scheme, string address, int port, string commandId)
        {
            return $"{scheme}://{address}:{port}/{commandId}/".ToLowerInvariant();
        }

        private void LogInfo(string message)
        {
            _config.Logger?.Log(_config.LogTag, $"{DateTime.Now:u} {message}");
        }

        private void LogError(string message)
        {
            _config.Logger?.LogError(_config.LogTag, $"{DateTime.Now:u} <color=#ff0000>{message}</color>");
        }

        private void LogWarning(string message)
        {
            _config.Logger?.LogWarning(_config.LogTag, $"{DateTime.Now:u} <color=#00ffff>{message}</color>");
        }

        private void Stop()
        {
            if (_commands != null)
                foreach (var command in _commands)
                    command.Dispose();
            _cancellationToken?.Cancel();
        }

        public string BuildCommandUrl(string commandId)
        {
            return BuildCommandUrl(_config, commandId);
        }

        public static string BuildCommandUrl(Config config, string commandId)
        {
            return BuildCommandUrl(config.Scheme, config.ServerAddress, config.ServerPort, commandId);
        }

        private enum ErrorCodes
        {
            MethodNotImplemented = 100
        }

        public struct Config
        {
            public SchemeType Scheme;
            public string ServerAddress;
            public int ServerPort;
            public string LogTag;
            public ILogger Logger;
            public bool ExtendedLogs;
        }
    }

    public class AdminWebServerBuilder
    {
        private readonly List<IServerCommand> _commands;
        private AdminWebServer.Config _config;

        public AdminWebServerBuilder(AdminWebServer.Config config)
        {
            _config = config;
            _commands = new List<IServerCommand>();
        }

        public static AdminWebServerBuilder Default()
        {
            return new AdminWebServerBuilder(AdminWebServer.DefaultConfig);
        }

        public AdminWebServerBuilder WithAddress(string address, int port)
        {
            _config.ServerAddress = address;
            _config.ServerPort = port;
            return this;
        }

        public AdminWebServerBuilder WithEndpoint(AdminWebServer.SchemeType scheme, string address, int port)
        {
            _config.Scheme = scheme;
            _config.ServerPort = port;
            _config.ServerAddress = address;
            return this;
        }

        public AdminWebServerBuilder WithGenericCommand(string id, Func<string, string> process)
        {
            return WithCommand(new FuncGenericCommand(id, process));
        }

        public AdminWebServerBuilder WithCommand(IServerCommand command)
        {
            _commands.Add(command);
            return this;
        }

        public AdminWebServerBuilder WithLogger(ILogger logger, string tag = AdminWebServer.DefaultLogTag)
        {
            _config.LogTag = tag;
            _config.Logger = logger;
            return this;
        }

        public AdminWebServer Build()
        {
            return new AdminWebServer(_config, _commands.ToArray());
        }

        public AdminWebServerBuilder WithExtendedLogs()
        {
            _config.ExtendedLogs = true;
            return this;
        }

        public static AdminWebServerBuilder WithConfig(AdminWebServer.Config config)
        {
            return new AdminWebServerBuilder(config);
        }

        public AdminWebServerBuilder WithCommands(IServerCommand[] serverCommands)
        {
            _commands.AddRange(serverCommands);
            return this;
        }
    }

    internal static class TaskExtensions
    {
        public static async Task<T> WaitOrCancel<T>(this Task<T> task, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await Task.WhenAny(task, token.WhenCanceled());
            token.ThrowIfCancellationRequested();

            return await task;
        }

        public static Task WhenCanceled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>) s).SetResult(true), tcs);
            return tcs.Task;
        }
    }
}