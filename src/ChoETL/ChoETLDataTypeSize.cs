using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoETLDataTypeSize
    {
        public static readonly ChoETLDataTypeSize Global = new ChoETLDataTypeSize();

        private Dictionary<Type, int> _dataTypeSize = new Dictionary<Type, int>();

        public ChoETLDataTypeSize()
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
            _dataTypeSize.Add(typeof(BigInteger), 100);
            _dataTypeSize.Add(typeof(Enum), 25);
            _dataTypeSize.Add(typeof(String), 255);
            _dataTypeSize.Add(typeof(DateTime), 10);
            _dataTypeSize.Add(typeof(ChoCurrency), 30);
        }

        public int GetSize(Type type)
        {
            if (!type.IsSimple())
                throw new ArgumentException("Invalid type passed. Expected simple type only.");

            return _dataTypeSize[type];
        }

        public int SetSize(Type type, int size)
        {
            if (!type.IsSimple())
                throw new ArgumentException("Invalid type passed. Expected simple type only.");
            if (size <= 0)
                throw new ArgumentException("Size must be > 0.");

            return _dataTypeSize[type] = size;
        }
    }
}
