using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoManifoldRecordReader : ChoRecordReader
    {
        private string[] _fieldNames = new string[] { };
        private bool _configCheckDone = false;

        public ChoManifoldRecordConfiguration Configuration
        {
            get;
            private set;
        }

        public ChoManifoldRecordReader(ChoManifoldRecordConfiguration configuration) : base(typeof(object))
        {
            ChoGuard.ArgumentNotNull(configuration, "Configuration");
            Configuration = configuration;
            //Configuration.Validate();
        }

        public override void LoadSchema(object source)
        {
            var e = AsEnumerable(source, ChoETLFramework.TraceSwitchOff).GetEnumerator();
            e.MoveNext();
        }

        public override IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null)
        {
            return AsEnumerable(source, ChoETLFramework.TraceSwitch, filterFunc);
        }

        private IEnumerable<object> AsEnumerable(object source, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            StreamReader sr = source as StreamReader;
            ChoGuard.ArgumentNotNull(sr, "StreamReader");

            sr.Seek(0, SeekOrigin.Begin);

            if (!RaiseBeginLoad(sr))
                yield break;

            string[] commentTokens = Configuration.Comments;
            bool _headerFound = false;
            using (ChoPeekEnumerator<Tuple<int, string>> e = new ChoPeekEnumerator<Tuple<int, string>>(
                new ChoIndexedEnumerator<string>(sr.ReadLines(Configuration.EOLDelimiter, Configuration.QuoteChar)).ToEnumerable(),
                (pair) =>
                {
                    //bool isStateAvail = IsStateAvail();

                    bool? skip = false;

                    //if (isStateAvail)
                    //{
                    //    if (!IsStateMatches(item))
                    //    {
                    //        skip = filterFunc != null ? filterFunc(item) : false;
                    //    }
                    //    else
                    //        skip = true;
                    //}
                    //else
                    //    skip = filterFunc != null ? filterFunc(item) : false;

                    if (skip == null)
                        return null;

                    //if (!(sr.BaseStream is MemoryStream))
                    //{
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, Environment.NewLine);

                        if (!skip.Value)
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading line [{0}]...".FormatString(pair.Item1));
                        else
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping line [{0}]...".FormatString(pair.Item1));
                    //}

                    if (skip.Value)
                        return skip;

                    //if (!(sr.BaseStream is MemoryStream))
                    //    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, ChoETLFramework.Switch.TraceVerbose, "Loading line [{0}]...".FormatString(item.Item1));

                    //if (Task != null)
                    //    return !IsStateNOTExistsOrNOTMatch(item);

                    if (pair.Item2.IsNullOrWhiteSpace())
                    {
                        if (!Configuration.IgnoreEmptyLine)
                            throw new ChoParserException("Empty line found at {0} location.".FormatString(e.Peek.Item1));
                        else
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Empty line found at [{0}]...".FormatString(pair.Item1));
                            return true;
                        }
                    }

                    if (commentTokens == null)
                        return false;
                    else
                    {
                        var x = (from comment in commentTokens
                                 where !pair.Item2.IsNull() && pair.Item2.StartsWith(comment, true, Configuration.Culture)
                                 select comment).FirstOrDefault();
                        if (x != null)
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Comment line found at [{0}]...".FormatString(pair.Item1));
                            return true;
                        }
                    }

                    if (!_configCheckDone)
                    {
                        Configuration.Validate(pair); // GetHeaders(pair.Item2));
                        _configCheckDone = true;
                    }

                    //Ignore Header if any
                    if (Configuration.FileHeaderConfiguration.HasHeaderRecord
                        && !_headerFound)
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring header line at [{0}]...".FormatString(pair.Item1));
                        _headerFound = true;
                        return true;
                    }

                    return false;
                }))
            {
                while (true)
                {
                    Tuple<int, string> pair = e.Peek;
                    if (pair == null)
                    {
                        RaiseEndLoad(sr);
                        yield break;
                    }

                    Type recType = Configuration.RecordSelector(pair.Item2);
                    if (recType == null)
                    {
                        if (Configuration.IgnoreIfNoRecordParserExists)
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, $"No record type found for [{pair.Item1}] line to parse.");
                        else
                            throw new ChoParserException($"No record type found for [{pair.Item1}] line to parse.");
                    }

                    object rec = ChoActivator.CreateInstance(recType);
                    if (!LoadLine(pair, ref rec))
                        yield break;

                    //StoreState(e.Current, rec != null);

                    e.MoveNext();

                    if (rec == null)
                        continue;

                    yield return rec;
                }
            }
        }

        private ChoFileRecordConfiguration GetConfiguration(Type recordType)
        {
            ChoFileRecordConfiguration config = Configuration[recordType];
            if (config == null)
            {
                ChoFileRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoFileRecordObjectAttribute>(recordType);
                if (recObjAttr == null || recObjAttr is ChoCSVRecordObjectAttribute)
                    Configuration[recordType] = new ChoCSVRecordConfiguration(recordType);
                else if (recObjAttr is ChoFixedLengthRecordObjectAttribute)
                    Configuration[recordType] = new ChoFixedLengthRecordConfiguration(recordType);
            }

            return Configuration[recordType];
        }

        private bool LoadLine(Tuple<int, string> pair, ref object rec)
        {
            try
            {
                if (!RaiseBeforeRecordLoad(rec, ref pair))
                    return false;

                if (pair.Item2 == null)
                {
                    rec = null;
                    return true;
                }
                else if (pair.Item2 == String.Empty)
                    return true;

                if (!pair.Item2.IsNullOrWhiteSpace())
                {
                    var config = GetConfiguration(rec.GetType());
                    if (config == null)
                        throw new ChoParserException("No parser found to parse record line.");

                    if (config.GetType() == typeof(ChoCSVRecordConfiguration))
                    {
                        var r = ChoCSVReader.LoadText(rec.GetType(), pair.Item2, config as ChoCSVRecordConfiguration, Configuration.Encoding, Configuration.BufferSize);
                        rec = r.FirstOrDefault<object>();
                    }
                    else if (config.GetType() == typeof(ChoFixedLengthRecordConfiguration))
                    {
                        var r = ChoFixedLengthReader.LoadText(rec.GetType(), pair.Item2, config as ChoFixedLengthRecordConfiguration, Configuration.Encoding, Configuration.BufferSize);
                        rec = r.FirstOrDefault<object>();
                    }
                    else
                        throw new ChoParserException("Unsupported record line found to parse.");
                }

                if (!RaiseAfterRecordLoad(rec, pair))
                    return false;
            }
            catch (ChoParserException)
            {
                throw;
            }
            catch (ChoMissingRecordFieldException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ChoETLFramework.HandleException(ex);
                if (Configuration.ErrorMode == ChoErrorMode.IgnoreAndContinue)
                {
                    rec = null;
                }
                else if (Configuration.ErrorMode == ChoErrorMode.ReportAndContinue)
                {
                    if (!RaiseRecordLoadError(rec, pair, ex))
                        throw;
                }
                else
                    throw;

                return true;
            }

            return true;
        }

        private bool RaiseBeginLoad(object state)
        {
            if (Configuration.CallbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.CallbackRecord.BeginLoad(state), true);
        }

        private void RaiseEndLoad(object state)
        {
            if (Configuration.CallbackRecord == null) return;
            ChoActionEx.RunWithIgnoreError(() => Configuration.CallbackRecord.EndLoad(state));
        }

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<int, string> pair)
        {
            if (Configuration.CallbackRecord == null) return true;
            int index = pair.Item1;
            object state = pair.Item2;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.CallbackRecord.BeforeRecordLoad(target, index, ref state), true);

            if (retValue)
                pair = new Tuple<int, string>(index, state as string);

            return retValue;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<int, string> pair)
        {
            if (Configuration.CallbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.CallbackRecord.AfterRecordLoad(target, pair.Item1, pair.Item2), true);
        }

        private bool RaiseRecordLoadError(object target, Tuple<int, string> pair, Exception ex)
        {
            if (Configuration.CallbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.CallbackRecord.RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
        }

        private bool RaiseBeforeRecordFieldLoad(object target, int index, string propName, ref object value)
        {
            if (Configuration.CallbackRecord == null) return true;
            object state = value;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.CallbackRecord.BeforeRecordFieldLoad(target, index, propName, ref state), true);

            if (retValue)
                value = state;

            return retValue;
        }

        private bool RaiseAfterRecordFieldLoad(object target, int index, string propName, object value)
        {
            if (Configuration.CallbackRecord == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.CallbackRecord.AfterRecordFieldLoad(target, index, propName, value), true);
        }

        private bool RaiseRecordFieldLoadError(object target, int index, string propName, object value, Exception ex)
        {
            if (Configuration.CallbackRecord == null) return false;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.CallbackRecord.RecordFieldLoadError(target, index, propName, value, ex), false);
        }
    }
}
