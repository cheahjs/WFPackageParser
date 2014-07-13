using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PackagesLexer
{
    /// <summary>
    /// Dictionary is in the form of unlocalized text, string
    /// </summary>
    public class Languages : Dictionary<string, string>
    {
        /// <summary>
        /// Parses <see cref="fileName"/> which should be a Languages.bin file and creates a dictionary with localization strings
        /// </summary>
        /// <param name="fileName">Path to Languages.bin</param>
        public Languages(string fileName)
        {
            using (var file = File.OpenRead(fileName))
            {
                using (var reader = new BinaryReader(file))
                {
                    reader.ReadBytes(0x1D);   //Unknown bytes
                    var numberOfLanguages = reader.ReadInt32();
                    for (int i = 0; i < numberOfLanguages; i++)
                    {
                        var length = reader.ReadInt32();
                        var templangbytes = reader.ReadBytes(length);
                        //var language = Encoding.ASCII.GetString(templangbytes);
                    }
                    reader.ReadInt32(); //Unknown
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var headerStrSize = reader.ReadInt32();
                        var tempheaderbytes = reader.ReadBytes(headerStrSize);
                        var headerStr = Encoding.UTF8.GetString(tempheaderbytes);
                        reader.ReadInt32(); //Unknown
                        var numberOfStrings = reader.ReadInt32();
                        var lengthOfStrings = reader.ReadInt32();
                        var tempstringbytes = reader.ReadBytes(lengthOfStrings);
                        var rawStrings = Encoding.UTF8.GetString(tempstringbytes);
                        var stringList = rawStrings.Split('\0');
                        for (int i = 0; i < numberOfStrings; i++)
                        {
                            var lengthOfSuffix = reader.ReadInt32();
                            var tempsuffixbytes = reader.ReadBytes(lengthOfSuffix);
                            var headerSuffix = Encoding.UTF8.GetString(tempsuffixbytes);
                            this[headerStr + headerSuffix] = stringList[i];
                            reader.ReadBytes(11);   //Unknown
                        }
                    }
                }
            }
        }
    }
}
