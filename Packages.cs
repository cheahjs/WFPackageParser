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
}
