using ICVR.Dots.Admin.Commands;
using ICVR.Dots.Admin.Messages;
using Unity.Entities;

// ReSharper disable once CheckNamespace
namespace ICVR.Dots.Admin.Systems
{
    public abstract class AdminServerCommandSystemBase : SystemBase, IServerCommand
    {
        protected override void OnUpdate()
        {    
        }

        public virtual void Dispose()
        {
        }
        
        public abstract string Id { get; }

        public abstract IServerMessage Process(string data);
    }
}