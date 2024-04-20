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
using System.Xml;

namespace aoe3_auto_packager
{
    public class XMBFile
    {
        #region Convert To XMB
        class XmlString
        {
            public string? Content { get; set; }
            public int Size { get; set; }
        }

        class NodeDetail
        {
            public long Offset { get; set; }
            public int Length { get; set; }
            public XmlNode Node { get; set; }
            public int NumChildren { get; set; }
            public NodeDetail? Parent { get; set; }
        }

        static void ExtractStrings(XmlNode node, ref Dictionary<string, XmlString> elements, ref Dictionary<string, XmlString> attributes)
        {
            elements.TryAdd(node.Name, new XmlString() { Content = node.Name, Size = elements.Count });

            foreach (XmlAttribute attr in node.Attributes!)
                attributes.TryAdd(attr.Name, new XmlString() { Content = attr.Name, Size = attributes.Count });

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                    ExtractStrings(child, ref elements, ref attributes);
            }

        }

        static int ExtractNodeDetails(NodeDetail? parent, XmlNode node, long offset, List<NodeDetail> collector, Dictionary<string, XmlString> elements, Dictionary<string, XmlString> attributes)
        {
            int currentLength = 2 + sizeof(int); // fixed header + byte length

            // innerTextLength
            currentLength += sizeof(int);
            if (node.HasChildNodes && node.FirstChild!.NodeType == XmlNodeType.Text)
            {
                currentLength += Encoding.Unicode.GetByteCount(node.FirstChild.Value!);
            }

            // nameID
            currentLength += 2 * sizeof(int);


            int NumAttributes = node.Attributes!.Count;
            // length attributes
            currentLength += sizeof(int);
            for (int i = 0; i < NumAttributes; ++i)
            {
                // attrID
                currentLength += sizeof(int);
                // attributeLength
                currentLength += sizeof(int);
                // attribute.InnerText
                currentLength += Encoding.Unicode.GetByteCount(node.Attributes[i].InnerText);
            }

            // Write later, put a placeholder here
            currentLength += sizeof(int);

            NodeDetail nodeDetail = new()
            {
                Offset = offset,
                Length = currentLength,
                Node = node,
                Parent = parent,
            };
            collector.Add(nodeDetail);

            int totalLength = currentLength;
            long nextOffset = offset + currentLength;
            foreach (XmlNode child in node.ChildNodes)
            {
                // root -> a1 -> b1
                //      -> a2 -> b2
                //            -> b3

                // direct
                // root n1
                // a1   n2
                // a2   n3
                // b1   n4
                // b2   n5
                // b3   n6

                // addition
                // b1   n(a1) += n(b1);   => n(a1) = n2 + n4,
                // b1   n(root) += n(b1); => n(root) = n1 + n4
                // a1   n(root) += n(a1); => n(root) = n1 + n4 + n2 + n4; #ERROR#

                if (child.NodeType == XmlNodeType.Element)
                {
                    int nextLength = ExtractNodeDetails(nodeDetail, child, nextOffset, collector, elements, attributes);
                    totalLength += nextLength;
                    nextOffset += nextLength;
                    nodeDetail.Length += nextLength;
                    nodeDetail.NumChildren++;
                }
            }

            return nodeDetail.Length;
        }

        static Task WriteNodeAsync(byte[] bytes, NodeDetail nodeDetail, Dictionary<string, XmlString> elements, Dictionary<string, XmlString> attributes)
        {
            return Task.Run(() =>
            {
                int cursor = (int)nodeDetail.Offset;
                bytes[cursor++] = (byte)'X';
                bytes[cursor++] = (byte)'N';

                // length in bytes
                BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), nodeDetail.Length - 6);
                cursor += sizeof(int);

                XmlNode node = nodeDetail.Node;
                if (node.HasChildNodes)
                {
                    if (node.FirstChild!.NodeType == XmlNodeType.Text)
                    {

                        // innerTextLength
                        BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), node.FirstChild.Value!.Length);
                        cursor += sizeof(int);
                        // innerText
                        if (node.FirstChild.Value.Length != 0) {
                            byte[] valueBytes = Encoding.Unicode.GetBytes(node.FirstChild.Value);
                            Array.Copy(valueBytes, 0, bytes, cursor, valueBytes.Length);
                            cursor += valueBytes.Length;
                        }
                        else
                        {
                            // innerTextLength
                            BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), 0);
                            cursor += sizeof(int);
                        }
                    }
                    else
                    {
                        // innerTextLength
                        BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), 0);
                        cursor += sizeof(int);
                    }
                }
                else
                {
                    // innerTextLength
                    BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), 0);
                    cursor += sizeof(int);
                }
                // nameID
                int NameID = elements[node.Name].Size;
                BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), NameID);
                cursor += sizeof(int);
                BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), 0);
                cursor += sizeof(int);


                int NumAttributes = node.Attributes!.Count;
                // length attributes
                BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), NumAttributes);
                cursor += sizeof(int);
                for (int i = 0; i < NumAttributes; ++i)
                {
                    int n = attributes[node.Attributes[i].Name].Size;
                    // attrID
                    BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), n);
                    cursor += sizeof(int);
                    // attributeLength
                    BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), node.Attributes[i].InnerText.Length);
                    cursor += sizeof(int);
                    // attribute.InnerText
                    byte[] valueBytes = Encoding.Unicode.GetBytes(node.Attributes[i].InnerText);
                    Array.Copy(valueBytes, 0, bytes, cursor, valueBytes.Length);
                    cursor += valueBytes.Length;
                }

                BitConverter.TryWriteBytes(new Span<byte>(bytes, cursor, sizeof(int)), nodeDetail.NumChildren);
            });
        }

        public static async Task CreateXMBFileALZ4(string inputFileName, string outputFileName)
        {
            using var output = new MemoryStream();

            var writer = new BinaryWriter(output, Encoding.Default, true);

            writer.Write((byte)'X');
            writer.Write((byte)'1');

            // Length
            writer.Write(0);

            writer.Write((byte)'X');
            writer.Write((byte)'R');
            writer.Write(4);
            writer.Write(8);


            XmlDocument file = new();
            file.Load(inputFileName);
            XmlNode rootElement = file.FirstChild!;


            // Get the list of element/attribute names, sorted by first appearance
            Dictionary<string, XmlString> elements = [];
            Dictionary<string, XmlString> attributes = [];
            List<NodeDetail> nodeDetails = [];

            int nodeByteCount = await Task.Run(() =>
            {
                ExtractStrings(file.DocumentElement!, ref elements, ref attributes);

                // Output element names
                int NumElements = elements.Count;
                writer.Write(NumElements);
                foreach (var key in elements.Keys)
                {
                    writer.Write(key.Length);
                    writer.Write(Encoding.Unicode.GetBytes(key));
                }

                int NumAttributes = attributes.Count;
                writer.Write(NumAttributes);
                foreach (var key in attributes.Keys)
                {
                    writer.Write(key.Length);
                    writer.Write(Encoding.Unicode.GetBytes(key));
                }

                return ExtractNodeDetails(null, file.DocumentElement!, output.Position, nodeDetails, elements, attributes);
            });
            output.Capacity = (int)output.Position + nodeByteCount;
            output.Seek(output.Capacity - 4, SeekOrigin.Begin);
            writer.Write(0);


            List<Task> encodeTasks = new(nodeDetails.Count);
            foreach (var nodeDetail in nodeDetails)
            {
                encodeTasks.Add(WriteNodeAsync(output.GetBuffer(), nodeDetail, elements, attributes));
            }

            await Task.WhenAll(encodeTasks);

            // Fill in data-length field near the beginning
            writer.BaseStream.Seek(2, SeekOrigin.Begin);
            writer.Write(output.Capacity - (2 + 4));
            await Alz4Utils.CompressBytesAsAlz4Async(output.ToArray(), outputFileName);
        }
        #endregion
    }
}
