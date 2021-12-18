using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ChoDisableAutoDiscoverabilityAttribute : Attribute
	{
		public bool Flag
        {
			get;
			private set;
        }
		public ChoDisableAutoDiscoverabilityAttribute(bool flag = true)
		{
			Flag = flag;
		}
	}
}
