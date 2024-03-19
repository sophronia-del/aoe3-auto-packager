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

using System.Text;
using System.Text.Json.Serialization;

namespace aoe3_auto_packager
{
    public class BarEntry
    {
        public static async Task<BarEntry> Create(string rootPath, string filename, long offset)
        {
            BarEntry barEntry = new()
            {
                FileName = Path.GetRelativePath(rootPath, filename),
                Offset = offset,
                isCompressed = 0
            };

            int fileSize = (int)new FileInfo(filename).Length;
            barEntry.FileSize = fileSize;
            if (Alz4Utils.IsAlz4File(filename))
            {
                barEntry.isCompressed = 1;
                barEntry.FileSize = await Alz4Utils.ReadCompressedSizeAlz4Async(filename);
            }
            barEntry.FileSize2 = fileSize;
            barEntry.FileSize3 = fileSize;

            return barEntry;
        }

        [JsonPropertyName("compression")]
        public uint isCompressed { get; set; }
        [JsonIgnore]
        private string? FileName { get; set; }

        [JsonIgnore]
        public long Offset { get; set; }
        [JsonIgnore]
        public int FileSize { get; set; }
        [JsonPropertyName("size")]
        public int FileSize2 { get; set; }
        [JsonIgnore]
        public int FileSize3 { get; set; }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Offset);
            bw.Write(FileSize);
            bw.Write(FileSize2);
            bw.Write(FileSize3);
            bw.Write(FileName.Length);
            bw.Write(Encoding.Unicode.GetBytes(FileName));
            bw.Write(isCompressed);
            return ms.ToArray();
        }
    }
}
