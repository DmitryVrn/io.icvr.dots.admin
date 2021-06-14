using System;

namespace ICVR.Dots.Admin.Messages
{
    [Serializable]
    public struct GenericStringMessage : IServerMessage
    {
        public string response;
    }
}