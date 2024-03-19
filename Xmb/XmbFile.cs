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
        public class XmlString
        {
            public string? Content { get; set; }
            public int Size { get; set; }
        }

        static void ExtractStrings(XmlNode node, ref List<XmlString> elements, ref List<XmlString> attributes)
        {
            if (!elements.Any(x => x.Content == node.Name))
                elements.Add(new XmlString() { Content = node.Name, Size = elements.Count });

            foreach (XmlAttribute attr in node.Attributes!)
                if (!attributes.Any(x => x.Content == attr.Name))
                    attributes.Add(new XmlString() { Content = attr.Name, Size = attributes.Count });

            int count = node.ChildNodes.Count;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                    ExtractStrings(child, ref elements, ref attributes);
            }

        }

        static void WriteNode(ref BinaryWriter writer, XmlNode node, List<XmlString> elements, List<XmlString> attributes)
        {
            writer.Write((byte)88);
            writer.Write((byte)78);


            long Length_off = writer.BaseStream.Position;
            // length in bytes
            writer.Write(0);
            if (node.HasChildNodes)
            {
                if (node.FirstChild!.NodeType == XmlNodeType.Text)
                {

                    // innerTextLength
                    writer.Write(node.FirstChild.Value!.Length);
                    // innerText
                    if (node.FirstChild.Value.Length != 0)
                        writer.Write(Encoding.Unicode.GetBytes(node.FirstChild.Value));
                }
                else
                {
                    // innerTextLength
                    writer.Write(0);
                }
            }
            else
            {

                // innerTextLength
                writer.Write(0);

            }
            // nameID
            int NameID = elements.FirstOrDefault(x => x.Content == node.Name)!.Size;
            writer.Write(NameID);

            /*      int lineNum = 0;
                  for (int i = 0; i < elements.Count; i++)
                      if (elements[i].Content == node.Name)
                      {
                          lineNum = i;
                          break;
                      }*/
            // Line number ... need recount
            writer.Write(0);


            int NumAttributes = node.Attributes!.Count;
            // length attributes
            writer.Write(NumAttributes);
            for (int i = 0; i < NumAttributes; ++i)
            {

                int n = attributes.FirstOrDefault(x => x.Content == node.Attributes[i].Name)!.Size;
                // attrID
                writer.Write(n);
                // attributeLength
                writer.Write(node.Attributes[i].InnerText.Length);
                // attribute.InnerText
                writer.Write(Encoding.Unicode.GetBytes(node.Attributes[i].InnerText));
            }

            int NumChildren = 0;
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {

                if (node.ChildNodes[i]!.NodeType == XmlNodeType.Element)
                {
                    NumChildren++;

                }
            }
            // NumChildren nodes (recursively)
            writer.Write(NumChildren);
            for (int i = 0; i < node.ChildNodes.Count; ++i)
                if (node.ChildNodes[i]!.NodeType == XmlNodeType.Element)
                {

                    WriteNode(ref writer, node.ChildNodes[i]!, elements, attributes);

                }
            long NodeEnd = writer.BaseStream.Position;
            writer.BaseStream.Seek(Length_off, SeekOrigin.Begin);

            writer.Write((int)(NodeEnd - (Length_off + 4)));
            writer.BaseStream.Seek(NodeEnd, SeekOrigin.Begin);
        }

        public static async Task CreateXMBFileALZ4(string inputFileName, string outputFileName)
        {
            using var output = new MemoryStream();

            var writer = new BinaryWriter(output, Encoding.Default, true);

            writer.Write((byte)88);
            writer.Write((byte)49);

            writer.Write(0);

            writer.Write((byte)88);
            writer.Write((byte)82);
            writer.Write(4);
            writer.Write(8);


            XmlDocument file = new XmlDocument();
            file.Load(inputFileName);
            XmlNode rootElement = file.FirstChild!;


            // Get the list of element/attribute names, sorted by first appearance
            List<XmlString> ElementNames = [];
            List<XmlString> AttributeNames = [];
            await Task.Run(() =>
            {
                ExtractStrings(file.DocumentElement!, ref ElementNames, ref AttributeNames);

            });

            // Output element names
            int NumElements = ElementNames.Count;
            writer.Write(NumElements);
            for (int i = 0; i < NumElements; ++i)
            {
                writer.Write(ElementNames[i].Content!.Length);
                writer.Write(Encoding.Unicode.GetBytes(ElementNames[i].Content!));
            }

            int NumAttributes = AttributeNames.Count;
            writer.Write(NumAttributes);
            for (int i = 0; i < NumAttributes; ++i)
            {
                writer.Write(AttributeNames[i].Content!.Length);
                writer.Write(Encoding.Unicode.GetBytes(AttributeNames[i].Content!));
            }

            // Output root node, plus all descendants
            await Task.Run(() =>
            {
                WriteNode(ref writer, rootElement, ElementNames, AttributeNames);
            });


            // Fill in data-length field near the beginning
            long DataEnd = writer.BaseStream.Position;
            writer.BaseStream.Seek(2, SeekOrigin.Begin);
            int Length = (int)(DataEnd - (2 + 4));
            writer.Write(Length);
            writer.BaseStream.Seek(DataEnd, SeekOrigin.Begin);
            await Alz4Utils.CompressBytesAsAlz4Async(output.ToArray(), outputFileName);
        }
        #endregion
    }
}
