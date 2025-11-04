using NoFences.Core.Model;
using NoFences.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoFences.View.Canvas.Handlers
{
    /// <summary>
    /// Factory for creating WPF-based fence handlers in the NEW canvas architecture.
    /// This is completely separate from the original FenceHandlerFactory.
    ///
    /// For the original WinForms factory, see View/Fences/Handlers/FenceHandlerFactory.cs
    /// </summary>
    public class FenceHandlerFactoryWpf
    {
        private readonly Dictionary<string, Type> handlers;

        public FenceHandlerFactoryWpf(Dictionary<string, Type> handlers)
        {
            // Validate that all handlers implement IFenceHandlerWpf
            foreach (var handlerType in handlers.Values)
            {
                if (!typeof(IFenceHandlerWpf).IsAssignableFrom(handlerType))
                {
                    throw new ArgumentException(
                        $"Type {handlerType.FullName} does not implement IFenceHandlerWpf.");
                }
            }

            this.handlers = handlers;
        }

        /// <summary>
        /// Creates a WPF fence handler based on the fence type.
        /// </summary>
        public IFenceHandlerWpf CreateFenceHandler(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            var handlerEntry = handlers.FirstOrDefault(h => h.Key == fenceInfo.Type);

            if (handlerEntry.Value == null)
            {
                throw new InvalidOperationException(
                    $"No handler registered for fence type: {fenceInfo.Type}");
            }

            var handler = (IFenceHandlerWpf)Activator.CreateInstance(handlerEntry.Value);
            handler.Initialize(fenceInfo);

            return handler;
        }
    }
}
