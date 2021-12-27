using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public unsafe class ChoObjectPool<T>
        where T : new()
    {
        private int _pointer = 0;
        private T[] _cache = null;
        private T[] _backupCache = null;
        private int _size = 1024;
        private object _padLock = new object();

        public ChoObjectPool(int size = 1024)
        {
            if (size > 1024)
                _size = size;

            _cache = new T[_size];
            _backupCache = new T[_size];
            Parallel.For(0, _size, i => _backupCache[i] = ChoActivator.CreateInstance<T>());
            Initialize();
        }

        private void Initialize()
        {
            _pointer = 0;
            Array.Copy(_backupCache, _cache, _size);
            Task.Factory.StartNew(() =>  Parallel.For(0, _size, i => _backupCache[i] = ChoActivator.CreateInstance<T>()));
        }

        public T GetNext()
        {
            if (_pointer >= _size)
                Initialize();

            return _cache[_pointer++];
        }

        public T GetNextSync()
        {
            lock (_padLock)
            {
                return GetNext();
            }
        }
    }
}
