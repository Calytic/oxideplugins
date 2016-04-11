/*
// Cron Library - Cron Library for Oxide Mod ()
// Copyright (c) 2008 MÃ¼nir Ozan TOPCU. All rights reserved.
//
//  Author(s):
//
//      MÃ¼nir Ozan TOPCU, feramor@computer.org
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
*/

/*NCrontab
    Copyright 2008 Atif Aziz 

    This product includes software developed by
    Atif Aziz (https://github.com/atifaziz/NCrontab).
*/

/* Cron Job Syntax

 # ââââââââââââââ min (0 - 59) 
 # â âââââââââââââââ hour (0 - 23)
 # â â ââââââââââââââââ day of month (1 - 31)
 # â â â âââââââââââââââââ month (1 - 12)
 # â â â â ââââââââââââââââââ day of week (0 - 6) (0 to 6 are Sunday to Saturday, or use names; 7 is Sunday, the same as 0)
 # â â â â â
 # â â â â â
 # * * * * *

*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

//Oxide
using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries;

//UnityEngine
using UnityEngine;

//Others
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Cron Library", "Feramor", "1.0.2", ResourceId = 1754)]
    [Description("Cron Job Library for Oxide.")]
    public class CronLibrary : RustPlugin
    {
        public static class NCrontab
        {
            /*
            // NCrontab - Crontab for .NET
            // Copyright (c) 2008 Atif Aziz. All rights reserved.
            //
            //  Author(s):
            //
            //      Atif Aziz, http://www.raboof.com
            //
            // Licensed under the Apache License, Version 2.0 (the "License");
            // you may not use this file except in compliance with the License.
            // You may obtain a copy of the License at
            //
            //     http://www.apache.org/licenses/LICENSE-2.0
            //
            // Unless required by applicable law or agreed to in writing, software
            // distributed under the License is distributed on an "AS IS" BASIS,
            // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
            // See the License for the specific language governing permissions and
            // limitations under the License.
            */

            public delegate ExceptionProvider CrontabFieldAccumulator(int start, int end, int interval, ExceptionHandler onError);
            public delegate void ExceptionHandler(Exception e);
            public delegate Exception ExceptionProvider();

            [Serializable]
            public class CrontabException : Exception
            {
                public CrontabException() :
                    base("Crontab error.")
                { } // TODO: Fix message and add it to resource.

                public CrontabException(string message) :
                    base(message)
                { }

                public CrontabException(string message, Exception innerException) :
                    base(message, innerException)
                { }

                protected CrontabException(SerializationInfo info, StreamingContext context) :
                    base(info, context)
                { }
            }
            [Serializable]
            public sealed class CrontabField : ICrontabField
            {
                private readonly BitArray _bits;
                private /* readonly */ int _minValueSet;
                private /* readonly */ int _maxValueSet;
                private readonly CrontabFieldImpl _impl;

                /// <summary>
                /// Parses a crontab field expression given its kind.
                /// </summary>

                public static CrontabField Parse(CrontabFieldKind kind, string expression)
                {
                    return TryParse(kind, expression, ErrorHandling.Throw).Value;
                }

                public static ValueOrError<CrontabField> TryParse(CrontabFieldKind kind, string expression)
                {
                    return TryParse(kind, expression, null);
                }

                public static ValueOrError<CrontabField> TryParse(CrontabFieldKind kind, string expression, ExceptionHandler onError)
                {
                    var field = new CrontabField(CrontabFieldImpl.FromKind(kind));
                    var error = field._impl.TryParse(expression, field.Accumulate, onError);
                    return error == null ? field : (ValueOrError<CrontabField>)error;
                }

                /// <summary>
                /// Parses a crontab field expression representing minutes.
                /// </summary>

                public static CrontabField Minutes(string expression)
                {
                    return Parse(CrontabFieldKind.Minute, expression);
                }

                /// <summary>
                /// Parses a crontab field expression representing hours.
                /// </summary>

                public static CrontabField Hours(string expression)
                {
                    return Parse(CrontabFieldKind.Hour, expression);
                }

                /// <summary>
                /// Parses a crontab field expression representing days in any given month.
                /// </summary>

                public static CrontabField Days(string expression)
                {
                    return Parse(CrontabFieldKind.Day, expression);
                }

                /// <summary>
                /// Parses a crontab field expression representing months.
                /// </summary>

                public static CrontabField Months(string expression)
                {
                    return Parse(CrontabFieldKind.Month, expression);
                }

                /// <summary>
                /// Parses a crontab field expression representing days of a week.
                /// </summary>

                public static CrontabField DaysOfWeek(string expression)
                {
                    return Parse(CrontabFieldKind.DayOfWeek, expression);
                }

                private CrontabField(CrontabFieldImpl impl)
                {
                    if (impl == null)
                        throw new ArgumentNullException("impl");

                    _impl = impl;
                    _bits = new BitArray(impl.ValueCount);

                    _bits.SetAll(false);
                    _minValueSet = int.MaxValue;
                    _maxValueSet = -1;
                }

                /// <summary>
                /// Gets the first value of the field or -1.
                /// </summary>

                public int GetFirst()
                {
                    return _minValueSet < int.MaxValue ? _minValueSet : -1;
                }

                /// <summary>
                /// Gets the next value of the field that occurs after the given 
                /// start value or -1 if there is no next value available.
                /// </summary>

                public int Next(int start)
                {
                    if (start < _minValueSet)
                        return _minValueSet;

                    var startIndex = ValueToIndex(start);
                    var lastIndex = ValueToIndex(_maxValueSet);

                    for (var i = startIndex; i <= lastIndex; i++)
                    {
                        if (_bits[i])
                            return IndexToValue(i);
                    }

                    return -1;
                }

                private int IndexToValue(int index)
                {
                    return index + _impl.MinValue;
                }

                private int ValueToIndex(int value)
                {
                    return value - _impl.MinValue;
                }

                /// <summary>
                /// Determines if the given value occurs in the field.
                /// </summary>

                public bool Contains(int value)
                {
                    return _bits[ValueToIndex(value)];
                }

                /// <summary>
                /// Accumulates the given range (start to end) and interval of values
                /// into the current set of the field.
                /// </summary>
                /// <remarks>
                /// To set the entire range of values representable by the field,
                /// set <param name="start" /> and <param name="end" /> to -1 and
                /// <param name="interval" /> to 1.
                /// </remarks>

                private ExceptionProvider Accumulate(int start, int end, int interval, ExceptionHandler onError)
                {
                    var minValue = _impl.MinValue;
                    var maxValue = _impl.MaxValue;

                    if (start == end)
                    {
                        if (start < 0)
                        {
                            //
                            // We're setting the entire range of values.
                            //

                            if (interval <= 1)
                            {
                                _minValueSet = minValue;
                                _maxValueSet = maxValue;
                                _bits.SetAll(true);
                                return null;
                            }

                            start = minValue;
                            end = maxValue;
                        }
                        else
                        {
                            //
                            // We're only setting a single value - check that it is in range.
                            //

                            if (start < minValue)
                                return OnValueBelowMinError(start, onError);

                            if (start > maxValue)
                                return OnValueAboveMaxError(start, onError);
                        }
                    }
                    else
                    {
                        //
                        // For ranges, if the start is bigger than the end value then
                        // swap them over.
                        //

                        if (start > end)
                        {
                            end ^= start;
                            start ^= end;
                            end ^= start;
                        }

                        if (start < 0)
                            start = minValue;
                        else if (start < minValue)
                            return OnValueBelowMinError(start, onError);

                        if (end < 0)
                            end = maxValue;
                        else if (end > maxValue)
                            return OnValueAboveMaxError(end, onError);
                    }

                    if (interval < 1)
                        interval = 1;

                    int i;

                    //
                    // Populate the _bits table by setting all the bits corresponding to
                    // the valid field values.
                    //

                    for (i = start - minValue; i <= (end - minValue); i += interval)
                        _bits[i] = true;

                    //
                    // Make sure we remember the minimum value set so far Keep track of
                    // the highest and lowest values that have been added to this field
                    // so far.
                    //

                    if (_minValueSet > start)
                        _minValueSet = start;

                    i += (minValue - interval);

                    if (_maxValueSet < i)
                        _maxValueSet = i;

                    return null;
                }

                private ExceptionProvider OnValueAboveMaxError(int value, ExceptionHandler onError)
                {
                    return ErrorHandling.OnError(
                        () => new CrontabException(string.Format(
                            "{0} is higher than the maximum allowable value for the [{3}] field. Value must be between {1} and {2} (all inclusive).",
                            value, _impl.MinValue, _impl.MaxValue, _impl.Kind)),
                        onError);
                }

                private ExceptionProvider OnValueBelowMinError(int value, ExceptionHandler onError)
                {
                    return ErrorHandling.OnError(
                        () => new CrontabException(string.Format(
                            "{0} is lower than the minimum allowable value for the [{3}] field. Value must be between {1} and {2} (all inclusive).",
                            value, _impl.MinValue, _impl.MaxValue, _impl.Kind)),
                        onError);
                }

                public override string ToString()
                {
                    return ToString(null);
                }

                public string ToString(string format)
                {
                    var writer = new StringWriter(CultureInfo.InvariantCulture);

                    switch (format)
                    {
                        case "G":
                        case null:
                            Format(writer, true);
                            break;
                        case "N":
                            Format(writer);
                            break;
                        default:
                            throw new FormatException();
                    }

                    return writer.ToString();
                }

                public void Format(TextWriter writer)
                {
                    Format(writer, false);
                }

                public void Format(TextWriter writer, bool noNames)
                {
                    _impl.Format(this, writer, noNames);
                }
            }
            [Serializable]
            public sealed class CrontabFieldImpl : IObjectReference
            {
                public static readonly CrontabFieldImpl Minute = new CrontabFieldImpl(CrontabFieldKind.Minute, 0, 59, null);
                public static readonly CrontabFieldImpl Hour = new CrontabFieldImpl(CrontabFieldKind.Hour, 0, 23, null);
                public static readonly CrontabFieldImpl Day = new CrontabFieldImpl(CrontabFieldKind.Day, 1, 31, null);
                public static readonly CrontabFieldImpl Month = new CrontabFieldImpl(CrontabFieldKind.Month, 1, 12, new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });
                public static readonly CrontabFieldImpl DayOfWeek = new CrontabFieldImpl(CrontabFieldKind.DayOfWeek, 0, 6, new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });

                private static readonly CrontabFieldImpl[] _fieldByKind = new[] { Minute, Hour, Day, Month, DayOfWeek };

                private static readonly CompareInfo _comparer = CultureInfo.InvariantCulture.CompareInfo;
                private static readonly char[] _comma = new[] { ',' };

                private readonly CrontabFieldKind _kind;
                private readonly int _minValue;
                private readonly int _maxValue;
                private readonly string[] _names;

                public static CrontabFieldImpl FromKind(CrontabFieldKind kind)
                {
                    if (!Enum.IsDefined(typeof(CrontabFieldKind), kind))
                    {
                        throw new ArgumentException(string.Format(
                            "Invalid crontab field kind. Valid values are {0}.",
                            string.Join(", ", Enum.GetNames(typeof(CrontabFieldKind)))), "kind");
                    }

                    return _fieldByKind[(int)kind];
                }

                private CrontabFieldImpl(CrontabFieldKind kind, int minValue, int maxValue, string[] names)
                {
                    UnityEngine.Debug.Assert(Enum.IsDefined(typeof(CrontabFieldKind), kind));
                    UnityEngine.Debug.Assert(minValue >= 0);
                    UnityEngine.Debug.Assert(maxValue >= minValue);
                    UnityEngine.Debug.Assert(names == null || names.Length == (maxValue - minValue + 1));

                    _kind = kind;
                    _minValue = minValue;
                    _maxValue = maxValue;
                    _names = names;
                }

                public CrontabFieldKind Kind
                {
                    get { return _kind; }
                }

                public int MinValue
                {
                    get { return _minValue; }
                }

                public int MaxValue
                {
                    get { return _maxValue; }
                }

                public int ValueCount
                {
                    get { return _maxValue - _minValue + 1; }
                }

                public void Format(ICrontabField field, TextWriter writer)
                {
                    Format(field, writer, false);
                }

                public void Format(ICrontabField field, TextWriter writer, bool noNames)
                {
                    if (field == null)
                        throw new ArgumentNullException("field");

                    if (writer == null)
                        throw new ArgumentNullException("writer");

                    var next = field.GetFirst();
                    var count = 0;

                    while (next != -1)
                    {
                        var first = next;
                        int last;

                        do
                        {
                            last = next;
                            next = field.Next(last + 1);
                        }
                        while (next - last == 1);

                        if (count == 0
                            && first == _minValue && last == _maxValue)
                        {
                            writer.Write('*');
                            return;
                        }

                        if (count > 0)
                            writer.Write(',');

                        if (first == last)
                        {
                            FormatValue(first, writer, noNames);
                        }
                        else
                        {
                            FormatValue(first, writer, noNames);
                            writer.Write('-');
                            FormatValue(last, writer, noNames);
                        }

                        count++;
                    }
                }

                private void FormatValue(int value, TextWriter writer, bool noNames)
                {
                    UnityEngine.Debug.Assert(writer != null);

                    if (noNames || _names == null)
                    {
                        if (value >= 0 && value < 100)
                        {
                            FastFormatNumericValue(value, writer);
                        }
                        else
                        {
                            writer.Write(value.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                        var index = value - _minValue;
                        writer.Write(_names[index]);
                    }
                }

                private static void FastFormatNumericValue(int value, TextWriter writer)
                {
                    UnityEngine.Debug.Assert(value >= 0 && value < 100);
                    UnityEngine.Debug.Assert(writer != null);

                    if (value >= 10)
                    {
                        writer.Write((char)('0' + (value / 10)));
                        writer.Write((char)('0' + (value % 10)));
                    }
                    else
                    {
                        writer.Write((char)('0' + value));
                    }
                }

                public void Parse(string str, CrontabFieldAccumulator acc)
                {
                    TryParse(str, acc, ErrorHandling.Throw);
                }

                public ExceptionProvider TryParse(string str, CrontabFieldAccumulator acc, ExceptionHandler onError)
                {
                    if (acc == null)
                        throw new ArgumentNullException("acc");

                    if (string.IsNullOrEmpty(str))
                        return null;

                    try
                    {
                        return InternalParse(str, acc, onError);
                    }
                    catch (FormatException e)
                    {
                        return OnParseException(e, str, onError);
                    }
                    catch (CrontabException e)
                    {
                        return OnParseException(e, str, onError);
                    }
                }

                private ExceptionProvider OnParseException(Exception innerException, string str, ExceptionHandler onError)
                {
                    UnityEngine.Debug.Assert(str != null);
                    UnityEngine.Debug.Assert(innerException != null);

                    return ErrorHandling.OnError(
                               () => new CrontabException(string.Format("'{0}' is not a valid [{1}] crontab field expression.", str, Kind), innerException),
                               onError);
                }

                private ExceptionProvider InternalParse(string str, CrontabFieldAccumulator acc, ExceptionHandler onError)
                {
                    UnityEngine.Debug.Assert(str != null);
                    UnityEngine.Debug.Assert(acc != null);

                    if (str.Length == 0)
                        return ErrorHandling.OnError(() => new CrontabException("A crontab field value cannot be empty."), onError);

                    //
                    // Next, look for a list of values (e.g. 1,2,3).
                    //

                    var commaIndex = str.IndexOf(",");

                    if (commaIndex > 0)
                    {
                        ExceptionProvider e = null;
                        var token = ((IEnumerable<string>)str.Split(_comma)).GetEnumerator();
                        while (token.MoveNext() && e == null)
                            e = InternalParse(token.Current, acc, onError);
                        return e;
                    }

                    var every = 1;

                    //
                    // Look for stepping first (e.g. */2 = every 2nd).
                    // 

                    var slashIndex = str.IndexOf("/");

                    if (slashIndex > 0)
                    {
                        every = int.Parse(str.Substring(slashIndex + 1), CultureInfo.InvariantCulture);
                        str = str.Substring(0, slashIndex);
                    }

                    //
                    // Next, look for wildcard (*).
                    //

                    if (str.Length == 1 && str[0] == '*')
                    {
                        return acc(-1, -1, every, onError);
                    }

                    //
                    // Next, look for a range of values (e.g. 2-10).
                    //

                    var dashIndex = str.IndexOf("-");

                    if (dashIndex > 0)
                    {
                        var first = ParseValue(str.Substring(0, dashIndex));
                        var last = ParseValue(str.Substring(dashIndex + 1));

                        return acc(first, last, every, onError);
                    }

                    //
                    // Finally, handle the case where there is only one number.
                    //

                    var value = ParseValue(str);

                    if (every == 1)
                        return acc(value, value, 1, onError);

                    UnityEngine.Debug.Assert(every != 0);
                    return acc(value, _maxValue, every, onError);
                }

                private int ParseValue(string str)
                {
                    UnityEngine.Debug.Assert(str != null);

                    if (str.Length == 0)
                        throw new CrontabException("A crontab field value cannot be empty.");

                    var firstChar = str[0];

                    if (firstChar >= '0' && firstChar <= '9')
                        return int.Parse(str, CultureInfo.InvariantCulture);

                    if (_names == null)
                    {
                        throw new CrontabException(string.Format(
                            "'{0}' is not a valid [{3}] crontab field value. It must be a numeric value between {1} and {2} (all inclusive).",
                            str, _minValue.ToString(), _maxValue.ToString(), _kind.ToString()));
                    }

                    for (var i = 0; i < _names.Length; i++)
                    {
                        if (_comparer.IsPrefix(_names[i], str, CompareOptions.IgnoreCase))
                            return i + _minValue;
                    }

                    throw new CrontabException(string.Format(
                        "'{0}' is not a known value name. Use one of the following: {1}.",
                        str, string.Join(", ", _names)));
                }

                object IObjectReference.GetRealObject(StreamingContext context)
                {
                    return FromKind(Kind);
                }
            }
            [Serializable]
            public enum CrontabFieldKind
            {
                Minute = 0, // Keep in order of appearance in expression
                Hour,
                Day,
                Month,
                DayOfWeek
            }
            [Serializable]
            public sealed class CrontabSchedule
            {
                private readonly CrontabField _minutes;
                private readonly CrontabField _hours;
                private readonly CrontabField _days;
                private readonly CrontabField _months;
                private readonly CrontabField _daysOfWeek;

                private static readonly char[] _separators = new[] { ' ' };

                //
                // Crontab expression format:
                //
                // * * * * *
                // - - - - -
                // | | | | |
                // | | | | +----- day of week (0 - 6) (Sunday=0)
                // | | | +------- month (1 - 12)
                // | | +--------- day of month (1 - 31)
                // | +----------- hour (0 - 23)
                // +------------- min (0 - 59)
                //
                // Star (*) in the value field above means all legal values as in 
                // braces for that column. The value column can have a * or a list 
                // of elements separated by commas. An element is either a number in 
                // the ranges shown above or two numbers in the range separated by a 
                // hyphen (meaning an inclusive range). 
                //
                // Source: http://www.adminschoice.com/docs/crontab.htm
                //

                public static CrontabSchedule Parse(string expression)
                {
                    return TryParse(expression, ErrorHandling.Throw).Value;
                }

                public static ValueOrError<CrontabSchedule> TryParse(string expression)
                {
                    return TryParse(expression, null);
                }

                private static ValueOrError<CrontabSchedule> TryParse(string expression, ExceptionHandler onError)
                {
                    if (expression == null)
                        throw new ArgumentNullException("expression");

                    var tokens = expression.Split(_separators, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length != 5)
                    {
                        return ErrorHandling.OnError(() => new CrontabException(string.Format(
                                   "'{0}' is not a valid crontab expression. It must contain at least 5 components of a schedule "
                                   + "(in the sequence of minutes, hours, days, months, days of week).",
                                   expression)), onError);
                    }

                    var fields = new CrontabField[5];

                    for (var i = 0; i < fields.Length; i++)
                    {
                        var field = CrontabField.TryParse((CrontabFieldKind)i, tokens[i], onError);
                        if (field.IsError)
                            return field.ErrorProvider;

                        fields[i] = field.Value;
                    }

                    return new CrontabSchedule(fields[0], fields[1], fields[2], fields[3], fields[4]);
                }

                private CrontabSchedule(
                    CrontabField minutes, CrontabField hours,
                    CrontabField days, CrontabField months,
                    CrontabField daysOfWeek)
                {
                    UnityEngine.Debug.Assert(minutes != null);
                    UnityEngine.Debug.Assert(hours != null);
                    UnityEngine.Debug.Assert(days != null);
                    UnityEngine.Debug.Assert(months != null);
                    UnityEngine.Debug.Assert(daysOfWeek != null);

                    _minutes = minutes;
                    _hours = hours;
                    _days = days;
                    _months = months;
                    _daysOfWeek = daysOfWeek;
                }

                /// <summary>
                /// Enumerates all the occurrences of this schedule starting with a
                /// base time and up to an end time limit. This method uses deferred
                /// execution such that the occurrences are only calculated as they 
                /// are enumerated.
                /// </summary>
                /// <remarks>
                /// This method does not return the value of <paramref name="baseTime"/>
                /// itself if it falls on the schedule. For example, if <paramref name="baseTime" />
                /// is midnight and the schedule was created from the expression <c>* * * * *</c> 
                /// (meaning every minute) then the next occurrence of the schedule 
                /// will be at one minute past midnight and not midnight itself.
                /// The method returns the <em>next</em> occurrence <em>after</em> 
                /// <paramref name="baseTime"/>. Also, <param name="endTime" /> is
                /// exclusive.
                /// </remarks>

                public IEnumerable<DateTime> GetNextOccurrences(DateTime baseTime, DateTime endTime)
                {
                    for (var occurrence = GetNextOccurrence(baseTime, endTime);
                         occurrence < endTime;
                         occurrence = GetNextOccurrence(occurrence, endTime))
                    {
                        yield return occurrence;
                    }
                }

                /// <summary>
                /// Gets the next occurrence of this schedule starting with a base time.
                /// </summary>

                public DateTime GetNextOccurrence(DateTime baseTime)
                {
                    return GetNextOccurrence(baseTime, DateTime.MaxValue);
                }

                /// <summary>
                /// Gets the next occurrence of this schedule starting with a base 
                /// time and up to an end time limit.
                /// </summary>
                /// <remarks>
                /// This method does not return the value of <paramref name="baseTime"/>
                /// itself if it falls on the schedule. For example, if <paramref name="baseTime" />
                /// is midnight and the schedule was created from the expression <c>* * * * *</c> 
                /// (meaning every minute) then the next occurrence of the schedule 
                /// will be at one minute past midnight and not midnight itself.
                /// The method returns the <em>next</em> occurrence <em>after</em> 
                /// <paramref name="baseTime"/>. Also, <param name="endTime" /> is
                /// exclusive.
                /// </remarks>

                public DateTime GetNextOccurrence(DateTime baseTime, DateTime endTime)
                {
                    const int nil = -1;

                    var baseYear = baseTime.Year;
                    var baseMonth = baseTime.Month;
                    var baseDay = baseTime.Day;
                    var baseHour = baseTime.Hour;
                    var baseMinute = baseTime.Minute;

                    var endYear = endTime.Year;
                    var endMonth = endTime.Month;
                    var endDay = endTime.Day;

                    var year = baseYear;
                    var month = baseMonth;
                    var day = baseDay;
                    var hour = baseHour;
                    var minute = baseMinute + 1;

                    //
                    // Minute
                    //

                    minute = _minutes.Next(minute);

                    if (minute == nil)
                    {
                        minute = _minutes.GetFirst();
                        hour++;
                    }

                    //
                    // Hour
                    //

                    hour = _hours.Next(hour);

                    if (hour == nil)
                    {
                        minute = _minutes.GetFirst();
                        hour = _hours.GetFirst();
                        day++;
                    }
                    else if (hour > baseHour)
                    {
                        minute = _minutes.GetFirst();
                    }

                    //
                    // Day
                    //

                    day = _days.Next(day);

                RetryDayMonth:

                    if (day == nil)
                    {
                        minute = _minutes.GetFirst();
                        hour = _hours.GetFirst();
                        day = _days.GetFirst();
                        month++;
                    }
                    else if (day > baseDay)
                    {
                        minute = _minutes.GetFirst();
                        hour = _hours.GetFirst();
                    }

                    //
                    // Month
                    //

                    month = _months.Next(month);

                    if (month == nil)
                    {
                        minute = _minutes.GetFirst();
                        hour = _hours.GetFirst();
                        day = _days.GetFirst();
                        month = _months.GetFirst();
                        year++;
                    }
                    else if (month > baseMonth)
                    {
                        minute = _minutes.GetFirst();
                        hour = _hours.GetFirst();
                        day = _days.GetFirst();
                    }

                    //
                    // The day field in a cron expression spans the entire range of days
                    // in a month, which is from 1 to 31. However, the number of days in
                    // a month tend to be variable depending on the month (and the year
                    // in case of February). So a check is needed here to see if the
                    // date is a border case. If the day happens to be beyond 28
                    // (meaning that we're dealing with the suspicious range of 29-31)
                    // and the date part has changed then we need to determine whether
                    // the day still makes sense for the given year and month. If the
                    // day is beyond the last possible value, then the day/month part
                    // for the schedule is re-evaluated. So an expression like "0 0
                    // 15,31 * *" will yield the following sequence starting on midnight
                    // of Jan 1, 2000:
                    //
                    //  Jan 15, Jan 31, Feb 15, Mar 15, Apr 15, Apr 31, ...
                    //

                    var dateChanged = day != baseDay || month != baseMonth || year != baseYear;

                    if (day > 28 && dateChanged && day > Calendar.GetDaysInMonth(year, month))
                    {
                        if (year >= endYear && month >= endMonth && day >= endDay)
                            return endTime;

                        day = nil;
                        goto RetryDayMonth;
                    }

                    var nextTime = new DateTime(year, month, day, hour, minute, 0, 0, baseTime.Kind);

                    if (nextTime >= endTime)
                        return endTime;

                    //
                    // Day of week
                    //

                    if (_daysOfWeek.Contains((int)nextTime.DayOfWeek))
                        return nextTime;

                    return GetNextOccurrence(new DateTime(year, month, day, 23, 59, 0, 0, baseTime.Kind), endTime);
                }

                /// <summary>
                /// Returns a string in crontab expression (expanded) that represents 
                /// this schedule.
                /// </summary>

                public override string ToString()
                {
                    var writer = new StringWriter(CultureInfo.InvariantCulture);

                    _minutes.Format(writer, true); writer.Write(' ');
                    _hours.Format(writer, true); writer.Write(' ');
                    _days.Format(writer, true); writer.Write(' ');
                    _months.Format(writer, true); writer.Write(' ');
                    _daysOfWeek.Format(writer, true);

                    return writer.ToString();
                }

                private static Calendar Calendar
                {
                    get { return CultureInfo.InvariantCulture.Calendar; }
                }
            }
            internal static class ErrorHandling
            {
                /// <summary>
                /// A stock <see cref="ExceptionHandler"/> that throws.
                /// </summary>

                public static readonly ExceptionHandler Throw = e => { throw e; };

                internal static ExceptionProvider OnError(ExceptionProvider provider, ExceptionHandler handler)
                {
                    UnityEngine.Debug.Assert(provider != null);

                    if (handler != null)
                        handler(provider());

                    return provider;
                }
            }
            public interface ICrontabField
            {
                int GetFirst();
                int Next(int start);
                bool Contains(int value);
            }
            [Serializable]
            public struct ValueOrError<T>
            {
                private readonly bool _hasValue;
                private readonly T _value;
                private readonly ExceptionProvider _ep;

                private static readonly ExceptionProvider _dep = () => new Exception("Value is undefined.");

                /// <summary>
                /// Initializes the object with a defined value.
                /// </summary>

                public ValueOrError(T value) : this()
                {
                    _hasValue = true;
                    _value = value;
                }

                /// <summary>
                /// Initializes the object with an error.
                /// </summary>

                public ValueOrError(Exception error) : this(CheckError(error)) { }

                private static ExceptionProvider CheckError(Exception error)
                {
                    if (error == null) throw new ArgumentNullException("error");
                    return () => error;
                }

                /// <summary>
                /// Initializes the object with a handler that will provide
                /// the error result when needed.
                /// </summary>

                public ValueOrError(ExceptionProvider provider)
                    : this()
                {
                    if (provider == null) throw new ArgumentNullException("provider");
                    _ep = provider;
                }

                /// <summary>
                /// Determines if object holds a defined value or not.
                /// </summary>

                public bool HasValue { get { return _hasValue; } }

                /// <summary>
                /// Gets the value otherwise throws an error if undefined.
                /// </summary>

                public T Value { get { if (!HasValue) throw ErrorProvider(); return _value; } }

                /// <summary>
                /// Determines if object identifies an error condition or not.
                /// </summary>

                public bool IsError { get { return ErrorProvider != null; } }

                /// <summary>
                /// Gets the <see cref="Exception"/> object if this object
                /// represents an error condition otherwise it returns <c>null</c>.
                /// </summary>

                public Exception Error { get { return IsError ? ErrorProvider() : null; } }

                /// <summary>
                /// Gets the <see cref="ExceptionProvider"/> object if this 
                /// object represents an error condition otherwise it returns <c>null</c>.
                /// </summary>

                public ExceptionProvider ErrorProvider { get { return HasValue ? null : _ep ?? _dep; } }

                /// <summary>
                /// Attempts to get the defined value or another in case
                /// of an error.
                /// </summary>

                public T TryGetValue(T errorValue)
                {
                    return IsError ? errorValue : Value;
                }

                /// <summary>
                /// Implicitly converts a <typeparamref name="T"/> value to
                /// an object of this type.
                /// </summary>

                public static implicit operator ValueOrError<T>(T value) { return new ValueOrError<T>(value); }

                /// <summary>
                /// Implicitly converts an <see cref="Exception"/> object to
                /// an object of this type that represents the error condition.
                /// </summary>

                public static implicit operator ValueOrError<T>(Exception error) { return new ValueOrError<T>(error); }

                /// <summary>
                /// Implicitly converts an <see cref="ExceptionProvider"/> object to
                /// an object of this type that represents the error condition.
                /// </summary>

                public static implicit operator ValueOrError<T>(ExceptionProvider provider) { return new ValueOrError<T>(provider); }

                /// <summary>
                /// Explicits converts this object to a <typeparamref name="T"/> value.
                /// </summary>

                public static explicit operator T(ValueOrError<T> ve) { return ve.Value; }

                /// <summary>
                /// Explicits converts this object to an <see cref="Exception"/> object
                /// if it represents an error condition. The conversion yields <c>null</c>
                /// if this object does not represent an error condition.
                /// </summary>

                public static explicit operator Exception(ValueOrError<T> ve) { return ve.Error; }

                /// <summary>
                /// Explicits converts this object to an <see cref="ExceptionProvider"/> object
                /// if it represents an error condition. The conversion yields <c>null</c>
                /// if this object does not represent an error condition.
                /// </summary>

                public static explicit operator ExceptionProvider(ValueOrError<T> ve) { return ve.ErrorProvider; }

                public override string ToString()
                {
                    var error = Error;
                    return IsError
                         ? error.GetType().FullName + ": " + error.Message
                         : _value != null
                         ? _value.ToString() : string.Empty;
                }
            }
        }
        private static Core.Logging.Logger RootLogger = Interface.GetMod().RootLogger;
        private Dictionary<Plugin, List<Timer>> TimerList = new Dictionary<Plugin, List<Timer>>();
        void Loaded()
        {
            Manager.OnPluginRemoved += OnPluginRemovedControlCrons;
            TimerList.Add(this, new List<Timer> { timer.Repeat(60, 0, new Action(() =>
            {
                foreach (KeyValuePair<Plugin,List<Timer>> Current in TimerList)
                {
                    if ((Current.Key == null) || (!Current.Key.IsLoaded))
                    {
                        Current.Value.FindAll(e => e.Destroyed == false).ForEach(e => e.Destroy());
                    }
                    Current.Value.RemoveAll(e => e.Destroyed);
                }
            }))});
        }
        void Unload()
        {
            Manager.OnPluginRemoved -= OnPluginRemovedControlCrons;
        }
        bool RegisterCron(Plugin Owner,bool UseUTC, string CronSyntax, Action Function)
        {

            Action RunAction = new Action(() =>
            {
                try
                {
                    if ((Owner != null) && (Owner.IsLoaded))
                    {
                        Function();
                        RegisterCron(Owner, UseUTC, CronSyntax, Function);
                    }

                }
                catch (Exception Ex)
                {
                    RootLogger.Write(Core.Logging.LogType.Error, "[Cron Library] {0}", Ex.Message);
                }
            });
            try
            {
                if (!TimerList.ContainsKey(Owner))
                    TimerList.Add(Owner, new List<Timer>());
                List<Timer> Current;
                if (TimerList.TryGetValue(Owner, out Current))
                {
                    DateTime BaseTime = (UseUTC) ? DateTime.UtcNow : DateTime.Now;
                    DateTime NextRun = NCrontab.CrontabSchedule.Parse(CronSyntax).GetNextOccurrence(BaseTime);

                    DateTime UTCTime = (UseUTC) ? NextRun : TimeZone.CurrentTimeZone.ToUniversalTime(NextRun);
                    DateTime UserTime = (UseUTC) ? TimeZone.CurrentTimeZone.ToLocalTime(NextRun) : NextRun;

                    RootLogger.Write(Core.Logging.LogType.Debug, "[Cron Library] {0} added new cron with {1}\n\t***Next Run***\n\tUTC\t: {2} UTC\n\tLocal\t: {3} {4}", Owner.Name, CronSyntax, UTCTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss"), UserTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss"), (TimeZone.CurrentTimeZone.IsDaylightSavingTime(UserTime) ? TimeZone.CurrentTimeZone.DaylightName:TimeZone.CurrentTimeZone.StandardName));
                    TimeSpan NextTimeSpan = NextRun - ((UseUTC) ? DateTime.UtcNow : DateTime.Now);
                    double TimeLeft = NextTimeSpan.TotalSeconds;
                    if (TimeLeft - Math.Round(TimeLeft) != 0)
                        TimeLeft++;
                    Current.Add((timer.Once((int)TimeLeft, RunAction)));
                    return true;
                }
                else
                    return false;
            }
            catch (Exception Ex)
            {
                RootLogger.Write(Core.Logging.LogType.Error, "[Cron Library] {0}", Ex.Message);
                return false;
            }
        }
        object GetNextOccurrence(bool UseUTC, string CronSyntax, bool IsString = true)
        {
            DateTime BaseTime = (UseUTC) ? DateTime.UtcNow : DateTime.Now;
            if (IsString)
                return NCrontab.CrontabSchedule.Parse(CronSyntax).GetNextOccurrence(BaseTime).ToString("ddd, dd MMM yyyy HH':'mm':'ss");
            else
                return NCrontab.CrontabSchedule.Parse(CronSyntax).GetNextOccurrence(BaseTime);
        }

        private void OnPluginRemovedControlCrons(Plugin plugin)
        {
            if (TimerList.ContainsKey(plugin))
            {
                int RemovedCrons = 0;
                foreach (KeyValuePair<Plugin, List<Timer>> Current in TimerList)
                {
                    Current.Value.FindAll(e => e.Destroyed == false).ForEach(e => e.Destroy());
                    RemovedCrons += Current.Value.RemoveAll(e => e.Destroyed);
                }
                TimerList.Remove(plugin);
                RootLogger.Write(Core.Logging.LogType.Warning, "[Cron Library] Plugin {0} unloaded.Removed {1} cron jobs.", plugin.Name, RemovedCrons);
            }
        }
    }
}
