using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
#if !NETSTANDARD2_0
    public class ChoSequenceNoGenerator : IValueConverter
#else
    public class ChoSequenceNoGenerator : IChoValueConverter
#endif
    {
        private string _seqName;
        private int _seed = 1;
        private int _step = 1;
        private ChoSequenceGenerator _gen = null;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (LoadParams(parameter.FirstOrDefault<string>()))
            {
                return _gen.Next();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (LoadParams(parameter.FirstOrDefault<string>()))
            {
                return _gen.Next();
            }
            return value;
        }

        private bool LoadParams(string parameter)
        {
            if (parameter.IsNullOrWhiteSpace()) return false;

            foreach (KeyValuePair<string, string> kvp in parameter.ToKeyValuePairs())
            {
                if (String.Compare(kvp.Key, "name", true) == 0)
                {
                    _seqName = kvp.Value;
                }
                else if (String.Compare(kvp.Key, "Seed", true) == 0)
                {
                    _seed = kvp.Value.CastTo<int>(1);
                }
                else if (String.Compare(kvp.Key, "Step", true) == 0)
                {
                    _step = kvp.Value.CastTo<int>(1);
                }
            }

            if (!_seqName.IsNullOrWhiteSpace())
            {
                _gen = ChoSequenceGenerator.GetSequenceGenerator(_seqName, _seed, _step);
                return true;
            }
            else
                return false;
        }
    }

    public class ChoSequenceGenerator
    {
        private int _nextSeqNo;
        private int _step;

        public ChoSequenceGenerator()
        {

        }

        public ChoSequenceGenerator(int seed, int step = 1)
        {
            _nextSeqNo = seed;
            if (step == 0)
                _step = 1;
            else
                _step = step;
        }

        public int Next()
        {
            int nextSeqNo = _nextSeqNo;
            _nextSeqNo = nextSeqNo + _step;
            return nextSeqNo;
        }

        private static readonly object _padLock = new object();
        private static readonly Dictionary<string, ChoSequenceGenerator> _seqGenDict = new Dictionary<string, ChoSequenceGenerator>(StringComparer.CurrentCultureIgnoreCase);
        public static ChoSequenceGenerator GetSequenceGenerator(string name, int seed = 1, int step = 1)
        {
            ChoGuard.ArgumentNotNullOrEmpty(name, "Name");

            if (_seqGenDict.ContainsKey(name))
                return _seqGenDict[name];

            lock (_padLock)
            {
                if (_seqGenDict.ContainsKey(name))
                    return _seqGenDict[name];

                _seqGenDict.Add(name, new ChoSequenceGenerator(seed, step));
                return _seqGenDict[name];
            }
        }
    }
}
