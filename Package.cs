using System;
using System.Collections.Generic;

namespace PackagesLexer
{
    public class Package : Dictionary<string, object>
    {
        private readonly string[] rawLines;
        private bool logVerbose;

        public Package(string raw, bool log = false) : base()
        {
            rawLines = raw.Trim().Replace("\r", "").Split('\n');
            logVerbose = log;
            ParseRoot();
        }

        public Package() : base() { }

        private void ParseRoot()
        {
            int i = 0;
            while (i < rawLines.Length)
            {
                var line = rawLines[i];
                if (logVerbose) Console.WriteLine("ROOT:{1}:{0}",line, i);
                var datapair = GetKvPair(line);
                var pair = new KeyValuePair<string, object>(datapair.Key, datapair.Value);
                if (datapair.Value == Tokens.BR_OPEN.ToString())
                {
                    i++;
                    var datapair2 = GetKvPair(rawLines[i]);
                    if (rawLines[i] == Tokens.BR_OPEN.ToString())
                    {
                        if (logVerbose) Console.WriteLine("ROOT>LIST:{0}", i);
                        pair = ParseList(ref i, datapair.Key);
                    }
                    else if (datapair2.Key == datapair2.Value /*&& datapair2.Key.EndsWith(",")*/) //we got an array
                    {
                        if (logVerbose) Console.WriteLine("ROOT>ARRAY:{0}", i);
                        pair = ParseArray(ref i, datapair.Key);
                    }
                    else
                    {
                        if (logVerbose) Console.WriteLine("ROOT>OBJ:{0}", i);
                        pair = ParseObject(ref i, datapair.Key);
                    }
                }
                if (datapair.Value == Tokens.BR_OPEN.ToString() + Tokens.BR_CLOSE.ToString())
                {
                    if (logVerbose) Console.WriteLine("ROOT>EMPTY:{0}", i);
                    pair = new KeyValuePair<string, object>(pair.Key, new object[0]);
                }
                else if (datapair.Value.StartsWith(Tokens.BR_OPEN.ToString()) && datapair.Value.EndsWith(Tokens.BR_CLOSE.ToString()))
                {
                    if (logVerbose) Console.WriteLine("ROOT>SINGLE:{0}", i);
                    pair = new KeyValuePair<string, object>(pair.Key, ParseSingleArray(datapair.Value));
                }
                int append = 1;
                while (this.ContainsKey(pair.Key))
                {
                    pair = new KeyValuePair<string, object>(pair.Key + "_" + append, pair.Value);
                    append++;
                }
                this.Add(pair.Key, pair.Value);
                i++;
            }
        }

        private KeyValuePair<string, object> ParseObject(ref int i, string key)
        {
            var dict2 = new Dictionary<string, object>();
            while (i < rawLines.Length)
            {
                var line = rawLines[i];
                var datapair = GetKvPair(line);
                if (logVerbose) Console.WriteLine("OBJ:{1}:{0}", line, i);
                var pair = new KeyValuePair<string, object>(datapair.Key, datapair.Value);
                if (datapair.Value == Tokens.BR_CLOSE.ToString())
                {
                    //i++;
                    break;
                }
                if (datapair.Value == Tokens.BR_OPEN.ToString())
                {
                    i++;
                    var datapair2 = GetKvPair(rawLines[i]);
                    if (rawLines[i] == Tokens.BR_OPEN.ToString())
                    {
                        if (logVerbose) Console.WriteLine("OBJ>LIST:{0}", i);
                        pair = ParseList(ref i, datapair.Key);
                    }
                    else if (datapair2.Key == datapair2.Value /*&& datapair2.Key.EndsWith(",")*/) //we got an array
                    {
                        if (logVerbose) Console.WriteLine("OBJ>ARRAY:{0}", i);
                        pair = ParseArray(ref i, datapair.Key);
                    }
                    else
                    {
                        if (logVerbose) Console.WriteLine("OBJ>OBJ:{0}", i);
                        pair = ParseObject(ref i, datapair.Key);
                    }
                }
                if (datapair.Value == Tokens.BR_OPEN.ToString() + Tokens.BR_CLOSE.ToString())
                {
                    if (logVerbose) Console.WriteLine("OBJ>EMPTY:{0}", i);
                    pair = new KeyValuePair<string, object>(pair.Key, new object[0]);
                }
                else if (datapair.Value.StartsWith(Tokens.BR_OPEN.ToString()) && datapair.Value.EndsWith(Tokens.BR_CLOSE.ToString()))
                {
                    if (logVerbose) Console.WriteLine("OBJ>SINGLE:{0}", i);
                    pair = new KeyValuePair<string, object>(pair.Key, ParseSingleArray(datapair.Value));
                }
                int append = 1;
                while (dict2.ContainsKey(pair.Key))
                {
                    pair = new KeyValuePair<string, object>(pair.Key + "_" + append, pair.Value);
                    append++;
                }
                dict2.Add(pair.Key, pair.Value);
                i++;
            }
            return new KeyValuePair<string, object>(key, dict2);
        }

        private KeyValuePair<string, object> ParseList(ref int index, string key)
        {
            var list = new List<object>();
            while (index < rawLines.Length)
            {
                index++;
                if (logVerbose) Console.WriteLine("LIST:{0}", index);
                list.Add(ParseListObject(ref index));
                if (rawLines[index] == Tokens.BR_CLOSE.ToString())
                    break;
                //index++;
            }
            return new KeyValuePair<string, object>(key, list);
        }

        private object ParseListObject(ref int i)
        {
            var dict2 = new Dictionary<string, object>();
            while (i < rawLines.Length)
            {
                var line = rawLines[i];
                if (logVerbose) Console.WriteLine("LOBJ:{1}:{0}", line, i);
                var datapair = GetKvPair(line);
                var pair = new KeyValuePair<string, object>(datapair.Key, datapair.Value);
                if (datapair.Value == Tokens.BR_CLOSE.ToString() || datapair.Value == Tokens.BR_CLOSE.ToString() + Tokens.COMMA.ToString())
                {
                    i++;
                    break;
                }
                if (datapair.Value == Tokens.BR_OPEN.ToString())
                {
                    i++;
                    var datapair2 = GetKvPair(rawLines[i]);
                    if (rawLines[i] == Tokens.BR_OPEN.ToString())
                    {
                        if (logVerbose) Console.WriteLine("LOBJ>LIST:{0}", i);
                        pair = ParseList(ref i, datapair.Key);
                    }
                    else if (datapair2.Key == datapair2.Value /*&& datapair2.Key.EndsWith(",")*/) //we got an array
                    {
                        if (logVerbose) Console.WriteLine("LOBJ>ARRAY:{0}", i);
                        pair = ParseArray(ref i, datapair.Key);
                    }
                    else
                    {
                        if (logVerbose) Console.WriteLine("LOBJ>OBJ:{0}", i);
                        pair = ParseObject(ref i, datapair.Key);
                    }
                }
                if (datapair.Value == Tokens.BR_OPEN.ToString() + Tokens.BR_CLOSE.ToString())
                {
                    if (logVerbose) Console.WriteLine("LOBJ>EMPTY:{0}", i);
                    pair = new KeyValuePair<string, object>(pair.Key, new object[0]);
                }
                else if (datapair.Value.StartsWith(Tokens.BR_OPEN.ToString()) && datapair.Value.EndsWith(Tokens.BR_CLOSE.ToString()))
                {
                    if (logVerbose) Console.WriteLine("LOBJ>SINGLE:{0}", i);
                    pair = new KeyValuePair<string, object>(pair.Key, ParseSingleArray(datapair.Value));
                }
                int append = 1;
                while (dict2.ContainsKey(pair.Key))
                {
                    pair = new KeyValuePair<string, object>(pair.Key + "_" + append, pair.Value);
                    append++;
                }
                dict2.Add(pair.Key, pair.Value);
                i++;
            }
            return dict2;
        }

        private KeyValuePair<string, object> ParseArray(ref int index, string key)
        {
            var list = new List<object>();
            while (index < rawLines.Length)
            {
                if (rawLines[index] == Tokens.BR_CLOSE.ToString())
                {
                    break;
                }
                list.Add(rawLines[index].Trim().TrimEnd(','));
                index++;
            }
            return new KeyValuePair<string, object>(key, list);
        }

        private static object[] ParseSingleArray(string values)
        {
            values = values.Trim('{', '}');
            return values.Split(',');
        }

        private static KeyValuePair<string, string> GetKvPair(string line)
        {
            var parts = line.Split('=');
            if (parts.Length == 2)
            {
                var key = parts[0];
                var value = parts[1].Replace("\"", "").Trim();
                return new KeyValuePair<string, string>(key, value);
            }
            return new KeyValuePair<string, string>(line, line);
        } 
    }
}