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
        public readonly Type RecordType;
        public event EventHandler<ChoRowsLoadedEventArgs> RowsLoaded;
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
        public event EventHandler<ChoRecordFieldTypeAssessmentEventArgs> RecordFieldTypeAssessment;
        public TraceSwitch TraceSwitch = ChoETLFramework.TraceSwitch;

        public ChoRecordReader(Type recordType)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            if (typeof(ICollection).IsAssignableFrom(recordType))
                throw new ChoReaderException("Invalid recordtype passed.");

            RecordType = recordType;
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
            if (membersDiscovered == null)
                return;
            var ea = new ChoEventArgs<IDictionary<string, Type>>(fieldTypes);
            membersDiscovered(this, ea);
        }

        protected void RaiseRecordFieldTypeAssessment(IDictionary<string, Type> fieldTypes, IDictionary<string, object> fieldValues, bool isLastScanRow = false)
        {
            EventHandler<ChoRecordFieldTypeAssessmentEventArgs> recordFieldTypeAssessment = RecordFieldTypeAssessment;
            if (recordFieldTypeAssessment == null)
            {
                OnRecordFieldTypeAssessment(fieldTypes, fieldValues, isLastScanRow);
                return;
            }
            var ea = new ChoRecordFieldTypeAssessmentEventArgs(fieldTypes, fieldValues, isLastScanRow);
            recordFieldTypeAssessment(this, ea);
        }

        protected virtual IDictionary<string, object> MigrateToNewSchema(IDictionary<string, object> rec, IDictionary<string, Type> recTypes)
        {
            IDictionary<string, object> newRec = new Dictionary<string, object>();
            foreach (var kvp in rec)
            {
                //newRec.ConvertNSetMemberValue(kvp.Key, kvp.Value, ref fieldValue, Configuration.Culture);
                newRec.Add(kvp.Key, kvp.Value.CastObjectTo(recTypes[kvp.Key]));
            }
            return rec;
        }

        protected virtual void OnRecordFieldTypeAssessment(IDictionary<string, Type> fieldTypes, IDictionary<string, object> fieldValues, bool isLastScanRow = false)
        {
            CultureInfo ci = Thread.CurrentThread.CurrentCulture;
            if (!isLastScanRow)
            {
                foreach (var key in fieldTypes.Keys.ToArray())
                {
                    if (!fieldValues.ContainsKey(key))
                        continue;

                    object value = fieldValues[key];
                    Type fieldType = null;
                    if (fieldTypes[key] == typeof(string))
                        continue;

                    if (value == null)
                    {
                        if (isLastScanRow)
                            fieldTypes[key] = typeof(string);
                        else
                            continue;
                    }

                    bool boolValue;
                    long lresult = 0;
                    double dresult = 0;
                    decimal decResult = 0;
                    DateTime dtResult;

                    if (ChoBoolean.TryParse(value.ToNString(), out boolValue))
                        fieldType = typeof(bool);
                    else if (!value.ToNString().Contains(ci.NumberFormat.NumberDecimalSeparator) && long.TryParse(value.ToNString(), out lresult))
                        fieldType = typeof(long);
                    else if (double.TryParse(value.ToNString(), out dresult))
                        fieldType = typeof(double);
                    else if (decimal.TryParse(value.ToNString(), out decResult))
                        fieldType = typeof(decimal);
                    else if (DateTime.TryParse(value.ToNString(), out dtResult))
                        fieldType = typeof(DateTime);
                    else
                        fieldType = typeof(string);

                    if (fieldType == typeof(string))
                        fieldTypes[key] = fieldType;
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
                    else
                        fieldType = typeof(string);
                }
            }
            else
            {
                foreach (var key in fieldTypes.Keys.ToArray())
                {
                    if (fieldTypes[key] == null)
                        fieldTypes[key] = typeof(string);
                }
            }
        }
        protected Type DiscoverFieldType(string value, ChoFileRecordConfiguration config)
        {
            bool treatCurrencyAsDecimal = config.TreatCurrencyAsDecimal;
            long lresult = 0;
            double dresult = 0;
            DateTime dtresult;
            Decimal decResult = 0;
            ChoCurrency currResult = 0;

            if (value == null)
                return typeof(string);
            else if (long.TryParse(value, out lresult))
                return typeof(long);
            else if (double.TryParse(value, out dresult))
                return typeof(double);
            else if (DateTime.TryParse(value, out dtresult))
                return typeof(DateTime);
            else if (DateTime.TryParse(value, out dtresult))
                return typeof(DateTime);
            else if (!treatCurrencyAsDecimal && ChoCurrency.TryParse(value, out currResult))
                return typeof(ChoCurrency);
            else if (treatCurrencyAsDecimal && Decimal.TryParse(value, NumberStyles.Currency, CultureInfo.CurrentCulture, out decResult))
                return typeof(Decimal);
            else
                return typeof(string);
        }

        protected object GetDeclaringRecord(string declaringMember, object rec)
        {
            if (declaringMember == null)
                return rec;

            return GetDeclaringRecord(rec, declaringMember);
        }

        protected static object GetDeclaringRecord(object src, string propName, bool leaf = true)
        {
            if (src == null) throw new ArgumentException("Value cannot be null.", "src");
            if (propName == null) throw new ArgumentException("Value cannot be null.", "propName");

            if (propName.Contains("."))//complex type nested
            {
                var temp = propName.Split(new char[] { '.' }, 2);
                return GetDeclaringRecord(GetDeclaringRecord(src, temp[0], false), temp[1]);
            }
            else
            {
                var prop = src.GetType().GetProperty(propName);
                if (!leaf && prop != null)
                {
                    var obj = prop.GetValue(src, null);
                    if (obj == null)
                    {
                        obj = Activator.CreateInstance(prop.PropertyType);
                        prop.SetValue(src, obj);
                    }
                    return obj;
                }
                else
                    return src;
            }
        }
    }
}
