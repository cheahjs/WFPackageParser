using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PackagesLexer
{
    public class Packages : List<PackageChunk>
    {
        /// <summary>
        /// Parses Packages.bin located at <see cref="fileName"/> and creates a List of <see cref="PackageChunk"/>
        /// </summary>
        /// <param name="fileName">Path to Packages.bin</param>
        public Packages(string fileName)
        {
            using (var file = File.OpenRead(fileName))
            {
                using (var reader = new BinaryReader(file))
                {
                    var hash = reader.ReadBytes(0x1d);
                    var numberOfTopStructs = reader.ReadInt32();
                    var topstructs = new List<TopStruct>();
                    for (var i = 0; i < numberOfTopStructs; i++)
                    {
                        var length = reader.ReadInt32();
                        var tempheaderbytes = reader.ReadBytes(length);
                        var str = Encoding.UTF8.GetString(tempheaderbytes);
                        var unk = reader.ReadInt32();
                        topstructs.Add(new TopStruct() { Name = str, Unknown = unk });
                    }
                    var totalchunksize = reader.ReadInt32();
                    var chunks = new List<string>();
                    for (var i = 0; i < totalchunksize; i++)
                    {
                        var sb = new StringBuilder();
                        byte inbyte;
                        while (true)
                        {
                            i++;
                            inbyte = reader.ReadByte();
                            if (inbyte == 0) break;
                            sb.Append((char)inbyte);
                        }
                        i--;
                        chunks.Add(sb.ToString());
                    }
                    var noOfChunks = reader.ReadInt32();
                    if (noOfChunks != chunks.Count)
                    {
                        Console.WriteLine("We have mismatch in number of chunks and expected chunks. {0} != {1}", chunks.Count, noOfChunks);
                    }
                    for (var i = 0; i < noOfChunks; i++)
                    {
                        var length = reader.ReadInt32();
                        var tempbytes = reader.ReadBytes(length);
                        var path = Encoding.UTF8.GetString(tempbytes);
                        length = reader.ReadInt32();
                        tempbytes = reader.ReadBytes(length);
                        var name = Encoding.UTF8.GetString(tempbytes);
                        reader.ReadBytes(5);
                        length = reader.ReadInt32();
                        tempbytes = reader.ReadBytes(length);
                        var basename = Encoding.UTF8.GetString(tempbytes);
                        reader.ReadInt32();
                        var package = new PackageChunk
                        {
                            Name = name,
                            BasePackage = basename,
                            RawChunk = chunks[i],
                            HeaderPath = path
                        };
                        try
                        {
                            package.ParsedChunk = new Package(package.RawChunk);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error parsing chunk.");
                        }
                        this.Add(package);
                    }
                    chunks.Clear();
                }
            }
        }
    }

    internal class TopStruct
    {
        public string Name;
        public int Unknown;
    }

    /// <summary>
    /// Holds data about a package chunk.
    /// </summary>
    public class PackageChunk
    {
        /// <summary>
        /// The raw string package.
        /// </summary>
        public string RawChunk;
        /// <summary>
        /// The name of the package.
        /// </summary>
        public string Name;
        /// <summary>
        /// Name/Path+Name to the base package.
        /// </summary>
        /// <remarks>
        /// If <see cref="BasePackage"/> is only the name, assume the same header path as this package.
        /// </remarks>
        public string BasePackage;
        /// <summary>
        /// The header path that this package is located in.
        /// </summary>
        public string HeaderPath;
        /// <summary>
        /// A dictionary that holds key-value pairs of the data in this package.
        /// </summary>
        public Package ParsedChunk;
    }

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

    public class Tokens
    {
        public const char EQUALS = '=';
        public const char WHITESPACE = ' ';
        public const char QUOTE = '"';
        public const char NEWLINE = '\n';
        public const char BR_OPEN = '{';
        public const char BR_CLOSE = '}';
        public const char COMMA = ',';
    }
}
