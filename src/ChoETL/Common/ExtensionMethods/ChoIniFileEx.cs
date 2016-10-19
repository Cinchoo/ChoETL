using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoIniFileEx
    {
        public static ChoIniFile OpenRecordParamsSection(this ChoIniFile @this)
        {
            ChoGuard.ArgumentNotNull(@this, "IniFile");
            return @this.GetSection("RECORD_PARAMS");
        }

        public static ChoIniFile OpenDbRecordParamsSection(this ChoIniFile @this)
        {
            ChoGuard.ArgumentNotNull(@this, "IniFile");
            return @this.GetSection("DB_RECORD_PARAMS");
        }

        public static ChoIniFile OpenTextRecordParamsSection(this ChoIniFile @this)
        {
            ChoGuard.ArgumentNotNull(@this, "IniFile");
            return @this.GetSection("TEXT_RECORD_PARAMS");
        }

        public static ChoIniFile OpenMapReduceParamsSection(this ChoIniFile @this)
        {
            ChoGuard.ArgumentNotNull(@this, "IniFile");
            return @this.GetSection("MAP_REDUCE_PARAMS");
        }

        public static string GetErrorMode(this ChoIniFile @this)
        {
            return @this.OpenRecordParamsSection().GetValue<string>("ErrorMode");
        }

        public static void SetErrorMode(this ChoIniFile @this, string errorMode)
        {
            @this.OpenRecordParamsSection().SetValue("ErrorMode", errorMode);
        }

        public static string GetFieldNames(this ChoIniFile @this)
        {
            return @this.OpenRecordParamsSection().GetValue<string>("FieldNames");
        }

        public static void SetFieldNames(this ChoIniFile @this, string fieldNames)
        {
            @this.OpenRecordParamsSection().SetValue("FieldNames", fieldNames);
        }

        public static int GetRecordLength(this ChoIniFile @this)
        {
            return @this.OpenTextRecordParamsSection().GetValue<int>("RecordLength");
        }

        public static void SetRecordLength(this ChoIniFile @this, int length)
        {
            @this.OpenTextRecordParamsSection().SetValue("RecordLength", length.ToString());
        }

        public static string GetEOLDelimiter(this ChoIniFile @this)
        {
            return @this.OpenTextRecordParamsSection().GetValue<string>("EOLDelimiter");
        }

        public static void SetEOLDelimiter(this ChoIniFile @this, string delimiter)
        {
            @this.OpenTextRecordParamsSection().SetValue("EOLDelimiter", delimiter);
        }

        public static bool GetIsFirstLineHeader(this ChoIniFile @this)
        {
            return @this.OpenTextRecordParamsSection().GetValue<bool>("IsFirstLineHeader");
        }

        public static void SetIsFirstLineHeader(this ChoIniFile @this, bool isFirstLineHeader)
        {
            @this.OpenTextRecordParamsSection().SetValue("IsFirstLineHeader", isFirstLineHeader.ToString());
        }

        public static string GetDelimiter(this ChoIniFile @this)
        {
            return @this.OpenTextRecordParamsSection().GetValue<string>("Delimiter");
        }

        public static void SetDelimiter(this ChoIniFile @this, string delimiter)
        {
            @this.OpenTextRecordParamsSection().SetValue("Delimiter", delimiter);
        }

        public static ChoIniFile OpenFixedLengthRecordFieldSection(this ChoIniFile @this)
        {
            ChoGuard.ArgumentNotNull(@this, "IniFile");
            return @this.GetSection("FIXED_LENGTH_RECORD_FIELD");
        }

        public static string GetFixedLengthRecordField(this ChoIniFile @this, string fieldName)
        {
            ChoGuard.ArgumentNotNullOrEmpty(fieldName, "FieldName");
            fieldName = fieldName.Trim();
            return @this.OpenFixedLengthRecordFieldSection().GetValue<string>(fieldName);
        }

        public static void SetFixedLengthRecordField(this ChoIniFile @this, string fieldName, string value)
        {
            ChoGuard.ArgumentNotNullOrEmpty(fieldName, "FieldName");
            fieldName = fieldName.Trim();

            @this.OpenFixedLengthRecordFieldSection().SetValue(fieldName, value);
        }
    }
}
