using NucleusAF.Interfaces.Abstractions.CompositeRegistry;

namespace NucleusAF.Abstractions.CompositeRegistry
{
    public sealed class CompositeComponentRegistry<TComponent>(IEnumerable<TComponent> components)
        : CompositeComponentRegistryBase<TComponent>(components)
        where TComponent : ICompositeRegistryComponent
    { }

    public abstract class CompositeComponentRegistryBase<TComponent>
        : ICompositeComponentRegistry<Type, TComponent>, IDisposable
        where TComponent : ICompositeRegistryComponent
    {
        protected readonly CompositeRegistry<Type, TComponent> CompositeRegistry = new();
        protected bool IsDisposed;

        protected ICompositeRegistry<Type, TComponent> Registry => this.CompositeRegistry;

        public CompositeComponentRegistryBase(IEnumerable<TComponent> components) => this.RegisterComponents(components);

        protected virtual void RegisterComponents(IEnumerable<TComponent> components)
        {
            foreach (TComponent component in components)
                this.CompositeRegistry.AddRegistrant(component.Key, component);
        }

        #region CompositeRegistryBase: Predicates

        public virtual bool IsRegistered(Type id, TComponent handler)
            => this.CompositeRegistry.IsRegistered(id, handler);

        #endregion

        #region CompositeRegistryBase: Get Operations

        public virtual TComponent GetComponent(Type id)
            => this.CompositeRegistry.GetRegistrant(id);
        public virtual IReadOnlyList<TComponent> GetComponents(Type id)
            => this.CompositeRegistry.GetRegistrants(id);

        public virtual IReadOnlyList<TComponent> GetAllComponents()
            => this.CompositeRegistry.GetAllRegistrants();

        #endregion

        #region CompositeRegistryBase: Try Get Operations

        public virtual bool TryGetComponent(Type id, out TComponent? handler)
            => this.CompositeRegistry.TryGetRegistrant(id, out handler);
        public virtual bool TryGetComponents(Type id, out IReadOnlyList<TComponent>? result)
            => this.CompositeRegistry.TryGetRegistrants(id, out result);

        #endregion

        public virtual void Dispose()
        {
            if (this.IsDisposed) return;

            this.IsDisposed = true;

            foreach (TComponent handler in this.CompositeRegistry.GetAllRegistrants())
                if (handler is IDisposable d)
                    try { d.Dispose(); } catch { /* GULP! */ }

            GC.SuppressFinalize(this);
        }
    }
}