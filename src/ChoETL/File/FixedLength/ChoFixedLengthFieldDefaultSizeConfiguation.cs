using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChoETL
{
    public sealed class ChoFixedLengthFieldDefaultSizeConfiguation
    {
        private static readonly ThreadLocal<ChoFixedLengthFieldDefaultSizeConfiguation> _instance = new ThreadLocal<ChoFixedLengthFieldDefaultSizeConfiguation>(() => new ChoFixedLengthFieldDefaultSizeConfiguation());
        public static ChoFixedLengthFieldDefaultSizeConfiguation Instance
        {
            get { return _instance.Value; }
        }

        private readonly Dictionary<Type, int> _dataTypeSize = new Dictionary<Type, int>();

        public ChoFixedLengthFieldDefaultSizeConfiguation()
        {
            _dataTypeSize.Add(typeof(bool), 1);
            _dataTypeSize.Add(typeof(char), 1);
            _dataTypeSize.Add(typeof(Byte), 3);
            _dataTypeSize.Add(typeof(SByte), 3);
            _dataTypeSize.Add(typeof(Int16), 5);
            _dataTypeSize.Add(typeof(UInt16), 5);
            _dataTypeSize.Add(typeof(Int32), 10);
            _dataTypeSize.Add(typeof(UInt32), 10);
            _dataTypeSize.Add(typeof(Int64), 20);
            _dataTypeSize.Add(typeof(UInt64), 20);
            _dataTypeSize.Add(typeof(Single), 7);
            _dataTypeSize.Add(typeof(Double), 15);
            _dataTypeSize.Add(typeof(Decimal), 29);
            _dataTypeSize.Add(typeof(BigInteger), 50);
            _dataTypeSize.Add(typeof(Enum), 25);
            _dataTypeSize.Add(typeof(String), 25);
            _dataTypeSize.Add(typeof(DateTime), 10);
            _dataTypeSize.Add(typeof(ChoCurrency), 30);
            _dataTypeSize.Add(typeof(Object), 30);
            _dataTypeSize.Add(typeof(Guid), 36);
        }

        public int GetSize(Type type)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            if (!_dataTypeSize.ContainsKey(type))
                return _dataTypeSize[typeof(Object)];
                //throw new ArgumentException("Can't find size for '{0}' type.".FormatString(type.Name));

            return _dataTypeSize[type];
        }

        public void SetSize(Type type, int size)
        {
            ChoGuard.ArgumentNotNull(type, "Type");

            _dataTypeSize.AddOrUpdate(type, size);
        }
    }
}
