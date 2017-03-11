namespace ChoETL
{
	#region NameSpaces

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections;

	#endregion NameSpaces

	public class ChoIndexedEnumerator<T> : IEnumerator<Tuple<long, T>>
	{
		#region Instance Data Members (Private)

		private readonly IEnumerable<T> _enumerable;
		private IEnumerator<T> _enumerator;
		private int _currentIndex;

		#endregion Instance Data Members (Private)

		#region Constructors

		public ChoIndexedEnumerator(IEnumerable<T> enumerable)
		{
			ChoGuard.ArgumentNotNull(enumerable, "enumerable");
			_enumerable = enumerable;

			Reset();
		}

		public ChoIndexedEnumerator(IEnumerator<T> enumerator)
		{
			ChoGuard.ArgumentNotNull(enumerator, "enumerator");
			_enumerator = enumerator;

			Reset();
		}

		#endregion Constructors

		#region IEnumerator Members

		public Tuple<long, T> Current
		{
			get
			{
				return new Tuple<long, T>(_currentIndex, _enumerator.Current);
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return Current;
			}
		}

		public bool MoveNext()
		{
			if (!_enumerator.MoveNext())
				return false;

			_currentIndex++;
			return true;
		}

		public void Reset()
		{
			_currentIndex = 0;
			_enumerator = _enumerable.GetEnumerator();
		}

		public void Dispose()
		{
			Reset();
		}

		#endregion
	}
}

