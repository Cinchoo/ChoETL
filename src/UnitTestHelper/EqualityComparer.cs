using System.Collections;
using System.Collections.Generic;

namespace UnitTestHelper
{
    public class ArrayListEqualityComparer : EqualityComparer<ArrayList>
    {
        public override bool Equals(ArrayList x, ArrayList y)
        {
            if (x == null && y == null)
                return true;
            if (x == null | y == null)
                return false;
            if (x.Count != y.Count)
                return false;
            for (int i = 0; i < x.Count; i++)
            {
                object xItem = x[i];
                object yItem = y[i];
                if (xItem == null ^ yItem == null)
                    return false;
                if (xItem == null)
                {
                    if (!(yItem.Equals(xItem)))
                        return false;
                }
                else
                {
                    if (!(xItem.Equals(yItem)))
                        return false;
                }
            }
            return true;
        }

        public override int GetHashCode(ArrayList obj)
        {
            return EqualityComparer<ArrayList>.Default.GetHashCode(obj);
        }
    }
    public class ArrayEqualityComparer<T> : EqualityComparer<T[]>
    {
        public override bool Equals(T[] x, T[] y)
        {
            if (x == null && y == null)
                return true;
            if (x == null | y == null)
                return false;
            if (x.GetLongLength(0) != y.GetLongLength(0))
                return false;
            if (x.GetLowerBound(0) != y.GetLowerBound(0))
                return false;
            if (x.GetUpperBound(0) != y.GetUpperBound(0))
                return false;
            if (x.GetLongLength(0) > 0)
                for (int i = x.GetLowerBound(0); i <= x.GetUpperBound(0); i++)
                {
                    if (!EqualityComparer<T>.Default.Equals(x[i], y[i]))
                        return false;
                }
            return true;
        }

        public override int GetHashCode(T[] obj)
        {
            return EqualityComparer<T[]>.Default.GetHashCode(obj);
        }
    }
    public class DictionaryEqualityComparer<TKey, TValue> : EqualityComparer<Dictionary<TKey, TValue>>
    {
        public override bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            if (x.Count != y.Count)
                return false;
            foreach (var keyItem in x.Keys)
            {
                if (!y.ContainsKey(keyItem) || !EqualityComparer<TValue>.Default.Equals(x[keyItem], y[keyItem]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode(Dictionary<TKey, TValue> obj)
        {
            return EqualityComparer<Dictionary<TKey, TValue>>.Default.GetHashCode(obj);
        }
    }
    public class ListEqualityComparer<T> : EqualityComparer<List<T>>
    {
        public override bool Equals(List<T> x, List<T> y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            if (x.Count != y.Count)
                return false;
            for (int i = 0; i < x.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(x[i], y[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode(List<T> obj)
        {
            return EqualityComparer<List<T>>.Default.GetHashCode(obj);
        }
    }
}
