using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoCSVFluentPlugIn : IDisposable, IEnumerable
    {
        ChoCSVRecordConfiguration Configuration
        {
            get;
        }
    }

    public static class ChoCSVFluentPlugIn
    {
        #region Fluent API

        public static IChoCSVFluentPlugIn WithDelimiter(this IChoCSVFluentPlugIn plugIn, string delimiter)
        {
            plugIn.Configuration.Delimiter = delimiter;
            return plugIn;
        }

        public static IChoCSVFluentPlugIn WithFirstLineHeader(this IChoCSVFluentPlugIn plugIn, bool flag = true)
        {
            plugIn.Configuration.FileHeaderConfiguration.HasHeaderRecord = flag;
            return plugIn;
        }

        public static IChoCSVFluentPlugIn WithFields(this IChoCSVFluentPlugIn plugIn, params string[] fieldsNames)
        {
            plugIn.Configuration.CSVRecordFieldConfigurations.Clear();
            if (!fieldsNames.IsNullOrEmpty())
            {
                int maxFieldPos = plugIn.Configuration.CSVRecordFieldConfigurations.Count > 0 ? plugIn.Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                foreach (string fn in fieldsNames)
                {
                    if (fn.IsNullOrEmpty())
                        continue;

                    plugIn.Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(fn.Trim(), ++maxFieldPos));
                }

            }

            return plugIn;
        }

        public static IChoCSVFluentPlugIn WithField(this IChoCSVFluentPlugIn plugIn, string fieldsName, Type fieldType = null)
        {
            if (!fieldsName.IsNullOrEmpty())
            {
                int maxFieldPos = plugIn.Configuration.CSVRecordFieldConfigurations.Count > 0 ? plugIn.Configuration.CSVRecordFieldConfigurations.Max(f => f.FieldPosition) : 0;
                plugIn.Configuration.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration(fieldsName.Trim(), ++maxFieldPos) { FieldType = fieldType });
            }

            return plugIn;
        }

        #endregion Fluent API
    }
}
