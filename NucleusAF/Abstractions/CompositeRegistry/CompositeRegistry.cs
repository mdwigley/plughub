using NucleusAF.Interfaces.Abstractions.CompositeRegistry;
using Serilog;
using System.Collections.Concurrent;

namespace NucleusAF.Abstractions.CompositeRegistry
{
    public sealed class CompositeRegistry<TKey, TValue>
        : CompositeRegistryBase<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    { }

    public abstract class CompositeRegistryBase<TKey, TValue>
        : ICompositeRegistry<TKey, TValue>, IDisposable
        where TKey : notnull
        where TValue : notnull
    {
        protected readonly ConcurrentDictionary<TKey, List<TValue>> Ledger = new();
        protected bool IsDisposed;

        protected IReadOnlyDictionary<TKey, List<TValue>> Registrants => this.Ledger;

        #region CompositeRegistryBase: Predicates

        public bool IsRegistered(TKey key, TValue registrant)
            => this.Ledger.TryGetValue(key, out List<TValue>? list) && list.Contains(registrant);
        public bool IsRegistered(TKey key)
            => this.Ledger.TryGetValue(key, out List<TValue>? list) && list.Count > 0;

        #endregion

        #region CompositeRegistryBase: Add/Remove

        public virtual void AddRegistrant(TKey key, TValue registrant)
        {
            List<TValue> list = this.Ledger.GetOrAdd(key, _ => []);

            lock (list) { list.Add(registrant); }
        }
        public virtual bool RemoveRegistrant(TKey key, TValue registrant)
        {
            if (this.Ledger.TryGetValue(key, out List<TValue>? list))
                lock (list) { return list.Remove(registrant); }
            return false;
        }
        public virtual void RemoveAllRegistrants(TKey key)
        {
            this.Ledger.TryRemove(key, out _);
        }
        public virtual void ClearAllRegistrants()
        {
            this.Ledger.Clear();
        }

        #endregion

        #region CompositeRegistryBase: Get Operations

        public virtual TValue GetRegistrant(TKey key)
        {
            if (this.Ledger.TryGetValue(key, out List<TValue>? list) && list.Count > 0)
                return list[^1]; // FILO: last in wins
            Log.Warning("[CompositeRegistryBase] No registrant registered for {Key}", key);
            throw new KeyNotFoundException($"No registrant registered for {key}.");
        }
        public virtual IReadOnlyList<TValue> GetRegistrants(TKey key)
        {
            if (this.Ledger.TryGetValue(key, out List<TValue>? list))
                return list.AsReadOnly();
            Log.Warning("[CompositeRegistryBase] No registrant(s) registered for {Key}", key);
            throw new KeyNotFoundException($"No registrant(s) registered for {key}.");
        }
        public virtual IReadOnlyList<TValue> GetKeyRegistrants()
        {
            List<TValue> result = [];

            foreach (TKey key in this.Ledger.Keys)
                result.Add(this.GetRegistrant(key));

            return result;
        }

        #endregion

        #region CompositeRegistryBase: Try Get Operations

        public virtual bool TryGetRegistrant(TKey key, out TValue? registrant)
        {
            if (this.Ledger.TryGetValue(key, out List<TValue>? list) && list.Count > 0)
            {
                registrant = list[^1];
                return true;
            }
            registrant = default;
            return false;
        }

        public virtual bool TryGetRegistrants(TKey key, out IReadOnlyList<TValue>? result)
        {
            if (this.Ledger.TryGetValue(key, out List<TValue>? list))
            {
                result = list.AsReadOnly();
                return true;
            }
            result = null;
            return false;
        }

        #endregion

        #region CompositeRegistryBase: Get All Operations

        public virtual IReadOnlyList<TValue> GetAllRegistrants(TKey key)
        {
            if (this.Ledger.TryGetValue(key, out List<TValue>? list))
                return list.AsReadOnly();
            Log.Warning("[CompositeRegistryBase] No registrant(s) registered for {Key}", key);
            throw new KeyNotFoundException($"No registrant(s) registered for {key}.");
        }
        public virtual IReadOnlyList<TValue> GetAllRegistrants()
        {
            List<TValue> all = [];
            foreach (KeyValuePair<TKey, List<TValue>> kvp in this.Ledger)
                all.AddRange(kvp.Value);
            return all.AsReadOnly();
        }

        #endregion

        #region CompositeRegistryBase: Try Get All Operations

        public virtual IReadOnlyList<TValue> TryGetAllRegistrants(TKey key)
        {
            if (this.Ledger.TryGetValue(key, out List<TValue>? list))
                return list.AsReadOnly();
            Log.Warning("[CompositeRegistryBase] No registrant(s) registered for {Key}", key);
            throw new KeyNotFoundException($"No registrant(s) registered for {key}.");
        }
        public virtual IReadOnlyList<TValue> TryGetAllRegistrants()
        {
            List<TValue> all = [];
            foreach (KeyValuePair<TKey, List<TValue>> kvp in this.Ledger)
                all.AddRange(kvp.Value);
            return all.AsReadOnly();
        }

        #endregion

        public void Dispose()
        {
            if (this.IsDisposed) return;
            this.IsDisposed = true;

            foreach (KeyValuePair<TKey, List<TValue>> kvp in this.Ledger)
            {
                foreach (TValue registrant in kvp.Value)
                {
                    if (registrant is IDisposable d)
                    {
                        try { d.Dispose(); } catch { /* swallow */ }
                    }
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}