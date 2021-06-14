using System;
using ICVR.Dots.Admin.Messages;

namespace ICVR.Dots.Admin.Commands
{
    public class GetUtcTimeCommand : IServerCommand
    {
        public static readonly string Id = "getUtcTime";

        string IServerCommand.Id => Id;

        public IServerMessage Process(string data)
        {
            return new GenericStringMessage
            {
                response = DateTime.UtcNow.ToString("u")
            };
        }

        public void Dispose()
        {
        }
    }
}