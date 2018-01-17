using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoCustomColumnMappable
    {
        bool MapColumn(int colPos, string colName, out string newColName);
    }
}