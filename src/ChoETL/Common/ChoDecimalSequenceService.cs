using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoDecimalSequenceService
    {
        private readonly static object _factoryLock = new object();
        private readonly static Dictionary<string, ChoDecimalSequenceService> _counterDict = new Dictionary<string, ChoDecimalSequenceService>();

        private readonly object _padLock = new object();
        private Decimal _counter = 0;
        private Decimal _seed = 0;

        public ChoDecimalSequenceService(Decimal seed = 0)
        {
            _seed = seed;
            Reset();
        }

        public Decimal Current
        {
            get { return _counter; }
        }

        public Decimal Increment(Decimal value = 1)
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

        public static ChoDecimalSequenceService NewService(string name, Decimal seed = 0)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (_counterDict.ContainsKey(name))
                throw new ApplicationException("'{0}' sequence service already exists.".FormatString(name));

            lock (_factoryLock)
            {
                if (_counterDict.ContainsKey(name))
                    throw new ApplicationException("'{0}' sequence service already exists.".FormatString(name));

                _counterDict.Add(name, new ChoDecimalSequenceService(seed));
                return _counterDict[name];
            }
        }

        public static ChoDecimalSequenceService GetService(string name)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (_counterDict.ContainsKey(name))
                return _counterDict[name];
            else
                throw new ApplicationException("'{0}' sequence service NOT exists.".FormatString(name));
        }
    }
}
