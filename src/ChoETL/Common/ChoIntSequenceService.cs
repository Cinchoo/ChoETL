using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoIntSequenceService
    {
        private readonly static object _factoryLock = new object();
        private readonly static Dictionary<string, ChoIntSequenceService> _counterDict = new Dictionary<string, ChoIntSequenceService>();

        private readonly object _padLock = new object();
        private int _counter = 0;
        private int _seed = 0;

        public ChoIntSequenceService(int seed = 0)
        {
            _seed = seed;
            Reset();
        }

        public int Current
        {
            get { return _counter; }
        }

        public int Increment(int value = 1)
        {
            lock (_padLock)
            {
                _counter += value;
                return _counter;
            }
        }

        public void Reset()
        {
            lock (_padLock)
            {
                _counter = _seed;
            }
        }

        public static ChoIntSequenceService NewService(string name, int seed = 0)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (_counterDict.ContainsKey(name))
                throw new ApplicationException("'{0}' sequence service already exists.".FormatString(name));

            lock (_factoryLock)
            {
                if (_counterDict.ContainsKey(name))
                    throw new ApplicationException("'{0}' sequence service already exists.".FormatString(name));

                _counterDict.Add(name, new ChoIntSequenceService(seed));
                return _counterDict[name];
            }
        }

        public static ChoIntSequenceService GetService(string name)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (_counterDict.ContainsKey(name))
                return _counterDict[name];
            else
                throw new ApplicationException("'{0}' sequence service NOT exists.".FormatString(name));
        }
    }
}
