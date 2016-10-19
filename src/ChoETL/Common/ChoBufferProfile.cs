namespace ChoETL
{
	#region NameSpaces

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    #endregion NameSpaces

    [Serializable]
	public class ChoBufferProfile : ChoBaseProfile
	{
		#region Instance Data Members (Private)

		private readonly StringBuilder _msg = new StringBuilder();

		#endregion Instance Data Members (Private)

		#region Constrctors

		public ChoBufferProfile(string msg, ChoBaseProfile outerProfile = null)
			: base(msg, outerProfile == null ? ChoETLFramework.GlobalProfile : outerProfile)
		{
		}

        internal ChoBufferProfile(bool condition, string msg, ChoBaseProfile outerProfile = null)
            : base(condition, msg, outerProfile == null ? ChoETLFramework.GlobalProfile : outerProfile)
		{
		}

		#endregion Constrctors

		protected override void Flush()
		{
			WriteToBackingStore(_msg.ToString());
		}

		protected override void Write(string msg)
		{
			_msg.Append(msg);
		}
	}

    [Serializable]
    public class ChoProfile : ChoBaseProfile
    {
        #region Constrctors

        public ChoProfile(string msg)
            : base(msg, null)
        {
        }

        internal ChoProfile(bool condition, string msg)
            : base(condition, msg, null)
        {
        }

        #endregion Constrctors

        protected override void Flush()
        {
        }

        protected override void Write(string msg)
        {
            WriteToBackingStore(msg);
        }
    }
}
