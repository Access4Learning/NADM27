using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Drawing;

using Mvp.Xml;
using Mvp.Xml.XInclude;

namespace GenerateSpec.Generator
{
    /// <summary>
    /// Provides utility methods for handling input xml. 
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
            //The line below is temporarily commented out for the Web Services spec. The code will also be removed when Data Model and Infrastructure are separated.
            //This line is now permanently out (VP:1/31/2011).
            //UpdateInput();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Initialize()
        {
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

        /// <summary>
        /// No longer used. 
        /// </summary>
        private void UpdateInput()
        {
            XPathDocument doc = new XPathDocument(inputDocumentPath);
            XPathNavigator navigator = doc.CreateNavigator();

            string begin = "<!--BEGIN_COMMON_TYPES_COMPUTED-->";
            string end = "<!--END_COMMON_TYPES_COMPUTED-->";

            StreamReader reader = new StreamReader(inputDocumentPath);
            string contents = reader.ReadToEnd();
            reader.Close();

            StreamWriter writer = new StreamWriter(inputDocumentPath + ".bak", false);
            writer.Write(contents);
            writer.Close();

            int pos = contents.IndexOf(begin);

            string top = contents.Substring(0, pos + begin.Length);
            string bottom = contents.Substring(contents.IndexOf(end, pos));

            string update = "";

            {
                ArrayList subscribeObjectNames = new ArrayList();
                ArrayList provideObjectNames = new ArrayList();
                ArrayList responseObjectNames = new ArrayList();
                ArrayList exampleObjectNames = new ArrayList();

                XPathNodeIterator objectIterator = Select(navigator, "//xhtml:Section[@name='Data Model' or @name='Infrastructure']//xhtml:DataObject");

                while (objectIterator.MoveNext())
                {
                    string objectName = objectIterator.Current.GetAttribute("name", "");

                    XPathNodeIterator eventsReportedIterator = Select(objectIterator.Current, "./xhtml:EventsReported");

                    if (eventsReportedIterator.MoveNext())
                    {
                        //build SubscribeObjectNames array
                        string eventsReported = eventsReportedIterator.Current.Value.Trim();

                        if (eventsReported != "false")
                        {
                            subscribeObjectNames.Add(objectName);
                        }
                        else
                        {
                            //Is this incomplete?
                            //objectName = objectName;
                        }

                        //build ProvideObjectNames array
                        if (objectName != "SIF_ZoneStatus" && objectName != "SIF_AgentACL")
                        {
                            provideObjectNames.Add(objectName);
                        }

                        //build ResponseObjectNames array. They all get on the list.
                        responseObjectNames.Add(objectName);
                    }
                    else
                    {
                        //TODO: This should also be written to the log.
                        System.Console.WriteLine("no EventsReported for " + objectName + "!!!");
                    }

                    XPathNodeIterator supportsExampleIterator = Select(objectIterator.Current, "./xhtml:SupportsSIF_Example");

                    if (supportsExampleIterator.MoveNext())
                    {
                        string supportsExample = supportsExampleIterator.Current.Value.Trim();

                        if (supportsExample == "true")
                        {
                            exampleObjectNames.Add(objectName);
                        }
                    }
                }

                StringWriter exampleStringWriter = null;
                XmlTextWriter exampleWriter = null;

                subscribeObjectNames.Sort();
                provideObjectNames.Sort();
                responseObjectNames.Sort();
                exampleObjectNames.Sort();

                //Holy Shit.  This is code that inserts specific content into the output
                //and ignores the input xml file that contains these common types. 
                //After all the trouble to create a generic document generator this code
                //throws everything out the window. Not that there aren't a thousand
                //exceptions in the code that depend on specific content to work, e.g. 
                //objectName != "SIF_ZoneStatus" && objectName != "SIF_AgentACL"
                

                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "SIF_ProvideObjectNamesType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "SIF_ProvideObjectNamesType");
                    exampleWriter.WriteRaw("<Description>The SIF object names that can be specified in a <code>SIF_Provide</code> message.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");
                    exampleWriter.WriteStartElement("Values");

                    foreach (string objectName in provideObjectNames)
                    {
                        exampleWriter.WriteStartElement("Value");
                        exampleWriter.WriteElementString("Code", objectName);
                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }

                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "SIF_SubscribeObjectNamesType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "SIF_SubscribeObjectNamesType");
                    exampleWriter.WriteRaw("<Description>The SIF object names that can be specified in a <code>SIF_Subscribe</code> message.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");
                    exampleWriter.WriteStartElement("Values");

                    foreach (string objectName in subscribeObjectNames)
                    {
                        exampleWriter.WriteStartElement("Value");
                        exampleWriter.WriteElementString("Code", objectName);
                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }

                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "SIF_RequestObjectNamesType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "SIF_RequestObjectNamesType");
                    exampleWriter.WriteRaw("<Description>The SIF object names that can be specified in a <code>SIF_Request</code> message, or every SIF object name.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");
                    exampleWriter.WriteStartElement("Values");

                    foreach (string objectName in responseObjectNames)
                    {
                        exampleWriter.WriteStartElement("Value");
                        exampleWriter.WriteElementString("Code", objectName);
                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }

                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "SIF_ResponseObjectType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "SIF_ResponseObjectType");
                    exampleWriter.WriteRaw("<Description>The SIF objects that can be included in a <code>SIF_Response</code> message, or every SIF object.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");
                    exampleWriter.WriteStartElement("Choice");

                    foreach (string objectName in responseObjectNames)
                    {
                        exampleWriter.WriteStartElement("Type");
                        exampleWriter.WriteAttributeString("name", objectName);

                        if (objectName.IndexOf("Type") == objectName.Length - "Type".Length)
                        {
                            exampleWriter.WriteAttributeString("typed", "false");
                        }

                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }

                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "SIF_ResponseObjectsType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "SIF_ResponseObjectsType");
                    exampleWriter.WriteRaw("<Description>The SIF objects that can be included, repeated, in a <code>SIF_Response</code> message.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");
                    exampleWriter.WriteStartElement("Type");

                    exampleWriter.WriteStartElement("xs:choice");
                    exampleWriter.WriteAttributeString("minOccurs", "0");

                    foreach (string objectName in responseObjectNames)
                    {
                        exampleWriter.WriteStartElement("xs:sequence");
                        exampleWriter.WriteStartElement("xs:element");
                        exampleWriter.WriteAttributeString("ref", "sif:" + objectName);
                        exampleWriter.WriteAttributeString("maxOccurs", "unbounded");
                        exampleWriter.WriteEndElement();
                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }

                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "SIF_EventObjectType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "SIF_EventObjectType");
                    exampleWriter.WriteRaw("<Description>The SIF objects that can be included in a <code>SIF_Event</code> message.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");
                    exampleWriter.WriteStartElement("Choice");

                    foreach (string objectName in subscribeObjectNames)
                    {
                        exampleWriter.WriteStartElement("Type");
                        exampleWriter.WriteAttributeString("name", objectName);

                        if (objectName.IndexOf("Type") == objectName.Length - "Type".Length)
                        {
                            exampleWriter.WriteAttributeString("typed", "false");
                        }

                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }

                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "SIF_ExampleObjectType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "SIF_ExampleObjectType");
                    exampleWriter.WriteRaw("<Description>The SIF objects that can be included in <code>SIF_Query/SIF_Example</code>.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");

                    if (exampleObjectNames.Count > 0)
                    {
                        exampleWriter.WriteStartElement("Choice");

                        foreach (string objectName in exampleObjectNames)
                        {
                            exampleWriter.WriteStartElement("Type");
                            exampleWriter.WriteAttributeString("name", objectName);

                            if (objectName.IndexOf("Type") == objectName.Length - "Type".Length)
                            {
                                exampleWriter.WriteAttributeString("typed", "false");
                            }

                            exampleWriter.WriteEndElement();
                        }

                        exampleWriter.WriteEndElement();
                    }
                    else
                    {
                        exampleWriter.WriteStartElement("Type");
                        exampleWriter.WriteAttributeString("name", "EMPTY");
                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }

                if (documentGlobalSettings.Intl == "us" || documentGlobalSettings.Intl == "au")
                {
                    exampleStringWriter = new StringWriter();
                    exampleWriter = new XmlTextWriter(exampleStringWriter);
                    exampleWriter.Formatting = Formatting.Indented;

                    exampleWriter.WriteStartElement("CommonElement");
                    exampleWriter.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
                    exampleWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
                    exampleWriter.WriteAttributeString("name", "ReportDataObjectType");
                    exampleWriter.WriteAttributeString("type", "true");
                    exampleWriter.WriteStartElement("Item");
                    exampleWriter.WriteElementString("Element", "ReportDataObjectType");
                    exampleWriter.WriteRaw("<Description>The SIF objects that can be included in <code>SIF_ReportObject/ReportData</code>, plus <code>ReportPackage</code>.  When this infrastructure type is implemented with a data model, the enumeration will contain the appropriate objects from that data model.</Description>");
                    exampleWriter.WriteStartElement("Choice");

                    ArrayList reportObjectNames = new ArrayList(responseObjectNames);
                    reportObjectNames.Add("ReportPackage");

                    foreach (string objectName in reportObjectNames)
                    {
                        exampleWriter.WriteStartElement("Type");
                        exampleWriter.WriteAttributeString("name", objectName);

                        if (objectName.IndexOf("Type") == objectName.Length - "Type".Length)
                        {
                            exampleWriter.WriteAttributeString("typed", "false");
                        }

                        exampleWriter.WriteEndElement();
                    }

                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.WriteEndElement();
                    exampleWriter.Close();

                    string example = exampleStringWriter.ToString();

                    update += example;
                }
            }

            contents = top + update + bottom;

            writer = new StreamWriter(inputDocumentPath, false);
            writer.Write(contents);
            writer.Close();
        }
    }
}
