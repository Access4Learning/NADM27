using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Mvp.Xml.XInclude;
using GenerateSpec.Generator.Util;

namespace GenerateSpec.Generator
{
    /// <summary>
    /// Provides utility methods for handling input xml. Handles things such as namespaces, putting the separate input xml files into one big file, and remembering file location paths. 
    /// </summary>
    public class InputDocumentManager
    {
        DocumentGlobalSettings documentGlobalSettings;

        /// <summary>
        /// 
        /// </summary>
        public DocumentGlobalSettings DocumentGlobalSettings
        {
            get { return documentGlobalSettings; }
         
        }

        
        string inputDocumentPath;

        /// <summary>
        /// 
        /// </summary>
        public string InputDocumentPath
        {
            get { return inputDocumentPath; }
        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="documentSettings"></param>
        public InputDocumentManager(DocumentGlobalSettings documentSettings)
        {
            documentGlobalSettings = documentSettings;
            Initialize();
        }

        /// <summary>
        /// 
        /// </summary>
        public void BuildInput()
        {
            inputDocumentPath = BuildInput(documentGlobalSettings.InputPath);

        }

        /// <summary>
        /// 
        /// </summary>
        private void Initialize()
        {
            //these are namespaces added to the combined input document. No output docs are affected.  
            namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("xhtml", "http://www.w3.org/1999/xhtml");
            namespaceManager.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");

        }

        /// <summary>
        /// Turns the separate include files referenced in SIF.xml into a single xml file
        /// called SIF.input.xml.
        /// </summary>
        /// <param name="inputPath"></param>
        /// <returns></returns>
        private string BuildInput(string inputPath)
        {
            int index = inputPath.LastIndexOf('.');

            if (index == -1)
            {
                index = inputPath.Length;
            }

            string outputPath = inputPath.Insert(index, ".temp");

            XIncludingReader reader = new XIncludingReader(inputPath);
            reader.ExposeTextInclusionsAsCDATA = true;

            XmlTextWriter writer = new XmlTextWriter(outputPath, Encoding.UTF8);

            while (reader.Read())
            {
                WriteShallowNode(reader, writer);
            }

            reader.Close();
            writer.Close();


            string deletePath = outputPath; 

            outputPath = inputPath.Insert(index, ".input");

            XmlReader reader2 = new XmlTextReader(deletePath);
            writer = new XmlTextWriter(outputPath, Encoding.UTF8);

            bool skipped = false;

            while (true)
            {
                if (!skipped)
                {
                    if (!reader2.Read())
                    {
                        break;
                    }
                }

                skipped = false;

                if (reader2.NodeType == XmlNodeType.Element && reader2.LocalName == "if")
                {
                    if (reader2.GetAttribute("intl") != documentGlobalSettings.Intl)
                    {
                        reader2.Skip();
                        skipped = true;
                        continue;
                    }

                    continue;
                }

                if (reader2.NodeType == XmlNodeType.EndElement && reader2.LocalName == "if")
                {
                    continue;
                }

                WriteShallowNode(reader2, writer);
            }

            reader2.Close();
            writer.Close();

            File.Delete(deletePath);

            return outputPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        private void WriteShallowNode(XmlReader reader, XmlWriter writer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:

                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);

                    if (reader.IsEmptyElement)
                    {
                        writer.WriteEndElement();
                    }

                    break;

                case XmlNodeType.Text:

                    writer.WriteString(reader.Value);
                    break;

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:

                    writer.WriteWhitespace(reader.Value);
                    break;

                case XmlNodeType.CDATA:

                    //writer.WriteCData(reader.Value);
                    writer.WriteRaw(reader.Value);
                    break;

                case XmlNodeType.EntityReference:

                    writer.WriteEntityRef(reader.Name);
                    break;

                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:

                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;

                case XmlNodeType.DocumentType:

                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                    break;

                case XmlNodeType.Comment:

                    writer.WriteComment(reader.Value);
                    break;

                case XmlNodeType.EndElement:

                    writer.WriteFullEndElement();
                    break;
            }

        }
        
        XmlNamespaceManager namespaceManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nav"></param>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public XPathNodeIterator  Select(XPathNavigator nav, string xpath)
        {
            XPathExpression expression = nav.Compile(xpath);
            expression.SetContext(namespaceManager);
            return nav.Select(expression);
        }
    }
}
