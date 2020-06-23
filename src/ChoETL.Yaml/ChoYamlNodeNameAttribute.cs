using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ChoYamlNodeNameAttribute : Attribute
	{
		public string Name
		{
			get;
			private set;
		}

		public ChoYamlNodeNameAttribute(string name)
		{
			Name = name;
		}
	}
}
