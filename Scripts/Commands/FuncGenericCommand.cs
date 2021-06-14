using System;
using ICVR.Dots.Admin.Messages;

namespace ICVR.Dots.Admin.Commands
{
    public class FuncGenericCommand : IServerCommand
    {
        private readonly Func<string, string> _process;

        public FuncGenericCommand(string id, Func<string, string> process)
        {
            _process = process;
            Id = id;
        }

        public string Id { get; }

        public IServerMessage Process(string data)
        {
            return new GenericStringMessage
            {
                response = _process(data)
            };
        }

        public void Dispose()
        {
        }
    }
}