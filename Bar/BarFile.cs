// The following code snippet is adapted from Resource-Manager, all rights reserved to the original author.
// Project URL: https://github.com/AOE3-Modding-Council/Resource-Manager
//
// MIT License
//
// Copyright (c) 2020 XaKOps
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.ObjectModel;
using System.Text;

namespace aoe3_auto_packager
{
    public class BarFile
    {
        public static async Task<BarFile> Create(string root, string filename)
        {
            BarFile barFile = new();

            if (!Directory.Exists(root))
                throw new Exception("Directory does not exist!");

            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);


            using (var fileStream = File.Open(Path.Combine(Directory.GetParent(root)!.FullName, filename + ".bar"), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using var writer = new BinaryWriter(fileStream);
                //Write Bar Header
                var header = new BarFileHeader(files, filename + ".bar");
                writer.Write(header.ToByteArray());
                writer.Write(0);

                //Write Files
                var barEntrys = new List<BarEntry>();
                foreach (var file in files)
                {
                    var entry = await BarEntry.Create(root, file, (int)writer.BaseStream.Position);

                    var data = await File.ReadAllBytesAsync(file);
                    writer.Write(data);

                    barEntrys.Add(entry);
                }

                barFile.RootPath = Path.GetFileName(root) + Path.DirectorySeparatorChar;
                barFile.NumberOfRootFiles = (uint)barEntrys.Count;
                barFile.BarFileEntrys = new ReadOnlyCollection<BarEntry>(barEntrys);

                writer.Write(barFile.ToByteArray());
            }

            return barFile;
        }

        public string RootPath { get; set; }

        public uint NumberOfRootFiles { get; set; }

        public IReadOnlyCollection<BarEntry> BarFileEntrys { get; set; }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(RootPath.Length);
            bw.Write(Encoding.Unicode.GetBytes(RootPath));
            bw.Write(NumberOfRootFiles);
            foreach (var barFileEntry in BarFileEntrys)
                bw.Write(barFileEntry.ToByteArray());
            return ms.ToArray();
        }
    }
}
