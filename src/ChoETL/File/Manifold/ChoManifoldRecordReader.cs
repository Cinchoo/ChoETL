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
            return AsEnumerable(source, TraceSwitch, filterFunc);
        }

        private IEnumerable<object> AsEnumerable(object source, TraceSwitch traceSwitch, Func<object, bool?> filterFunc = null)
        {
            TraceSwitch = traceSwitch;

            StreamReader sr = source as StreamReader;
            ChoGuard.ArgumentNotNull(sr, "StreamReader");

            if (Configuration.RecordSelector == null)
                throw new ChoRecordConfigurationException("Missing record selector.");

            sr.Seek(0, SeekOrigin.Begin);

            if (!RaiseBeginLoad(sr))
                yield break;

            string[] commentTokens = Configuration.Comments;
            bool? skip = false;
            bool _headerFound = false;
            using (ChoPeekEnumerator<Tuple<long, string>> e = new ChoPeekEnumerator<Tuple<long, string>>(
                new ChoIndexedEnumerator<string>(sr.ReadLines(Configuration.EOLDelimiter, Configuration.QuoteChar, Configuration.MayContainEOLInData)).ToEnumerable(),
                (pair) =>
                {
                    //bool isStateAvail = IsStateAvail();

                    skip = false;

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


                    if (TraceSwitch.TraceVerbose)
                    {
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, Environment.NewLine);

                        if (!skip.Value)
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Loading line [{0}]...".FormatString(pair.Item1));
                        else
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Skipping line [{0}]...".FormatString(pair.Item1));
                    }

                    if (skip.Value)
                        return skip;

                    //if (!(sr.BaseStream is MemoryStream))
                    //    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, ChoETLFramework.Switch.TraceVerbose, "Loading line [{0}]...".FormatString(item.Item1));

                    //if (Task != null)
                    //    return !IsStateNOTExistsOrNOTMatch(item);

                    if (pair.Item2.IsNullOrWhiteSpace())
                    {
                        if (!Configuration.IgnoreEmptyLine)
                            throw new ChoParserException("Empty line found at [{0}] location.".FormatString(pair.Item1));
                        else
                        {
                            if (TraceSwitch.TraceVerbose)
                                ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring empty line found at [{0}].".FormatString(pair.Item1));
                            return true;
                        }
                    }

                    if (commentTokens != null && commentTokens.Length > 0)
                    {
                        foreach (string comment in commentTokens)
                        {
                            if (!pair.Item2.IsNull() && pair.Item2.StartsWith(comment, StringComparison.Ordinal)) //, true, Configuration.Culture))
                            {
                                if (TraceSwitch.TraceVerbose)
                                    ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Comment line found at [{0}]...".FormatString(pair.Item1));
                                return true;
                            }
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
                        if (TraceSwitch.TraceVerbose)
                        ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Ignoring header line at [{0}]...".FormatString(pair.Item1));
                        _headerFound = true;
                        return true;
                    }

                    return false;
                }))
            {
                while (true)
                {
                    Tuple<long, string> pair = e.Peek;
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

                    object rec = Activator.CreateInstance(recType);
                    if (!LoadLine(pair, ref rec))
                        yield break;

                    //StoreState(e.Current, rec != null);

                    e.MoveNext();

                    if (rec == null)
                        continue;

                    yield return rec;
                    if (Configuration.NotifyAfter > 0 && pair.Item1 % Configuration.NotifyAfter == 0)
                    {
                        if (RaisedRowsLoaded(pair.Item1))
                        {
                            ChoETLFramework.WriteLog(TraceSwitch.TraceVerbose, "Abort requested.");
                            yield break;
                        }
                    }
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

        private bool LoadLine(Tuple<long, string> pair, ref object rec)
        {
            Type recType = rec.GetType();
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
            catch (ChoParserException pEx)
            {
                throw new ChoParserException($"Failed to parse line to '{recType}' object.", pEx);
            }
            catch (ChoMissingRecordFieldException mEx)
            {
                throw new ChoParserException($"Failed to parse line to '{recType}' object.", mEx);
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
                        throw new ChoReaderException($"Failed to parse line to '{recType}' object.", ex);
                }
                else
                    throw new ChoReaderException($"Failed to parse line to '{recType}' object.", ex);

                return true;
            }

            return true;
        }

        private bool RaiseBeginLoad(object state)
        {
            if (Configuration.NotifyRecordReadObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.BeginLoad(state), true);
        }

        private void RaiseEndLoad(object state)
        {
            if (Configuration.NotifyRecordReadObject == null) return;
            ChoActionEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.EndLoad(state));
        }

        private bool RaiseBeforeRecordLoad(object target, ref Tuple<long, string> pair)
        {
            if (Configuration.NotifyRecordReadObject == null) return true;
            long index = pair.Item1;
            object state = pair.Item2;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.BeforeRecordLoad(target, index, ref state), true);

            if (retValue)
                pair = new Tuple<long, string>(index, state as string);

            return retValue;
        }

        private bool RaiseAfterRecordLoad(object target, Tuple<long, string> pair)
        {
            if (Configuration.NotifyRecordReadObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.AfterRecordLoad(target, pair.Item1, pair.Item2), true);
        }

        private bool RaiseRecordLoadError(object target, Tuple<long, string> pair, Exception ex)
        {
            if (Configuration.NotifyRecordReadObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.RecordLoadError(target, pair.Item1, pair.Item2, ex), false);
        }

        private bool RaiseBeforeRecordFieldLoad(object target, int index, string propName, ref object value)
        {
            if (Configuration.NotifyRecordReadObject == null) return true;
            object state = value;
            bool retValue = ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.BeforeRecordFieldLoad(target, index, propName, ref state), true);

            if (retValue)
                value = state;

            return retValue;
        }

        private bool RaiseAfterRecordFieldLoad(object target, int index, string propName, object value)
        {
            if (Configuration.NotifyRecordReadObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.AfterRecordFieldLoad(target, index, propName, value), true);
        }

        private bool RaiseRecordFieldLoadError(object target, int index, string propName, object value, Exception ex)
        {
            if (Configuration.NotifyRecordReadObject == null) return true;
            return ChoFuncEx.RunWithIgnoreError(() => Configuration.NotifyRecordReadObject.RecordFieldLoadError(target, index, propName, value, ex), true);
        }
    }
}
