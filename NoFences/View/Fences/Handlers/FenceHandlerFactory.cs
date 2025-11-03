using NoFences.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoFences.View.Fences.Handlers
{
    public class FenceHandlerFactory
    {
        private readonly Dictionary<string, Type> _handlers;

        public FenceHandlerFactory(Dictionary<string, Type> handlers)
        {
            foreach(var handlerType in handlers.Values)
            {
                if (!typeof(IFenceHandler).IsAssignableFrom(handlerType))
                {
                    throw new ArgumentException($"Type {handlerType.FullName} does not implement IFenceHandler.");
                }
            }
            _handlers = handlers;
        }

        public IFenceHandler CreateFenceHandler(FenceInfo fenceInfo)
        {
            return (IFenceHandler) Activator.CreateInstance(_handlers.FirstOrDefault(h => h.Key == fenceInfo.Type).Value);
        }
    }
}
