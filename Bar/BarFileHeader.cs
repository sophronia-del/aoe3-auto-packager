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

namespace aoe3_auto_packager
{
    public class BarFileHeader
    {
        private static readonly string ESPN = "ESPN";
        public BarFileHeader(IReadOnlyCollection<string> fileInfos, string filename)
        {
            Unk1 = 0x44332211;
            Unk2 = new byte[66 * 4];
            Checksum = 0;
            NumberOfFiles = (uint)fileInfos.Count;
            Unk3 = 0;
            FilesTableOffset = 304 + fileInfos.Select(name => new FileInfo(name)).Sum(key => key.Length);
            FileNameHash = GetSuperFastHash(Encoding.Default.GetBytes(filename.ToUpper()));
        }

        public uint Unk1 { get; }

        public byte[] Unk2 { get; }

        public uint Checksum { get; }

        public uint NumberOfFiles { get; }

        public uint Unk3 { get; set; }


        public long FilesTableOffset { get; }

        public uint FileNameHash { get; }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(ESPN.ToCharArray());
            bw.Write(6);
            bw.Write(Unk1);
            bw.Write(Unk2);
            bw.Write(Checksum);
            bw.Write(NumberOfFiles);
            bw.Write(Unk3);
            bw.Write(FilesTableOffset);
            bw.Write(FileNameHash);
            return ms.ToArray();
        }
        private static uint GetSuperFastHash(byte[] dataToHash)
        {
            var dataLength = dataToHash.Length;
            if (dataLength == 0)
                return 0;

            // CUSTOMIZED --> Starts with 0, not with datalen
            // var hash = Convert.ToUInt32(dataLength);
            uint hash = 0;
            var remainingBytes = dataLength & 3; // mod 4
            var numberOfLoops = dataLength >> 2; // div 4
            var currentIndex = 0;
            while (numberOfLoops > 0)
            {
                hash += BitConverter.ToUInt16(dataToHash, currentIndex);
                var tmp = (uint)(BitConverter.ToUInt16(dataToHash, currentIndex + 2) << 11) ^ hash;
                hash = (hash << 16) ^ tmp;
                hash += hash >> 11;
                currentIndex += 4;
                numberOfLoops--;
            }

            switch (remainingBytes)
            {
                case 3:
                    hash += BitConverter.ToUInt16(dataToHash, currentIndex);
                    hash ^= hash << 16;
                    hash ^= (uint)dataToHash[currentIndex + 2] << 18;

                    hash += hash >> 11;
                    break;
                case 2:
                    hash += BitConverter.ToUInt16(dataToHash, currentIndex);
                    hash ^= hash << 11;
                    hash += hash >> 17;
                    break;
                case 1:
                    hash += dataToHash[currentIndex];
                    hash ^= hash << 10;
                    hash += hash >> 1;
                    break;
                // ReSharper disable once RedundantEmptySwitchSection
                default:
                    break;
            }

            /* Force "avalanching" of final 127 bits */
            hash ^= hash << 3;
            hash += hash >> 5;
            // CUSTOMIZED --> Altered avalanching part
            hash ^= hash << 2;
            hash += hash >> 15;
            hash ^= hash << 10;

            // Old Part:
            // hash ^= hash << 4;
            // hash += hash >> 17;
            // hash ^= hash << 25;
            // hash += hash >> 6;

            return hash;
        }
    }
}
