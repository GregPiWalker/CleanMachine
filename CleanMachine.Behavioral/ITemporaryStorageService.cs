using System;
using System.Collections.Generic;

namespace CleanMachine.Behavioral
{
    public interface ITemporaryStorageService : IDisposable
    {
        void Deposit(object id, object o);

        TResult Withdraw<TResult>(object id);

        void Clear();

        bool HasItem(object id);
    }

    public class TemporaryStorageService : ITemporaryStorageService
    {
        private Dictionary<object, object> _store = new Dictionary<object, object>();

        public void Deposit(object id, object o)
        {
            _store[id] = o;
        }

        public TResult Withdraw<TResult>(object id)
        {
            TResult o = (TResult)_store[id];
            _store.Remove(id);
            return o;
        }

        public void Clear()
        {
            _store.Clear();
        }

        public bool HasItem(object id) => _store.ContainsKey(id);

        public void Dispose()
        {
            Clear();
        }
    }
}
