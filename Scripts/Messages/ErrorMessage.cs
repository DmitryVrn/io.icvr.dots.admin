using System;

namespace ICVR.Dots.Admin.Messages
{
    [Serializable]
    internal struct ErrorMessage : IServerMessage
    {
        public int errorCode;
        public string errorMessage;
        public string stackTrace;
    }
}