using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoDoubleSequenceService
    {
        private readonly static object _factoryLock = new object();
        private readonly static Dictionary<string, ChoDoubleSequenceService> _counterDict = new Dictionary<string, ChoDoubleSequenceService>();

        private readonly object _padLock = new object();
        private Double _counter = 0;
        private Double _seed = 0;

        public ChoDoubleSequenceService(Double seed = 0)
        {
            _seed = seed;
            Reset();
        }

        public Double Current
        {
            get { return _counter; }
        }

        public Double Increment(Double value = 1)
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

        public static ChoDoubleSequenceService NewService(string name, Double seed = 0)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (_counterDict.ContainsKey(name))
                throw new ApplicationException("'{0}' sequence service already exists.".FormatString(name));

            lock (_factoryLock)
            {
                if (_counterDict.ContainsKey(name))
                    throw new ApplicationException("'{0}' sequence service already exists.".FormatString(name));

                _counterDict.Add(name, new ChoDoubleSequenceService(seed));
                return _counterDict[name];
            }
        }

        public static ChoDoubleSequenceService GetService(string name)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (_counterDict.ContainsKey(name))
                return _counterDict[name];
            else
                throw new ApplicationException("'{0}' sequence service NOT exists.".FormatString(name));
        }
    }
}
