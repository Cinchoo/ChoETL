using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class ChoQuoteFieldAttribute : Attribute
    {
        public bool QuoteField { get; private set; }

        public ChoQuoteFieldAttribute(bool flag = true)
        {
            QuoteField = flag;
        }
    }
}
