
using System;
using ICVR.Dots.Admin.Messages;

// ReSharper disable once CheckNamespace

namespace ICVR.Dots.Admin.Commands
{
    public interface IServerCommand : IDisposable
    {
        string Id { get; }
        IServerMessage Process(string data);
    }
}