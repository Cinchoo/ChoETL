using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ChoJSONNRootNameAttribute : Attribute
	{
		public string Name
		{
			get;
			private set;
		}

		public ChoJSONNRootNameAttribute(string name)
		{
			Name = name;
		}
	}
}
