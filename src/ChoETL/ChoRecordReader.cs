using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordReader : IChoDeferedObjectMemberDiscoverer
    {
        public virtual Type RecordType { get; set; }
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;

        public abstract ChoRecordConfiguration RecordConfiguration
        {
            get;
        }

        public ChoRecordReader(Type recordType, bool force = true)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            if (force)
            {
                if (!recordType.IsDynamicType() && typeof(ICollection).IsAssignableFrom(recordType))
                    throw new ChoReaderException("Invalid recordtype passed.");
            }

            RecordType = recordType;
        }

        protected void InitializeRecordConfiguration(ChoRecordConfiguration configuration)
        {
            if (configuration == null || configuration.IsDynamicObject || configuration.RecordType == null)
                return;

            if (!typeof(IChoNotifyRecordConfigurable).IsAssignableFrom(configuration.RecordMapType))
                return;

            var obj = ChoActivator.CreateInstance(configuration.RecordMapType) as IChoNotifyRecordConfigurable;
            if (obj != null)
                obj.RecondConfigure(configuration);
        }

        protected void InitializeRecordFieldConfiguration(ChoRecordConfiguration configuration)
        {
            if (configuration == null || configuration.IsDynamicObject || configuration.RecordType == null)
                return;

            if (!typeof(IChoNotifyRecordFieldConfigurable).IsAssignableFrom(configuration.RecordMapType))
                return;

            var obj = ChoActivator.CreateInstance(configuration.RecordMapType) as IChoNotifyRecordFieldConfigurable;
            if (obj == null)
                return;

            foreach (var fc in configuration.RecordFieldConfigurations)
                obj.RecondFieldConfigure(fc);
        }

        protected bool RaisedRowsLoaded(long rowsLoaded, bool isFinal = false)
        {
            EventHandler<ChoRowsLoadedEventArgs> rowsLoadedEvent = RowsLoaded;
            if (rowsLoadedEvent == null)
                return false;

            var ea = new ChoRowsLoadedEventArgs(rowsLoaded, isFinal);
            rowsLoadedEvent(this, ea);
            return ea.Abort;
        }

        public abstract IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null);
        //public abstract void LoadSchema(object source);

        protected void RaiseMembersDiscovered(IDictionary<string, Type> fieldTypes)
        {
            EventHandler<ChoEventArgs<IDictionary<string, Type>>> membersDiscovered = MembersDiscovered;
            if (membersDiscovered != null)
            {
                var ea = new ChoEventArgs<IDictionary<string, Type>>(fieldTypes);
                membersDiscovered(this, ea);
            }
            InitializeRecordFieldConfiguration(RecordConfiguration);
        }

        protected void RaiseRecordFieldTypeAssessment(IDictionary<string, Type> fieldTypes, IDictionary<string, object> fieldValues, bool isLastScanRow = false)
        {
            EventHandler<ChoRecordFieldTypeAssessmentEventArgs> recordFieldTypeAssessment = RecordFieldTypeAssessment;
            OnRecordFieldTypeAssessment(fieldTypes, fieldValues, isLastScanRow);
            if (recordFieldTypeAssessment != null)
            {
                var ea = new ChoRecordFieldTypeAssessmentEventArgs(fieldTypes, fieldValues, isLastScanRow);
                recordFieldTypeAssessment(this, ea);
            }
        }

        protected virtual IDictionary<string, object> MigrateToNewSchema(IDictionary<string, object> rec, IDictionary<string, Type> recTypes)
        {
            IDictionary<string, object> newRec = new Dictionary<string, object>();
            foreach (var kvp in rec)
            {
                //newRec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                if (recTypes.ContainsKey(kvp.Key))
                    newRec.Add(kvp.Key, kvp.Value.CastObjectTo(recTypes[kvp.Key]));
                else
                    newRec.Add(kvp.Key, typeof(ChoDynamicObject));

                if (kvp.Value == null)
                {
                    var dobj = newRec as ChoDynamicObject;
                    dobj.SetMemberType(kvp.Key, recTypes[kvp.Key]);
                }
            }
            return newRec;
        }

        protected virtual void OnRecordFieldTypeAssessment(IDictionary<string, Type> fieldTypes, IDictionary<string, object> fieldValues, bool isLastScanRow = false)
        {
            CultureInfo ci = Thread.CurrentThread.CurrentCulture;
            foreach (var key in fieldTypes.Keys.ToArray())
            {
                if (!fieldValues.ContainsKey(key))
                    continue;

                object value = fieldValues[key];
                Type fieldType = null;
                if (fieldTypes[key] == typeof(string))
                    continue;

                if (value == null
                    || (value is string && ((string)value).IsNullOrWhiteSpace()))
                {
                    //if (isLastScanRow)
                    //    fieldTypes[key] = typeof(string);
                    //else
                        continue;
                }
                else if (!(value is string))
                {
                    fieldTypes[key] = value.GetType();
                    continue;
                }

                bool boolValue;
                long lresult = 0;
                double dresult = 0;
                decimal decResult = 0;
                DateTime dtResult;
                ChoCurrency currResult = 0;
                Guid guidResult;
                bool treatCurrencyAsDecimal = RecordConfiguration.TypeConverterFormatSpec.TreatCurrencyAsDecimal;

                if (ChoBoolean.TryParse(value.ToNString(), out boolValue))
                    fieldType = typeof(bool);
                else if (Guid.TryParse(value.ToNString(), out guidResult))
                    fieldType = typeof(Guid);
                else if (!value.ToNString().Contains(ci.NumberFormat.NumberDecimalSeparator) && long.TryParse(value.ToNString(), out lresult))
                    fieldType = typeof(long);
                else if (double.TryParse(value.ToNString(), out dresult))
                    fieldType = typeof(double);
                else if (decimal.TryParse(value.ToNString(), out decResult))
                    fieldType = typeof(decimal);
                else if (!treatCurrencyAsDecimal && ChoCurrency.TryParse(value.ToNString(), out currResult))
                    fieldType = typeof(ChoCurrency);
                else if (treatCurrencyAsDecimal && Decimal.TryParse(value.ToNString(), NumberStyles.Currency, CultureInfo.CurrentCulture, out decResult))
                    fieldType = typeof(Decimal);
                else if (!RecordConfiguration.TypeConverterFormatSpec.DateTimeFormat.IsNullOrWhiteSpace()
                        && ChoDateTime.TryParseExact(value.ToNString(), RecordConfiguration.TypeConverterFormatSpec.DateTimeFormat, CultureInfo.CurrentCulture, out dtResult))
                    fieldType = typeof(DateTime);
                else if (DateTime.TryParse(value.ToNString(), out dtResult))
                    fieldType = typeof(DateTime);
                else
                {
                    if (value.ToNString().Length == 1)
                        fieldType = typeof(char);
                    else
                        fieldType = typeof(string);
                }

                if (fieldType == typeof(string))
                    fieldTypes[key] = fieldType;
                else if (fieldType == typeof(ChoCurrency))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                    else if (fieldTypes[key] != typeof(ChoCurrency))
                        fieldTypes[key] = typeof(string);
                }
                else if (fieldType == typeof(Guid))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                    else if (fieldTypes[key] != typeof(Guid))
                        fieldTypes[key] = typeof(string);
                }
                else if (fieldType == typeof(DateTime))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                    else if (fieldTypes[key] != typeof(DateTime))
                        fieldTypes[key] = typeof(string);
                }
                else if (fieldType == typeof(decimal))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                    else if (fieldTypes[key] == typeof(DateTime))
                        fieldTypes[key] = typeof(string);
                    else if (fieldTypes[key] == typeof(double)
                        || fieldTypes[key] == typeof(long)
                        || fieldTypes[key] == typeof(bool))
                        fieldTypes[key] = fieldType;
                }
                else if (fieldType == typeof(double))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                    else if (fieldTypes[key] == typeof(DateTime))
                        fieldTypes[key] = typeof(string);
                    else if (fieldTypes[key] == typeof(long)
                        || fieldTypes[key] == typeof(bool))
                        fieldTypes[key] = fieldType;
                }
                else if (fieldType == typeof(long))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                    else if (fieldTypes[key] == typeof(DateTime))
                        fieldTypes[key] = typeof(string);
                    else if (fieldTypes[key] == typeof(bool))
                        fieldTypes[key] = fieldType;
                }
                else if (fieldType == typeof(bool))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                }
                else if (fieldType == typeof(char))
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = fieldType;
                }
                else
                    fieldType = typeof(string);
            }
            if (isLastScanRow)
            {
                foreach (var key in fieldTypes.Keys.ToArray())
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = typeof(string);
                }
            }
        }

        //protected Type DiscoverFieldType(string value, ChoFileRecordConfiguration config)
        //{
        //    bool treatCurrencyAsDecimal = config.TreatCurrencyAsDecimal;
        //    long lresult = 0;
        //    double dresult = 0;
        //    DateTime dtresult;
        //    Decimal decResult = 0;
        //    ChoCurrency currResult = 0;
        //    Guid guidResult;

        //    if (value == null)
        //        return typeof(string);
        //    else if (long.TryParse(value, out lresult))
        //        return typeof(long);
        //    else if (double.TryParse(value, out dresult))
        //        return typeof(double);
        //    else if (RecordConfiguration.TypeConverterFormatSpec.DateTimeFormat.IsNullOrWhiteSpace()
        //        && ChoDateTime.TryParseExact(value, RecordConfiguration.TypeConverterFormatSpec.DateTimeFormat, CultureInfo.CurrentCulture, out dtResult))
        //        return typeof(DateTime);
        //    else if (DateTime.TryParse(value, out dtresult))
        //        return typeof(DateTime);
        //    else if (Guid.TryParse(value, out guidResult))
        //        return typeof(Guid);
        //    else if (!treatCurrencyAsDecimal && ChoCurrency.TryParse(value, out currResult))
        //        return typeof(ChoCurrency);
        //    else if (treatCurrencyAsDecimal && Decimal.TryParse(value, NumberStyles.Currency, CultureInfo.CurrentCulture, out decResult))
        //        return typeof(Decimal);
        //    else
        //        return typeof(string);
        //}

        protected object GetDeclaringRecord(string declaringMember, object rec, ChoFileRecordFieldConfiguration config = null)
        {
            if (rec == null)
                return null;

            var obj = ChoType.GetDeclaringRecord(declaringMember, rec);
            if (obj == null)
                return null;

            Type recordType = obj.GetType();

            if (config != null)
            {
                if (config.ArrayIndex != null 
                    && config.ArrayIndex.Value >= 0 
                    && obj is IEnumerable
                    && !(obj is ArrayList))
                {
                    var item = Enumerable.Skip(((IEnumerable)obj).Cast<object>(), config.ArrayIndex.Value).FirstOrDefault();

                    if (item == null)
                    {
                        Type itemType = obj.GetType().GetItemType();
                        item = ChoActivator.CreateInstance(itemType);
                    }

                    if (obj is Array)
                    {
                        if (config.ArrayIndex.Value < ((Array)obj).Length)
                            ((Array)obj).SetValue(item, config.ArrayIndex.Value);
                    }
                    else if (obj is IList)
                        ((IList)obj).Add(item);

                    return item;
                }
            }

            return obj;
        }
    }
}
