using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using Serilog;

namespace NucleusAF.Abstractions.CompositeRegistry
{
    public sealed class CompositeAccessorRegistry<TAccessor, THandler>(IEnumerable<TAccessor> accessors, IEnumerable<THandler> handlers)
        : CompositeAccessorRegistryBase<TAccessor, THandler>(accessors, handlers) where TAccessor : ICompositeRegistryAccessor where THandler : ICompositeRegistryHandler
    { }

    public abstract class CompositeAccessorRegistryBase<TAccessor, THandler>(IEnumerable<TAccessor> accessors, IEnumerable<THandler> handlers)
        : ICompositeAccessorRegistry<TAccessor, THandler>
        where TAccessor : ICompositeRegistryAccessor
        where THandler : ICompositeRegistryHandler
    {
        private readonly CompositeComponentRegistry<TAccessor> accessorRegistry = new(accessors);
        private readonly CompositeComponentRegistry<THandler> handlerRegistry = new(handlers);

        #region CompositeAccessorRegistryBase: Predicates

        public bool DoesRegistryAccessorExist(Type id, TAccessor accessor)
            => this.accessorRegistry.IsRegistered(id, accessor);
        public bool DoesRegistryHandlerExist(Type id, THandler handler)
            => this.handlerRegistry.IsRegistered(id, handler);

        public bool IsRegistryAccessorForHandler(Type handlerId, TAccessor accessor)
            => this.handlerRegistry.TryGetComponent(handlerId, out THandler? handler) && this.IsRegistryAccessorForHandler(handler!, accessor);
        public bool IsRegistryAccessorAvailableFor(Type handlerId)
            => this.TryGetRegistryAccessorsFor(handlerId, out IReadOnlyList<TAccessor>? accessors) && accessors?.Count > 0;
        public bool IsRegistryAccessorForHandler(THandler handler, TAccessor accessor)
        {
            Type? accessorIdentity = accessor.GetType().GetInterfaces()
                .FirstOrDefault(i => typeof(ICompositeRegistryAccessor).IsAssignableFrom(i) && i != typeof(ICompositeRegistryAccessor));

            return accessorIdentity is not null && handler.GetType().GetInterfaces()
                .Any(i => i.IsGenericType &&
                          i.GetGenericTypeDefinition() == typeof(ICompositeRegistryHandlerFor<>) &&
                          i.GenericTypeArguments[0] == accessorIdentity);
        }
        public bool IsRegistryAccessorAvailableFor(THandler handler)
        {
            return this.TryGetRegistryAccessorsFor(handler, out IReadOnlyList<TAccessor>? accessors) && accessors?.Count > 0;
        }

        #endregion

        #region CompositeAccessorRegistryBase: Accessor Operations

        public TAccessor GetRegistryAccessor(Type id)
            => this.accessorRegistry.GetComponent(id);
        public IReadOnlyList<TAccessor> GetRegistryAccessors(Type id)
            => this.accessorRegistry.GetComponents(id);

        public TAccessor GetRegistryAccessorFor(Type handlerId)
        {
            if (!this.TryGetRegistryAccessorFor(handlerId, out TAccessor? accessor))
            {
                Log.Warning("[CompositeAccessorRegistryBase] No accessor found for handler id {HandlerId}", handlerId);
                throw new KeyNotFoundException($"No accessor found for handler id {handlerId}");
            }
            return accessor!;
        }
        public IReadOnlyList<TAccessor> GetRegistryAccessorsFor(Type handlerId)
        {
            if (!this.TryGetRegistryAccessorsFor(handlerId, out IReadOnlyList<TAccessor>? accessors))
            {
                Log.Warning("[CompositeAccessorRegistryBase] No accessors found for handler type {HandlerType}", handlerId);
                throw new KeyNotFoundException($"No accessors found for handler type {handlerId}");
            }
            return accessors!;
        }
        public TAccessor GetRegistryAccessorFor(THandler handler)
        {
            if (!this.TryGetRegistryAccessorFor(handler, out TAccessor? accessor))
            {
                Log.Warning("[CompositeAccessorRegistryBase] No accessor found for handler {Handler}", handler);
                throw new KeyNotFoundException($"No accessor found for handler {handler}");
            }
            return accessor!;
        }
        public IReadOnlyList<TAccessor> GetRegistryAccessorsFor(THandler handler)
        {
            if (!this.TryGetRegistryAccessorsFor(handler, out IReadOnlyList<TAccessor>? accessors))
            {
                Log.Warning("[CompositeAccessorRegistryBase] No accessors found for handler {HandlerType}", handler.GetType().Name);
                throw new KeyNotFoundException($"No accessors found for handler {handler.GetType().Name}");
            }
            return accessors!;
        }

        #endregion

        #region CompositeAccessorRegistryBase: Accessor Try Operations

        public bool TryGetRegistryAccessor(Type id, out TAccessor? accessor)
            => this.accessorRegistry.TryGetComponent(id, out accessor);
        public bool TryGetRegistryAccessors(Type id, out IReadOnlyList<TAccessor>? accessors)
            => this.accessorRegistry.TryGetComponents(id, out accessors);

        public bool TryGetRegistryAccessorFor(Type handlerId, out TAccessor? accessor)
        {
            if (this.TryGetRegistryAccessorsFor(handlerId, out IReadOnlyList<TAccessor>? accessors) && accessors != null && accessors.Count > 0)
            {
                accessor = accessors[^1];

                return accessor != null;
            }

            accessor = default;

            return false;
        }
        public bool TryGetRegistryAccessorsFor(Type handlerId, out IReadOnlyList<TAccessor>? accessors)
        {
            if (!this.handlerRegistry.TryGetComponent(handlerId, out THandler? handler))
            {
                accessors = null;
                return false;
            }
            return this.TryGetRegistryAccessorsFor(handler!, out accessors);
        }
        public bool TryGetRegistryAccessorFor(THandler handler, out TAccessor? accessor)
        {
            if (this.TryGetRegistryAccessorsFor(handler, out IReadOnlyList<TAccessor>? accessors) && accessors != null && accessors.Count > 0)
            {
                accessor = accessors[^1];

                return accessor != null;
            }

            accessor = default;

            return false;
        }
        public bool TryGetRegistryAccessorsFor(THandler handler, out IReadOnlyList<TAccessor>? accessors)
        {
            IEnumerable<Type> supportedAccessorTypes = handler.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICompositeRegistryHandlerFor<>))
                .Select(i => i.GenericTypeArguments[0]);

            List<TAccessor> results = [];

            foreach (Type type in supportedAccessorTypes)
                if (this.accessorRegistry.TryGetComponents(type, out IReadOnlyList<TAccessor>? list) && list != null)
                    results.AddRange(list);

            if (results.Count == 0)
            {
                accessors = null;
                return false;
            }
            accessors = results;
            return true;
        }

        #endregion

        #region CompositeAccessorRegistryBase: Handler Operations

        public THandler GetRegistryHandler(Type id)
            => this.handlerRegistry.GetComponent(id);
        public IReadOnlyList<THandler> GetRegistryHandlers(Type id)
            => this.handlerRegistry.GetComponents(id);

        public IReadOnlyList<THandler> GetAllRegistryHandlers()
            => this.handlerRegistry.GetAllComponents();

        #endregion

        #region CompositeAccessorRegistryBase: Handler Try Operations

        public bool TryGetRegistryHandler(Type id, out THandler? handler)
            => this.handlerRegistry.TryGetComponent(id, out handler);
        public bool TryGetRegistryHandlers(Type id, out IReadOnlyList<THandler>? handlers)
            => this.handlerRegistry.TryGetComponents(id, out handlers);

        #endregion
    }
}