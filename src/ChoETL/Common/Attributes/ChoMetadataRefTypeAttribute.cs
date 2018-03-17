namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Linq;

	#endregion

	[AttributeUsage(AttributeTargets.Class)]
	public class ChoMetadataRefTypeAttribute : Attribute
	{
		public ChoMetadataRefTypeAttribute(Type metadataRefClassType)
		{
			MetadataRefClassType = metadataRefClassType;
		}

		public Type MetadataRefClassType { get; private set; }
	}
}
