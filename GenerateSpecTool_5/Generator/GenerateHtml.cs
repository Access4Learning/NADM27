using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Drawing;
using GenerateSpec.Generator.Util;
using GenerateSpec.Tools;
using log4net; 


namespace GenerateSpec.Generator
{
    /// <summary>
    /// Generates the HTML document(s).
    /// </summary>
    public class GenerateHtml
    {
        // Create a logger for use in this class
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
           
        CodeComparer codeComparer = new CodeComparer();
        IndexNumberComparer indexNumberComparer = new IndexNumberComparer();

        string outputPath;

        /// <summary>
        /// 
        /// </summary>
        public string OutputPath
        {
            get { return outputPath; }
            set 
            { 
                outputPath = value;
                if (outputPath[outputPath.Length - 1] != '\\')
                {
                    outputPath += '\\';
                }
            }
        }

        string rootFileName;

        /// <summary>
        /// 
        /// </summary>
        public string RootFileName
        {
            get { return rootFileName; }
            set { rootFileName = value; }
        }

        bool singleDocument = false;

        /// <summary>
        /// 
        /// </summary>
        public bool SingleDocument
        {
            get { return singleDocument; }
            set { singleDocument = value; }
        }

        string documentType = "";

        /// <summary>
        ///Indicates what types of text to be included in the document output.  See the XSD for enumeration.
        /// </summary>
        public string DocumentType
        {
            get { return documentType; }
            set { documentType = value; }
        }
        

        DocumentGlobalSettings documentGlobalSettings;

        /// <summary>
        /// A data object used for storing and passing global settings.
        /// </summary>
        public DocumentGlobalSettings DocumentGlobalSettings
        {
            get { return documentGlobalSettings; }
        
        }

        /// <summary>
        /// The constructor for the GenerateHtml class. Takes two parameters and sets them equal to instance properties. 
        /// </summary>
        /// <param name="documentGlobalSettings">A data object that contains global settings.</param>
        /// <param name="documentManager">An object with properties and methods that manage the input xml.</param>
        public GenerateHtml(DocumentGlobalSettings documentGlobalSettings, InputDocumentManager documentManager)
        {
            this.documentGlobalSettings = documentGlobalSettings;
            inputDocumentManager = documentManager;
        }

        string nextSectionHack = "";

        /// <summary>
        /// Added by Mark Ward:
        /// NextSectionHack is used in the Navigation to provide the generator the next section after Data Model.  Right now the generator is not smart enough to determine this.
        /// </summary>
        public string NextSectionHack
        {
            get { return nextSectionHack; }
            set { nextSectionHack = value; }
        }

        string rootTitle;

        /// <summary>
        /// 
        /// </summary>
        public string RootTitle
        {
            get { return rootTitle; }
            set { rootTitle = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        private InputDocumentManager inputDocumentManager;


        /// <summary>
        /// 
        /// </summary>
        public InputDocumentManager InputDocumentManager
        {
            get { return inputDocumentManager; }
            
        }
      

        XmlTextWriter rootWriter;
        XmlTextWriter writer;
        XPathDocument doc;
        XPathNavigator navigator;
        //string section;
        int level;

        ArrayList quickContents;
        ArrayList fullContents;
        ArrayList examples;
        ArrayList tables;
        ArrayList figures;
        ArrayList objects;
        ArrayList commonElements;
        ArrayList commonTypes;
        ArrayList elements;
        ArrayList attributes;

        string docId;
        string rootDocId;
        string prevDocId;
        //string nextDocId;

        string path;
        string title;
 
        string exampleXML;
        string errorXML;


        /// <summary>
        /// This is the core organizing method of the html generation process. 
        /// The main pieces of the spec are put together in this method. 
        /// </summary>
        public void GenerateSpec()
        {
            //System.Console.WriteLine("Generating spec...");

            Initialize();

            //System.Console.WriteLine(CountNumberOfObjects() + " objects.");
            //System.Console.WriteLine(CountNumberOfElements() + " elements.");
            //System.Console.WriteLine(CountNumberOfAttributes() + " attributes.");

            //ListRepeatableElements();

            //the level variable tracks the depth we are in in the document.  Level 1 is treated differently.
            level = 1;
            //section = "1";

            quickContents = new ArrayList();
            fullContents = new ArrayList();
            tables = new ArrayList();
            examples = new ArrayList();
            figures = new ArrayList();
            objects = new ArrayList();
            commonElements = new ArrayList();
            commonTypes = new ArrayList();
            elements = new ArrayList();
            attributes = new ArrayList();

            //rootDocId: value stays = 'index' throughout. 
            //this could be setting the name of the root html document as index.html instead of
            //something else like main.html, etc. 
            rootDocId = "index";

            StartSpreadsheet(this.outputPath + @"temp\ImplementationSpecification.xml");

            //starts the an html document and creates new XMLTextWriter writer object
            StartDocument(this.outputPath + this.rootFileName, rootDocId, rootTitle, "Introduction");

            //rootWriter now has the same value(s) as writer that was created by StartDocument()
            //rootWriter and writer are tested and used in the methods following. 
            //not sure of the scope of these objects right now. 
            rootWriter = writer;

            GenerateTitlePage(level);

            writer.WriteElementString("hr", "");

            //This generates the bulk of the html document
            GenerateSections(level);

            //This generates the back matter: appendices, indexes, etc. 
            GenerateAppendices(level);

            //??
            EndDocument("");

            writer = rootWriter;

            if (!this.singleDocument)
            {
                //this is to fix a bug in IE that mangles following anchors when this
                //type of anchor is used
                //WriteAnchor("contents");
                writer.WriteStartElement("a");
                writer.WriteAttributeString("name", "contents");
                writer.WriteFullEndElement() ; 

                GenerateQuickContents();
                GenerateFullContents();
            }

            this.prevDocId = "";

            //
            EndDocument("Introduction");

            rootWriter.Close();

            if (this.singleDocument)
            {
                StringWriter contentsWriter = new StringWriter();
                writer = new XmlTextWriter(contentsWriter);

                GenerateQuickContents();
                GenerateFullContents();

                writer.Close();

                string contents = contentsWriter.ToString();

                string begin = "<!--BEGIN_CONTENTS-->";
                string end = "<!--END_CONTENTS-->";  //this string is not getting written to the single html file.

                StreamReader reader = new StreamReader(this.outputPath + this.rootFileName);
                string fileContents = reader.ReadToEnd();
                reader.Close();

                int pos = fileContents.IndexOf(begin);

                string top = fileContents.Substring(0, pos + begin.Length);
                string bottom = fileContents.Substring(fileContents.IndexOf(end, pos));

                string update = top + "<a name='" + this.rootDocId + "____contents'/>" + contents + bottom;

                StreamWriter streamWriter = new StreamWriter(this.outputPath + this.rootFileName, false);
                streamWriter.Write(update);
                streamWriter.Close();
            }

            EndSpreadsheet();
        }


        /// <summary>
        /// Constructor for ValidateExamples (plural) method.
        /// </summary>
        public void ValidateExamples()
        {
            
            //vp: The line below calls the validator with a specific XSD to use for validation
            //of the xml example. 
            ValidateExamples(this.outputPath + @"XSD\SIF_MessageNoIncludes\SIF_Message.xsd");
            //ValidateExamples(this.outputPath + @"XSD\DataModelNoIncludes\SIF_Message.xsd");
        }

       
        /// <summary>
        /// ValidateExamples method implementation.
        /// </summary>
        /// <param name="xsd"></param>
        void ValidateExamples(string xsd)
        {
            string outputMessage;

            outputMessage = "===== " + xsd + "=====";
            if (log.IsErrorEnabled) log.Error(outputMessage);
            Console.WriteLine(outputMessage);
            Console.WriteLine();

            Initialize();

            //vp: create an iterator on the xml input document. This is the raw xml that is
            //created by the inputDocumentManager. 
            //XPathNodeIterator exampleIterator = inputDocumentManager.Select(navigator, "//xhtml:Example[ @intl=false() or @intl='" + documentGlobalSettings.Intl + "' ]");
            XPathNodeIterator exampleIterator = inputDocumentManager.Select(navigator, "//xhtml:Example[(@intl=false() or @intl='" + documentGlobalSettings.Intl + "') and @skipValidate=false() ]");

            while (exampleIterator.MoveNext())
            {
                try
                {
                    outputMessage = "Validating..." + exampleIterator.Current.GetAttribute("name", "") + "...";
                    if (log.IsErrorEnabled) {
                        log.Error("====="); 
                        log.Error(outputMessage); 
                    }
                    Console.WriteLine();
                    Console.WriteLine(outputMessage);
                    
                    

                    XPathNodeIterator contentsIterator = inputDocumentManager.Select(exampleIterator.Current, "child::node()");

                    StringWriter stringWriter = new StringWriter();
                    XmlTextWriter textWriter = new XmlTextWriter(stringWriter);
                    textWriter.Formatting = Formatting.Indented;

                    while (contentsIterator.MoveNext())
                    {
                        if (contentsIterator.Current.NodeType != XPathNodeType.Element)
                        {
                            continue;
                        }

                        textWriter.WriteNode(new Developmentor.Xml.NavigatorReader(contentsIterator.Current), true);
                    }

                    textWriter.Close();

                    exampleXML = stringWriter.ToString();

                    int index = exampleXML.IndexOf(">");

                    if (index != -1)
                    {
                        int index2 = exampleXML.IndexOf("xmlns=");

                        if (index2 == -1 || index2 > index)
                        {
                            exampleXML = exampleXML.Insert(index, " xmlns=\"" + documentGlobalSettings.SifNamespace + "\"");
                        }
                    }

                    /*if (exampleXML.IndexOf("StudentAcademicRecord") == -1)
                    {
                        continue;
                    }*/

                    //System.Console.WriteLine(exampleXML);
                    errorXML = exampleXML;

                    if (exampleXML != "")
                    {
                        ValidateExample(xsd, exampleXML);
                    }
                    else
                    {
                        string exampleNotFound = "Example XML was empty";
                        Console.WriteLine(exampleNotFound);
                        if (log.IsErrorEnabled) log.Error(exampleNotFound);
                    }
                }

               
                catch (Exception ex)
                {

                    if (log.IsErrorEnabled) log.Error("Validation Example error", ex);
                    Console.WriteLine(ex.Message);
                }

                finally
                {
                    System.Console.WriteLine();
                }
            }
        }


        /// <summary>
        /// Validate a single example. 
        /// </summary>
        /// <param name="xsd"></param>
        /// <param name="xml"></param>
        void ValidateExample(string xsd, string xml)
        {
            // Set the validation settings.
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += new ValidationEventHandler(this.ShowValidationErrors); //set the callback function for logging errors
            settings.Schemas.Add(documentGlobalSettings.SifNamespace, xsd); //set the XSD that will be used

            //do this because XmlReader.Create is looking for a path to xml file rather than the xml itself
            //the .Create method detects that the input is a stream rather than a filename. 
            StringReader strReader = new StringReader(xml); 

            // Create the XmlReader object.
            XmlReader reader = XmlReader.Create(strReader, settings); 
  
            //parse the file
            while (reader.Read())
            {
                //System.Console.WriteLine(reader.Value);
            }
        }

        /*
        /// <summary>
        /// This is the old version of the above method. It was rewritten because it uses deprecated code. 
        /// </summary>
        /// <param name="xsd"></param>
        /// <param name="xml"></param>
        void ValidateExample(string xsd, string xml)
        {
            XmlValidatingReader reader = null;
            ValidationEventHandler eventHandler = new ValidationEventHandler(this.ShowCompileErrors);

            XmlParserContext context = new XmlParserContext(null, null, "", XmlSpace.None);



            reader = new XmlValidatingReader(xml, XmlNodeType.Document, context);
            reader.ValidationEventHandler += eventHandler;
            reader.Schemas.ValidationEventHandler += eventHandler;
            reader.ValidationType = ValidationType.Schema;

            reader.Schemas.Add(documentGlobalSettings.SifNamespace, xsd);


            while (reader.Read())
            {
                //System.Console.WriteLine(reader.Value);
            }
        } */

        /// <summary>
        /// Creates the html document header sections down to the body tag.
        /// Used once when generating a single file.  Used on each html page
        /// when the spec is generated as multiple pages. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="id"></param>
        /// <param name="title"></param>
        /// <param name="nextId"></param>
        void StartDocument(string path, string id, string title, string nextId)
        {
            this.prevDocId = this.docId;

            this.path = path;
            this.title = title;
            this.docId = id;

            if (this.singleDocument && writer != null) return;

            writer = new XmlTextWriter(new StreamWriter(path));
            writer.Formatting = Formatting.Indented;

            writer.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);
            writer.WriteStartElement("html");
            writer.WriteAttributeString("xmlns", "http://www.w3.org/1999/xhtml");
            writer.WriteStartElement("head");
            writer.WriteStartElement("meta");
            writer.WriteAttributeString("http-equiv", "Content-Type");
            writer.WriteAttributeString("content", "text/html;charset=utf-8");
            writer.WriteEndElement();
            writer.WriteElementString("title", title);
            writer.WriteStartElement("link");
            writer.WriteAttributeString("rel", "stylesheet");
            writer.WriteAttributeString("type", "text/css");
            writer.WriteAttributeString("href", "include/document.css");
            writer.WriteEndElement();
            writer.WriteStartElement("link");
            writer.WriteAttributeString("rel", "stylesheet");
            writer.WriteAttributeString("type", "text/css");
            writer.WriteAttributeString("href", "include/specification.css");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("body");

            if (this.singleDocument && docId != rootDocId) return;

            WriteTopNav();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void ShowValidationErrors(object sender, ValidationEventArgs args)
        {

            if (errorXML != null)
            {
                if (log.IsErrorEnabled) log.Error(errorXML);
                Console.WriteLine(errorXML);

                errorXML = null;
            }

            string outputMessage = args.Exception.SourceUri + ": " + args.Exception.LineNumber + ", " + args.Exception.LinePosition + ": " + args.Message;
            if (log.IsErrorEnabled) log.Error(outputMessage);
            Console.WriteLine(outputMessage);
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        void GenerateTitlePage(int level)
        {
            writer.WriteStartElement("div");
            writer.WriteAttributeString("class", "titlePage");

            XPathNodeIterator iterator = inputDocumentManager.Select(navigator, "//xhtml:TitlePage/*");

            while (iterator.MoveNext())
            {
                writer.WriteNode(new Developmentor.Xml.NavigatorReader(iterator.Current), true);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Uses the inputDocumentManager to get the sections in the spec.  It then
        /// iterates through the sections and builds the html by calling the GenerateSection method. 
        /// </summary>
        /// <param name="level"></param>
        void GenerateSections(int level)
        {
            int sectionNumber = 1;

            XPathNodeIterator sectionIterator = inputDocumentManager.Select(navigator, "/xhtml:SIFSpecification/xhtml:Section");

            while (sectionIterator.MoveNext())
            {
               
                string nextSectionID = "";

                if (level == 1)
                {
                    XPathNodeIterator nextSectionIterator = sectionIterator.Clone();

                    //if this is a data model document and we have an infrastructure section, then skip to 
                    //the next section. Because the Data Model document also generates all the 
                    //XSDs, we need to include the Infra Messages but not print the section. Added by Vince (8/29/2010).
                    if ((this.DocumentType == "DataModel") && (nextSectionIterator.Current.GetAttribute("sectionType", "") == "infrastructure"))
                    { continue; };

                    if (nextSectionIterator.MoveNext())
                    {
                        nextSectionID = GetID(nextSectionIterator.Current.GetAttribute("name", ""));
                    }
                }

                //call the method below with the sectionIterator pointer positioned at the current section
                GenerateSection(sectionIterator, level, sectionNumber.ToString(), nextSectionID);

                ++sectionNumber;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        void GenerateAppendices(int level)
        {
            int sectionNumber = 1;

            XPathNodeIterator sectionIterator = inputDocumentManager.Select(navigator, "/xhtml:SIFSpecification/xhtml:Appendix");

            while (sectionIterator.MoveNext())
            {
                char appendixLetter = (char)('A' + sectionNumber - 1);

                string nextSectionID = "";

                XPathNodeIterator nextSectionIterator = sectionIterator.Clone();

                if (nextSectionIterator.MoveNext())
                {
                    nextSectionID = GetID(nextSectionIterator.Current.GetAttribute("name", ""));
                }

                GenerateAppendix(sectionIterator, level, appendixLetter.ToString(), nextSectionID);

                ++sectionNumber;
            }
        }

        /// <summary>
        /// Closes out an .html document: writes EOF to disk. 
        /// </summary>
        /// <param name="nextId"></param>
        void EndDocument(string nextId)
        {
            if (this.singleDocument && this.docId != this.rootDocId) return;

            WriteBottomNav(nextId);

            if (this.singleDocument) return;

            writer.WriteEndElement();

            writer.WriteEndElement();

            if (writer != rootWriter)
            {
                writer.Close();
            }

            this.prevDocId = this.docId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        void WriteAnchor(string id)
        {
            writer.WriteStartElement("a");

            string anchor = id;

            if (this.singleDocument)
            {
                anchor = this.docId + "__" + anchor;
            }

            writer.WriteAttributeString("name", anchor);

            writer.WriteEndElement();
        }

        /// <summary>
        /// Generates a short TOC by calling the GenerateContents method with a parameter that specifies
        /// that the short TOC (called Quick Contents) be generated. The quickContents array (that holds
        /// the actual contents info) is also passed to the GenerateContents method.
        /// </summary>
        void GenerateQuickContents()
        {
            GenerateContents("Quick Table of Contents", quickContents);
        }



        /// <summary>
        /// Writes the Home/TOC navigation used at the top of each 
        /// page. 
        /// </summary>
        void WriteTopNav()
        {

            writer.WriteStartElement("div");
            writer.WriteAttributeString("class", "navigation");

            writer.WriteAttributeString("id", "topnavigation");

            if (!this.singleDocument)
            {
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", "index.html");
                writer.WriteString("home");
                writer.WriteEndElement();
            }

            writer.WriteStartElement("a");

            if (!this.singleDocument)
            {
                writer.WriteAttributeString("href", this.rootDocId + ".html#contents");
            }
            else
            {
                writer.WriteAttributeString("href", this.rootDocId + ".html#" + this.rootDocId + "____contents");
            }

            writer.WriteString("table of contents");
            writer.WriteEndElement();

            writer.WriteStartElement("hr");
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        /// <summary>
        /// Creates the Home/Previous/Next/TOC navigation used at the bottom of each 
        /// page. 
        /// </summary>
        /// <param name="nextId"></param>
        void WriteBottomNav(string nextId)
        {

            writer.WriteStartElement("div");
            writer.WriteAttributeString("class", "navigation");


            writer.WriteAttributeString("id", "bottomnavigation");

            writer.WriteStartElement("hr");
            writer.WriteEndElement();

            if (!this.singleDocument)
            {
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", "index.html");
                writer.WriteString("home");
                writer.WriteEndElement();

                if (this.prevDocId != null && this.prevDocId != "")
                {
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("href", this.prevDocId + ".html");
                    writer.WriteString("previous");
                    writer.WriteEndElement();
                }

                if (nextId != null && nextId != "")
                {
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("href", nextId + ".html");
                    writer.WriteString("next");
                    writer.WriteEndElement();
                }
            }

            writer.WriteStartElement("a");

            if (!this.singleDocument)
            {
                writer.WriteAttributeString("href", this.rootDocId + ".html#contents");
            }
            else
            {
                writer.WriteAttributeString("href", this.rootDocId + ".html#" + this.rootDocId + "____contents");
            }

            writer.WriteString("table of contents");
            writer.WriteEndElement();

            writer.WriteEndElement();

            writer.WriteRaw("<p align='center'><a href='http://validator.w3.org/check?uri=referer'><img src='http://www.w3.org/Icons/valid-xhtml10' alt='Valid XHTML 1.0 Transitional'/></a></p>");
 
        }


        /// <summary>
        /// ***NO LONGER USED*****
        /// Creates the Home/Previous/Next/TOC navigation used at the bottom of each 
        /// page. 
        /// </summary>
        /// <param name="nextId"></param>
        /// <param name="top"></param>
        void GenerateNavigation(string nextId, bool top)
        {

            writer.WriteStartElement("div");
            writer.WriteAttributeString("class", "navigation");

            if (!top)
            {
                writer.WriteAttributeString("id", "bottomnavigation");

                writer.WriteStartElement("hr");
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteAttributeString("id", "topnavigation");
            }

            if (!this.singleDocument)
            {
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", "index.html");
                writer.WriteString("home");
                writer.WriteEndElement();

                if (this.prevDocId != null && this.prevDocId != "")
                {
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("href", this.prevDocId + ".html");
                    writer.WriteString("previous");
                    writer.WriteEndElement();
                }

                if (nextId != null && nextId != "")
                {
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("href", nextId + ".html");
                    writer.WriteString("next");
                    writer.WriteEndElement();
                }
            }

            writer.WriteStartElement("a");

            if (!this.singleDocument)
            {
                writer.WriteAttributeString("href", this.rootDocId + ".html#contents");
            }
            else
            {
                writer.WriteAttributeString("href", this.rootDocId + ".html#" + this.rootDocId + "____contents");
            }

            writer.WriteString("table of contents");
            writer.WriteEndElement();

            if (top)
            {
                writer.WriteStartElement("hr");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            if (!top)
            {
                writer.WriteRaw("<p align='center'><a href='http://validator.w3.org/check?uri=referer'><img src='http://www.w3.org/Icons/valid-xhtml10' alt='Valid XHTML 1.0 Transitional'/></a></p>");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void GenerateFullContents()
        {
            GenerateContents("Full Table of Contents", fullContents);
        }

        /// <summary>
        /// 
        /// </summary>
        void Initialize()
        {
            //keyNames.Clear();

            rootWriter = writer = null;

            //read the xml document into an XPathDocument, then use the XPathDocument
            //to create an XPathNavigator object
            doc = new XPathDocument(inputDocumentManager.InputDocumentPath);
            navigator = doc.CreateNavigator();

            ReadBuildComment();
        }

        string buildComment;

        /// <summary>
        /// 
        /// </summary>
        void ReadBuildComment()
        {
            buildComment = "";

            XPathNodeIterator iterator = inputDocumentManager.Select(navigator, "//xhtml:BuildComment");

            if (iterator.MoveNext())
            {
                buildComment = iterator.Current.Value;
            }
        }

        XmlTextWriter ssWriter;
        StringWriter ssStringWriter;
        XmlTextWriter ssTempWriter = null;
        int ssRows;
        int ssCols;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        void StartSpreadsheet(string path)
        {
            ssWriter = new XmlTextWriter(new StreamWriter(path));
            ssWriter.Formatting = Formatting.Indented;

            ssWriter.WriteRaw("<?xml version = '1.0' encoding = 'UTF-8'?>");
            ssWriter.WriteRaw("<Workbook xmlns = 'urn:schemas-microsoft-com:office:spreadsheet' xmlns:html = 'http://www.w3.org/TR/REC-html40' xmlns:ss = 'urn:schemas-microsoft-com:office:spreadsheet' xmlns:x = 'urn:schemas-microsoft-com:office:excel' xmlns:o = 'urn:schemas-microsoft-com:office:office'>\r\n");
            ssWriter.WriteRaw("<DocumentProperties xmlns = 'urn:schemas-microsoft-com:office:office'>\r\n");
            ssWriter.WriteRaw("<Author>SIF Association</Author>\r\n");
            ssWriter.WriteRaw("<Company>SIF Association</Company>\r\n");
            ssWriter.WriteRaw("<Version/>\r\n");
            ssWriter.WriteRaw("</DocumentProperties>\r\n");
            ssWriter.WriteRaw("<OfficeDocumentSettings xmlns = 'urn:schemas-microsoft-com:office:office'>\r\n");
            ssWriter.WriteRaw("<DownloadComponents/>\r\n");
            ssWriter.WriteRaw("<LocationOfComponents HRef = ''/>\r\n");
            ssWriter.WriteRaw("</OfficeDocumentSettings>\r\n");
            ssWriter.WriteRaw("<ExcelWorkbook xmlns = 'urn:schemas-microsoft-com:office:excel'>\r\n");
            ssWriter.WriteRaw("<WindowHeight>9210</WindowHeight>\r\n");
            ssWriter.WriteRaw("<WindowWidth>15195</WindowWidth>\r\n");
            ssWriter.WriteRaw("<WindowTopX>0</WindowTopX>\r\n");
            ssWriter.WriteRaw("<WindowTopY>60</WindowTopY>\r\n");
            ssWriter.WriteRaw("<ProtectStructure>False</ProtectStructure>\r\n");
            ssWriter.WriteRaw("<ProtectWindows>False</ProtectWindows>\r\n");
            ssWriter.WriteRaw("</ExcelWorkbook>\r\n");
            ssWriter.WriteRaw("<Styles>\r\n");
            ssWriter.WriteRaw("<Style ss:Name = 'Normal' ss:ID = 'Default'>\r\n");
            ssWriter.WriteRaw("<Alignment ss:Vertical = 'Bottom'/>\r\n");
            ssWriter.WriteRaw("<Borders/>\r\n");
            ssWriter.WriteRaw("<Font/>\r\n");
            ssWriter.WriteRaw("<Interior/>\r\n");
            ssWriter.WriteRaw("<NumberFormat/>\r\n");
            ssWriter.WriteRaw("<Protection/>\r\n");
            ssWriter.WriteRaw("</Style>\r\n");
            ssWriter.WriteRaw("<Style ss:ID='s25'>\r\n");
            ssWriter.WriteRaw("<Alignment ss:Vertical='Top' ss:WrapText='1'/>\r\n");
            ssWriter.WriteRaw("</Style>\r\n");
            ssWriter.WriteRaw("</Styles>\r\n");
        }

        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheet()
        {
            ssWriter.WriteRaw("</Workbook>\r\n");

            ssWriter.Close();
            ssWriter = null;
        }

       

        

        /// <summary>
        /// Takes a name and strips all characters (spaces mostly) except letters, numbers, and underscores. 
        /// It also capitalizes the first letter after spaces in the name (camel case). 
        /// The returned Id is used mostly in anchor tags. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static string GetID(string name)
        {
            string result = "";

            bool lastWasSkipped = true;

            foreach (char c in name)
            {
                if (Char.IsLetter(c) || Char.IsDigit(c) || c == '_')
                {
                    if (lastWasSkipped && Char.IsLower(c))
                    {
                        result += Char.ToUpper(c);
                    }
                    else
                    {
                        result += c;
                    }

                    lastWasSkipped = false;
                }
                else
                {
                    lastWasSkipped = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Generates the html for a single section.  Has specialized code for handling Preamble,
        /// Infrastructure, and Data Model Sections.
        /// TODO - add a handler for Zone Services section
        /// </summary>
        /// <param name="section"></param>
        /// <param name="level"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="nextSection"></param>
        void  GenerateSection(XPathNodeIterator section, int level, string sectionNumber, string nextSection)
        {
            //only <Section> tags get to this point
            
            String sectionName = section.Current.GetAttribute("name", "");
            
            string displayName = sectionName;
            string displayID = GetID(displayName);

            string id = section.Current.GetAttribute("id", "");

            if (id != null && id != "")
            {
                displayID = GetID(id);
            }

            
            if (level == 1 && writer != rootWriter)
            {
                if (this.docId == "Introduction")
                {
                    this.prevDocId = "index";
                }
                //Close out the html document from the previous iteration?
                EndDocument(displayID);
            }

            //Do for all level 1's except for Preamble
            if (level == 1 && displayID != "Preamble")
            {
                //Start a new .html file
                StartDocument(new FileInfo(this.path).Directory + "\\" + displayID + ".html", displayID, this.rootTitle + " - " + displayName, nextSection);
            }

            WriteAnchor(displayID);

            AddContents(level, sectionNumber, displayName, displayID);

            writer.WriteElementString(GetHTag(level), sectionNumber + " " + displayName);

            WriteComments(section);

            XPathNodeIterator textIterator = inputDocumentManager.Select(section.Current, "./xhtml:Intro/child::node()");

            while (textIterator.MoveNext())
            {
                writer.WriteNode(new Developmentor.Xml.NavigatorReader(textIterator.Current), false);
            }


            //Do the Infrastructure Section
            if (level == 1 && sectionName == "Infrastructure")
            {
                StartSpreadsheetInfrastructure();

                GenerateInfrastructure(level + 1, sectionNumber);

                EndSpreadsheetInfrastructure();
            }

            //Do the Data Model Section
            else if (level == 1 && sectionName == "Data Model")
            {
                StartSpreadsheetDataModel();

                GenerateDataModel(section, level + 1, sectionNumber);

                EndSpreadsheetDataModel();
            }


           //Do the Zone Services Section
            else if (level == 1 && sectionName == "Zone Services")
            {
                StartSpreadsheetZoneServices();

                GenerateZoneServices(section, level, sectionNumber); 
                               
                EndSpreadsheetZoneServices();
            }
            
            //Do a Zone Services Data Objects Section
            //This is an exception caught at lower than level 1
            else if (sectionName == "Service Body Definitions")
            {

                int objectSectionNumber = 1;
                ArrayList objectNames = new ArrayList();
                XPathNodeIterator objectIterator = inputDocumentManager.Select(section.Current, ".//xhtml:DataObject/@name");
              

                while (objectIterator.MoveNext())
                {

                    objectNames.Add(objectIterator.Current.Value);
                }

                /*if (sort != "false")
                {
                    objectNames.Sort();
                }*/

                //Write the Element Table for the Data Object
                foreach (string objectName in objectNames)
                {
                    objectIterator = inputDocumentManager.Select(section.Current, ".//xhtml:DataObject[@name='" + objectName + "']");

                    GenerateElementTable(objectIterator, level, sectionNumber, objectSectionNumber);

                    ++objectSectionNumber;

                }
            }


            //Do a regular section or subsection
            //This is the meat of the method and recursively calls itself except for some
            //branching based upon special types of sections. 
            else
            {
                XPathNodeIterator contentsIterator = inputDocumentManager.Select(section.Current, "*[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                int nextSectionNumber = 1;
                int exampleNumber = 1;
                int tableNumber = 1;
                int figureNumber = 1;

                while (contentsIterator.MoveNext())
                {
                    if (contentsIterator.Current.LocalName == "Section")
                    {
                        GenerateSection(contentsIterator, level + 1, sectionNumber + "." + nextSectionNumber.ToString(), "");

                        ++nextSectionNumber;
                    }
                    else if (contentsIterator.Current.LocalName == "Example")
                    {
                        WriteExample(contentsIterator, sectionNumber, exampleNumber++, "");
                    }
                    else if (contentsIterator.Current.LocalName == "Message")
                    {
                        WriteMessage(contentsIterator, level + 1, sectionNumber + "." + tableNumber.ToString());

                        ++tableNumber;
                    }
                    else
                    {
                        string localName = contentsIterator.Current.LocalName;
                        string title = contentsIterator.Current.GetAttribute("title", "");

                        if (localName == "p")
                        {
                            XPathNodeIterator imgIterator = inputDocumentManager.Select(contentsIterator.Current, "xhtml:img");

                            if (imgIterator.MoveNext())
                            {
                                localName = "img";
                                title = imgIterator.Current.GetAttribute("title", "");
                            }
                        }

                        if (localName == "table" && title != "")
                        {
                            AddTable(writer, sectionNumber + "-" + tableNumber.ToString(), title);

                        }
                        else if (localName == "img" && title != "")
                        {
                            AddFigure(writer, sectionNumber + "-" + figureNumber.ToString(), title);
                        }

                        writer.WriteNode(new Developmentor.Xml.NavigatorReader(contentsIterator.Current), true);

                        if (localName == "table" && title != "")
                        {
                            WriteCaption(writer, "Table", sectionNumber + "-" + tableNumber.ToString(), title);

                            ++tableNumber;
                        }
                        else if (localName == "img" && title != "")
                        {
                            WriteCaption(writer, "Figure", sectionNumber + "-" + figureNumber.ToString(), title);

                            ++figureNumber;
                        }
                    }
                }
            }

            if (level == 1 && displayID == "Preamble")
            {
                writer.WriteRaw("<!--BEGIN_CONTENTS-->");
                writer.WriteRaw("<!--END_CONTENTS-->");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void StartSpreadsheetDataModel()
        {
            ssWriter.WriteRaw("<Worksheet ss:Name = 'Data Model'>\r\n");

            ssStringWriter = new StringWriter();
            ssTempWriter = new XmlTextWriter(ssStringWriter);
            ssRows = ssCols = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        void StartSpreadsheetInfrastructure()
        {
            ssWriter.WriteRaw("<Worksheet ss:Name = 'Infrastructure'>\r\n");

            ssStringWriter = new StringWriter();
            ssTempWriter = new XmlTextWriter(ssStringWriter);
            ssRows = ssCols = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        void StartSpreadsheetZoneServices()
        {
            ssWriter.WriteRaw("<Worksheet ss:Name = 'Zone Services'>\r\n");

            ssStringWriter = new StringWriter();
            ssTempWriter = new XmlTextWriter(ssStringWriter);
            ssRows = ssCols = 0;
        }


        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheetDataModel()
        {
            ssTempWriter.Close();
            ssTempWriter = null;

            ssWriter.WriteRaw("<Table x:FullRows = '1' x:FullColumns = '1' ss:ExpandedRowCount = '" + ssRows + "' ss:ExpandedColumnCount = '" + ssCols + "'>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='50'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='300'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='100'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw(ssStringWriter.ToString());
            ssWriter.WriteRaw("</Table>\r\n");

            EndSpreadsheetWorksheet();
        }



        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheetInfrastructure()
        {
            ssTempWriter.Close();
            ssTempWriter = null;

            ssWriter.WriteRaw("<Table x:FullRows = '1' x:FullColumns = '1' ss:ExpandedRowCount = '" + ssRows + "' ss:ExpandedColumnCount = '" + ssCols + "'>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='50'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='300'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='100'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw(ssStringWriter.ToString());
            ssWriter.WriteRaw("</Table>\r\n");

            EndSpreadsheetWorksheet();
        }


        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheetZoneServices()
        {
            ssTempWriter.Close();
            ssTempWriter = null;

            ssWriter.WriteRaw("<Table x:FullRows = '1' x:FullColumns = '1' ss:ExpandedRowCount = '" + ssRows + "' ss:ExpandedColumnCount = '" + ssCols + "'>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='50'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='300'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='100'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw(ssStringWriter.ToString());
            ssWriter.WriteRaw("</Table>\r\n");

            EndSpreadsheetWorksheet();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="appendixIterator"></param>
        /// <param name="level"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="nextAppendix"></param>
        void GenerateAppendix(XPathNodeIterator appendixIterator, int level, string sectionNumber, string nextAppendix)
        {
            string name = appendixIterator.Current.GetAttribute("name", "");
            string displayID = GetID(name);

            EndDocument(displayID);

            StartDocument(new FileInfo(this.path).Directory + "\\" + displayID + ".html", displayID, this.rootTitle + " - " + name, nextAppendix);

            WriteAnchor(displayID);

            AddContents(level, sectionNumber, name, displayID);

            writer.WriteElementString(GetHTag(level), "Appendix " + sectionNumber + ": " + name);

            if (name == "External Code Sets")
            {
                StartSpreadsheetExternalCodeSets();

                WriteCodeSetAppendix(appendixIterator, sectionNumber, level);

                EndSpreadsheetExternalCodeSets();
            }
            else if (name == "Code Sets")
            {
                StartSpreadsheetCodeSets();

                WriteCodeSetAppendix(appendixIterator, sectionNumber, level);

                EndSpreadsheetCodeSets();
            }
            /*
        else if (name == "Common Types")
        {
            WriteCommonTypesAppendix(appendixIterator, sectionNumber, level);
        }
             */
            else if (name == "Common Types")
            {
                StartSpreadsheetCommonTypes();

                WriteCommonTypesAppendix2(appendixIterator, sectionNumber, level);

                EndSpreadsheetCommonTypes();
            }
            else if (name == "Background/Supplementary Documentation (non-normative)")
            {
                XPathNodeIterator introIterator = inputDocumentManager.Select(appendixIterator.Current, "./xhtml:Intro/child::node()");

                while (introIterator.MoveNext())
                {
                    writer.WriteNode(new Developmentor.Xml.NavigatorReader(introIterator.Current), false);
                }

                string backgroundDirectory = this.outputPath + @"background\";

                writer.WriteStartElement("div");
                writer.WriteAttributeString("class", "directory");

                WriteBackgroundDirectory(backgroundDirectory, 1);

                writer.WriteEndElement();
            }
            else if (name == "Index of Tables")
            {
                WriteIndex(writer, ref tables);
            }
            else if (name == "Index of Examples")
            {
                WriteIndex(writer, ref examples);
            }
            else if (name == "Index of Figures")
            {
                WriteIndex(writer, ref figures);
            }
            else if (name == "Index of Objects")
            {
                //Write a note at the top of this index
                //writer.WriteStartElement("p");
                //writer.WriteStartElement("em");
                //writer.WriteString("Note: Objects marked 6.* are SIF Data Model Objects; objects marked 7.* are Service Body Definitions; and objects marked 5.* are SIF Infrastructure objects.");
                //writer.WriteEndElement();
               // writer.WriteEndElement();

                WriteIndex(writer, ref objects, true);
            }
            else if (name == "Index of Common Elements")
            {
                WriteIndex(writer, ref commonElements, true);
            }
            else if (name == "Index of Common Types")
            {
                WriteIndex(writer, ref commonTypes, true);
            }
            else if (name == "Index of Elements")
            {
                WriteMultiIndex(writer, ref elements);
            }
            else if (name == "Index of Attributes")
            {
                WriteMultiIndex(writer, ref attributes);
            }
            else
            {
                WriteGenericAppendix(appendixIterator, sectionNumber, level);
            }
        }

        /// <summary>
        /// Takes an array of TOC data and generates an html table of contents.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="contents"></param>
        void GenerateContents(string title, ArrayList contents)
        {
            writer.WriteStartElement("div");
            writer.WriteAttributeString("class", "contents");

            writer.WriteElementString("h1", title);

            foreach (ContentsItem item in contents)
            {
                if (item.number == "A")
                {
                    writer.WriteStartElement("br");
                    writer.WriteEndElement();
                }

                writer.WriteStartElement(GetHTag(item.level + 1));

                if (item.number != "")
                {
                    writer.WriteString(item.number + " ");
                }

                writer.WriteStartElement("a");

                string reference = "";

                if (!this.singleDocument)
                {
                    reference = item.docId + ".html";
                    reference += "#" + item.id;
                }
                else
                {
                    reference = "#" + item.docId + "__" + item.id;
                }

                writer.WriteAttributeString("href", reference);
                writer.WriteString(item.name);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds an item to the TOC and the Quick TOC arrays
        /// </summary>
        /// <param name="level"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        void AddContents(int level, string number, string name, string id)
        {
            ContentsItem item = new ContentsItem();
            item.docId = this.docId;
            item.id = id;
            item.name = name;
            item.level = level;
            item.number = number;

            if (level < 2)
            {
                quickContents.Add(item);
            }

            fullContents.Add(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        static string GetHTag(int level)
        {
            if (level >= 1 && level <= 6) return "h" + level.ToString() + "";

            if (level > 6) return "h6";

            return "h6";
        }

        /// <summary>
        /// Vince (3/3/2010): This does not seem to be used.
        /// Writes comments wrapped by a Comment tag out as an html list.
        /// </summary>
        /// <param name="itemIterator"></param>
        void WriteComments(XPathNodeIterator itemIterator)
        {
            XPathNodeIterator commentIterator = inputDocumentManager.Select(itemIterator.Current, "./xhtml:Comment");

            bool first = true;

            while (commentIterator.MoveNext())
            {
                if (first)
                {
                    writer.WriteStartElement("ol");
                    writer.WriteAttributeString("class", "comment");
                }

                writer.WriteStartElement("li");

                XPathNodeIterator contentIterator = inputDocumentManager.Select(commentIterator.Current, "child::node()");

                while (contentIterator.MoveNext())
                {
                    writer.WriteNode(new Developmentor.Xml.NavigatorReader(contentIterator.Current), false);
                }

                writer.WriteEndElement();

                first = false;
            }

            if (!first)
            {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void AddExample(XmlTextWriter writer, string number, string name)
        {
            AddIndexItem(writer, ref examples, "Example", number, name);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void AddTable(XmlTextWriter writer, string number, string name)
        {
            AddIndexItem(writer, ref tables, "Table", number, name);
        }


        /// <summary>
        /// 
        /// </summary>
        /// Adds the item to the Figures index
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void AddFigure(XmlTextWriter writer, string number, string name)
        {
            AddIndexItem(writer, ref figures, "Figure", number, name);
        }


        /// <summary>
        /// Adds the item to the Object Index.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void AddObject(XmlTextWriter writer, string number, string name)
        {
            AddIndexItem(writer, ref objects, "", number, name);
        }

        /// <summary>
        /// Adds an item to the Common Element Index.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void AddCommonElement(XmlTextWriter writer, string number, string name)
        {
            AddIndexItem(writer, ref commonElements, "", number, name);
        }

        /// <summary>
        /// Adds an item to the Common Type Index.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void AddCommonType(XmlTextWriter writer, string number, string name)
        {
            AddIndexItem(writer, ref commonTypes, "", number, name);
        }

        /// <summary>
        /// Adds an item to the Element Index.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        void AddElement(XmlTextWriter writer, string number, string name, string id)
        {
            AddIndexItem(writer, ref elements, "", number, name, id);
        }

        /// <summary>
        /// Adds an item to the Attribute Index. 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        void AddAttribute(XmlTextWriter writer, string number, string name, string id)
        {
            AddIndexItem(writer, ref attributes, "", number, name, id);
        }

        /// <summary>
        /// Generic routine to add an item to an index.  The index and the item are passed
        /// in as parameters. 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="index"></param>
        /// <param name="type"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        void AddIndexItem(XmlTextWriter writer, ref ArrayList index, string type, string number, string name, string id)
        {
            string indexName = type + " " + number;

            if (name != "")
            {
                indexName += ": " + name;
            }

            string indexID = GetID(indexName);

            if (id != "")
            {
                indexID = id;
            }
            else
            {
                WriteAnchor(indexID);
            }

            ContentsItem item = new ContentsItem();
            item.docId = this.docId;
            item.id = indexID;
            item.name = name;
            item.number = number;
            item.type = type;

            index.Add(item);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="type"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void WriteCaption(XmlTextWriter writer, string type, string number, string name)
        {
            writer.WriteStartElement("span");
            writer.WriteAttributeString("class", "caption");
            writer.WriteString(type + " " + number);

            if (name != "")
            {
                writer.WriteString(": " + name);
            }

            writer.WriteEndElement();

        }

        /// <summary>
        /// Creates the html Object Table in the spec that lists elements and attributes. 
        /// </summary>
        /// <param name="objectIterator"></param>
        /// <param name="level"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="objectSectionNumber"></param>
        void  GenerateElementTable(XPathNodeIterator objectIterator, int level, string sectionNumber, int objectSectionNumber)
        {
            //(VP) Even though an iterator is used here, it always contains just one object/member. 
            while (objectIterator.MoveNext())
            {
                string displayName = objectIterator.Current.GetAttribute("name", "").Trim();
                string displayID = GetID(displayName);

                writer.WriteStartElement("div");
                writer.WriteAttributeString("class", "element_section");

                //This is a hack to make the TOC data object references unique.  With the addition of Zone Services
                //we now have method names that are the same as data object names.  This is causing anchor tag links to break.
                //Therefore we are prepending "obj:" to the display Id to make them all have a different quasi-namespace
                //than all other types of anchor references. 
                displayID = "obj:" + displayID; 

                //Write the Anchor for the TOC (Index anchors are written separately)
                WriteAnchor(displayID);

                string displayNumber = sectionNumber + "." + objectSectionNumber.ToString();

                //Add the item to the TOC
                AddContents(level + 1, displayNumber, displayName, displayID);

                if (objectIterator.Current.LocalName == "DataObject")
                {
                    AddObject(writer, displayNumber, displayName);
                }
                else if (objectIterator.Current.LocalName == "CommonElement" && objectIterator.Current.GetAttribute("type", "") != "true")
                {
                    AddCommonElement(writer, displayNumber, displayName);
                }
                else if (objectIterator.Current.LocalName == "CommonElement" && objectIterator.Current.GetAttribute("type", "") == "true")
                {
                    AddCommonType(writer, displayNumber, displayName);
                }

                writer.WriteStartElement(GetHTag(level + 1));
                writer.WriteAttributeString("class", "element_name");
                writer.WriteString(displayNumber + " " + displayName);
                writer.WriteEndElement();

                WriteComments(objectIterator);

                bool wroteIntro = false;

                XPathNodeIterator textIterator = inputDocumentManager.Select(objectIterator.Current, "xhtml:Intro/child::node()");

                //Write the Introduction for the table to html
                while (textIterator.MoveNext())
                {
                    //For Testing
                    //writer.WriteComment("Writing the Intro for the Element Table");
                    
                    writer.WriteNode(new Developmentor.Xml.NavigatorReader(textIterator.Current), false);

                    wroteIntro = true;
                }


                //If no Introduction was written, look for the description of the root element
                //and use it to write a description for the table.
                if (!wroteIntro)
                {
                    XPathNodeIterator rootIterator = inputDocumentManager.Select(objectIterator.Current, "xhtml:Item[xhtml:Element='" + displayName + "' and (@intl=false() or @intl='" + documentGlobalSettings.Intl + "')]/xhtml:Description/child::node()");

                    while (rootIterator.MoveNext())
                    {
                        //For Testing
                        //writer.WriteComment("Writing the Description for the Element Table");

                        writer.WriteNode(new Developmentor.Xml.NavigatorReader(rootIterator.Current), false);
                    }
                }

                //Events Reported
                XPathNodeIterator objectsReportedIterator = inputDocumentManager.Select(objectIterator.Current, "xhtml:EventsReported");
                if (objectsReportedIterator.MoveNext())
                {
                    writer.WriteStartElement("p");
                    writer.WriteAttributeString("class", "emphasized_note");

                    if (objectsReportedIterator.Current.Value == "true")
                    {
                        writer.WriteString("SIF_Events are reported for this object.");
                    }
                    else if (objectsReportedIterator.Current.Value == "false")
                    {
                        writer.WriteString("SIF_Events are not reported for this object.");
                    }
                    else
                    {
                        objectsReportedIterator = inputDocumentManager.Select(objectsReportedIterator.Current, "child::node()");

                        while (objectsReportedIterator.MoveNext())
                        {
                            writer.WriteNode(new Developmentor.Xml.NavigatorReader(objectsReportedIterator.Current), false);
                        }
                    }

                    writer.WriteEndElement();
                }

                //--------------------------------------
                if (objectIterator.Current.LocalName == "DataObject")
                {
                    string groupId = this.docId;

                    if (groupId == "Infrastructure")
                    {
                        groupId = "InfrastructureWorkingGroup";
                    }

                    WriteBackgroundLink(this.docId, displayID);
                }

                //-------------Write the reference to the Graphic, then create Graphic in Temp directory-------------------------

                //here we substitute the filename (tempName) for the displayName b/c a colon is illegal in a filename
                string tempName = displayName;
                //check for a ':' in the name and remove
                if (tempName.Contains(":"))
                {
                    tempName = tempName.Replace(":", "");
                }

                AddFigure(writer, displayNumber + "-1", displayName);

                writer.WriteStartElement("div");
                writer.WriteAttributeString("class", "diagram_parent");
                writer.WriteAttributeString("align", "center");
                writer.WriteStartElement("a");
                writer.WriteAttributeString("target", "_blank");
                writer.WriteAttributeString("href", "diagrams/" + tempName + ".png");



                //Bitmap bitmap = new Bitmap(documentGlobalSettings.OutputDiagramPath + displayName + ".png");
                  Bitmap bitmap = new Bitmap(documentGlobalSettings.OutputDiagramPath + tempName + ".png");

                int bitmapWidth = bitmap.Width;
                int bitmapHeight = bitmap.Height;

                
                writer.WriteStartElement("img");
                writer.WriteAttributeString("class", "diagram");
                writer.WriteAttributeString("src", "diagrams/" + tempName + ".png");
                writer.WriteAttributeString("alt", displayName);
                writer.WriteAttributeString("width", bitmapWidth.ToString());
                writer.WriteAttributeString("height", bitmapHeight.ToString());
                writer.WriteAttributeString("usemap", "#" + displayName + "__map");
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();

                WriteCaption(writer, "Figure", displayNumber + "-1", displayName);

              //StreamReader reader = new StreamReader(documentGlobalSettings.OutputDiagramPath.Replace(@"\diagrams\", @"\temp\") + displayName + ".png.html");
                StreamReader reader = new StreamReader(documentGlobalSettings.OutputDiagramPath.Replace(@"\diagrams\", @"\temp\") + tempName + ".png.html");
                string map = reader.ReadToEnd();
                reader.Close();

                writer.WriteRaw(map);

                int tableNumber = 1;

                string displayTableNumber = displayNumber + "-" + tableNumber.ToString();

                AddTable(writer, displayTableNumber, displayName);

                XPathNodeIterator pkeyIterator = inputDocumentManager.Select(objectIterator.Current, "xhtml:Key");

                ArrayList pkeys = new ArrayList();

                while (pkeyIterator.MoveNext())
                {
                    pkeys.Add(pkeyIterator.Current.Value);
                }


                //-----------------Write the Elements Table---------------------

                /* no need to handle Choice and Sequence, as described in items */
                XPathNodeIterator elementIterator = inputDocumentManager.Select(objectIterator.Current, ".//xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                if (elementIterator.MoveNext())
                {
                    //start the html table 0-----------------------------
                    writer.WriteStartElement("table");
                    writer.WriteAttributeString("class", "definition");
                    writer.WriteStartElement("thead");
                    writer.WriteStartElement("tr");
                    /*
                    writer.WriteElementString("th", "Element");
                    writer.WriteElementString("th", "Attribute");
                     */
                    writer.WriteElementString("th", "\xA0");
                    writer.WriteElementString("th", "Element/@Attribute");

                    writer.WriteElementString("th", "Char");
                    writer.WriteElementString("th", "Description");
                    writer.WriteElementString("th", "Type");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteStartElement("tbody");

                    /* no need to handle Choice and Sequence, as described in items */
                    elementIterator = inputDocumentManager.Select(objectIterator.Current, ".//xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                    XPathNodeIterator exampleIterator;

                    string parentElementName = "";
                    string lastElementName = "";
                    int lastElementDepth = 0;

                    int itemNumber = 0;

                    //for each Item (usually an element)
                    while (elementIterator.MoveNext())
                    {
                        string[] ssRow = new string[6];

                        ssRow[0] = displayNumber;
                        ssRow[1] = displayName;
                        ssRow[2] = ssRow[3] = ssRow[4] = ssRow[5] = "";

                        if (itemNumber > 0)
                        {
                            ssRow[0] = "";
                            ssRow[1] = "";
                        }

                        ++itemNumber;

                        writer.WriteStartElement("tr");


                        //get a whole bunch of subparts of each item. 
                        XPathNodeIterator elementNameIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:Element");
                        XPathNodeIterator attributeNameIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:Attribute");
                        XPathNodeIterator characteristicsIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:Characteristics");
                        XPathNodeIterator descriptionIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:Description/child::node()");
                        XPathNodeIterator typeIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:Type");
                        XPathNodeIterator listIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:List");
                        XPathNodeIterator typesIterator = inputDocumentManager.Select(elementIterator.Current, "child::node()[local-name(.) = 'Choice' or local-name(.) = 'Union']");
                        exampleIterator = inputDocumentManager.Select(elementIterator.Current, ".//xhtml:ItemExample");

                        writer.WriteStartElement("td");
                        writer.WriteAttributeString("class", "indicators");

                        if (elementNameIterator.MoveNext() && elementNameIterator.Current.Value.Trim() != "")
                        {
                            string elementName = elementNameIterator.Current.Value;
                            string displayName2 = elementName;

                            //strippedElementName and eventually displayName2 are versions of the element name
                            //that have the '/' character removed
                            string strippedElementName = elementName.TrimStart(new char[] { '/' });

                            int elementDepth = elementName.Length - strippedElementName.Length;

                            if (elementDepth > lastElementDepth)
                            {
                                parentElementName = lastElementName;
                                displayName2 = parentElementName + "/" + strippedElementName;
                            }
                            else if (elementDepth < lastElementDepth)
                            {
                                while (elementDepth < lastElementDepth)
                                {
                                    int lastIndex = parentElementName.LastIndexOf('/');

                                    if (lastIndex != -1)
                                    {
                                        parentElementName = parentElementName.Remove(lastIndex, parentElementName.Length - lastIndex);
                                        displayName2 = parentElementName + "/" + strippedElementName;

                                        --lastElementDepth;
                                    }
                                    else
                                    {
                                        parentElementName = "";
                                        displayName2 = strippedElementName;

                                        break;
                                    }
                                }
                            }
                            else if (elementDepth == lastElementDepth)
                            {
                                displayName2 = strippedElementName;

                                if (parentElementName != "")
                                {
                                    displayName2 = parentElementName + "/" + displayName2;
                                }
                            }

                            ssRow[2] = displayName2;

                                            
                            //Add the key symbol
                            if (pkeys.Contains(displayName2))
                            {
                                writer.WriteStartElement("img");
                                writer.WriteAttributeString("src", "images/key.png");
                                writer.WriteAttributeString("alt", "key");
                                writer.WriteEndElement();

                                //ssRow[1] += "K";
                            }

                            writer.WriteString("\xA0");
                            writer.WriteEndElement();
                            writer.WriteStartElement("td");
                          

                            writer.WriteAttributeString("class", "element");

                            //***write the anchor tag for the row in the table ***
                            //when displayName2=displayName then we are at the top level otherwise we have nested elements separated by __
                            string anchorID = (displayName2 != displayName ? (GetID(displayName) + "__") : "") + GetID(displayName2.Replace("/", "__"));
                            WriteAnchor(anchorID);

                            AddElement(writer, displayTableNumber + "." + itemNumber, elementName.TrimStart('/'), anchorID);

                            int breakLength = 24;

                            if (displayName2.Length > breakLength && displayName2.IndexOf("/") != -1)
                            {
                                string[] parts = displayName2.Split(new char[] { '/' });
                                int partNumber = 0;
                                int currentLength = 0;

                                for (partNumber = 0; partNumber < parts.Length; ++partNumber)
                                {
                                    string part = parts[partNumber];

                                    writer.WriteString(part);
                                    currentLength += part.Length;

                                    if (partNumber + 1 < parts.Length)
                                    {
                                        writer.WriteString("/");
                                        currentLength += 1;
                                    }

                                    if (currentLength > breakLength)
                                    {
                                        writer.WriteRaw("<br/>");
                                        writer.WriteRaw("&#160;&#160;&#160;&#160;&#160;");
                                        currentLength = 0;
                                    }
                                }
                            }
                            else
                            {
                                if (displayName2 == displayName)
                                {
                                    writer.WriteStartElement("span");
                                    writer.WriteAttributeString("class", "rootElement");
                                }

                                writer.WriteString(displayName2);

                                if (displayName2 == displayName)
                                {
                                    writer.WriteEndElement();
                                }
                            }

                            lastElementName = displayName2;
                            lastElementDepth = elementDepth;
                        }
                        else
                        {
                            //writer.WriteAttributeString("class", "emptycell");
                        }

                        //writer.WriteEndElement();

                        //writer.WriteStartElement("td");

                        if (attributeNameIterator.MoveNext() && attributeNameIterator.Current.Value.Trim() != "")
                        {
                            writer.WriteString("@");

                            string seekKey = "@" + attributeNameIterator.Current.Value.Trim();

                            ssRow[2] = seekKey;

                            if (lastElementName != "" && lastElementName != displayName)
                            {
                                seekKey = lastElementName + "/" + seekKey;
                            }

                            if (pkeys.Contains(seekKey))
                            {
                                //if (ssRow[1].IndexOf("K") == -1) ssRow[1] += "K";

                                writer.WriteStartElement("br");
                                writer.WriteEndElement();
                                writer.WriteStartElement("img");
                                writer.WriteAttributeString("class", "key");
                                writer.WriteAttributeString("src", "images/key.png");
                                writer.WriteAttributeString("alt", "key");
                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();
                            writer.WriteStartElement("td");

                            writer.WriteAttributeString("class", "attribute");

                            string anchorID = (displayName != lastElementName ? GetID(displayName) + "__" : "") + GetID(lastElementName.Replace("/", "__")) + "___" + attributeNameIterator.Current.Value;
                            WriteAnchor(anchorID);

                            AddAttribute(writer, displayTableNumber + "." + itemNumber, attributeNameIterator.Current.Value, anchorID);

                            writer.WriteString(attributeNameIterator.Current.Value);
                        }
                        else
                        {
                            //writer.WriteAttributeString("class", "emptycell");
                        }

                        writer.WriteEndElement();

                        writer.WriteStartElement("td");

                        if (characteristicsIterator.MoveNext())
                        {
                            string characteristics = characteristicsIterator.Current.Value;
                            ssRow[3] = characteristics;

                            writer.WriteAttributeString("class", "characteristics");
                            writer.WriteString(characteristics);
                        }
                        else
                        {
                            writer.WriteAttributeString("class", "emptycell");
                            writer.WriteCharEntity((char)0xA0);
                        }

                        writer.WriteEndElement();

                        writer.WriteStartElement("td");
                        writer.WriteAttributeString("class", "description");

                        bool empty = true;
                        bool firstExample = true;

                        StringWriter tempSSWriter = new StringWriter();
                        XmlTextWriter tempWriter = new XmlTextWriter(tempSSWriter);

                        bool firstDescription = true;

                        //Handle the Description tag 
                        while (descriptionIterator.MoveNext())
                        {
                            writer.WriteNode(new Developmentor.Xml.NavigatorReader(descriptionIterator.Current), true);

                            if (!firstDescription)
                            {
                                tempWriter.WriteCharEntity('\n');
                                tempWriter.WriteCharEntity('\n');
                            }

                            StringWriter descStringWriter = new StringWriter();
                            XmlTextWriter descWriter = new XmlTextWriter(descStringWriter);

                            descWriter.WriteNode(new Developmentor.Xml.NavigatorReader(descriptionIterator.Current), true);

                            descWriter.Close();

                            tempWriter.WriteRaw(Textify(descStringWriter.ToString()));

                            firstDescription = false;
                        }

                        //Handle Examples
                        while (exampleIterator.MoveNext())
                        {
                            if (firstExample)
                            {
                                writer.WriteStartElement("p");
                                writer.WriteStartElement("span");
                                writer.WriteAttributeString("class", "title");
                                writer.WriteString("Examples");
                                writer.WriteEndElement();
                                writer.WriteStartElement("br");
                                writer.WriteEndElement();
                            }

                            writer.WriteStartElement("span");
                            writer.WriteAttributeString("class", "example");
                            writer.WriteString(exampleIterator.Current.Value);
                            writer.WriteEndElement();
                            writer.WriteStartElement("br");
                            writer.WriteEndElement();

                            firstExample = false;
                            empty = false;
                        }

                        if (!firstExample)
                        {
                            writer.WriteEndElement();
                        }

                        if (empty)
                        {
                            writer.WriteCharEntity((char)0xA0);
                        }

                        writer.WriteEndElement();

                        tempWriter.Close();
                        ssRow[4] = tempSSWriter.ToString();

                        writer.WriteStartElement("td");
                        //writer.WriteAttributeString("class", "type");

                        tempSSWriter = new StringWriter();
                        tempWriter = new XmlTextWriter(tempSSWriter);

                        bool typed = false;
                        bool firstValue = true;

                        if (!typed)
                        {
                            bool firstTypeValue = true;

                            string typeType = "";

                            if (typesIterator.MoveNext())
                            {
                                typeType = typesIterator.Current.LocalName.ToLower();
                                typeIterator = inputDocumentManager.Select(typesIterator.Current, "xhtml:Type");

                                writer.WriteAttributeString("class", "type"); /* for td */
                                typed = true;

                                writer.WriteStartElement("span");
                                writer.WriteAttributeString("class", "title");
                                writer.WriteString(typeType + " of:");
                                writer.WriteEndElement();
                                writer.WriteStartElement("br");
                                writer.WriteEndElement();
                                writer.WriteStartElement("br");
                                writer.WriteEndElement();

                                tempWriter.WriteString(typeType + " of:");
                                tempWriter.WriteCharEntity('\n');
                                tempWriter.WriteCharEntity('\n');
                            }

                            while (typeIterator.MoveNext())
                            {
                                string infra = typeIterator.Current.GetAttribute("infra", "");

                                if (!typed)
                                {
                                    writer.WriteAttributeString("class", "type"); /* for td */
                                }

                                if (!firstTypeValue)
                                {
                                    writer.WriteStartElement("br");
                                    writer.WriteEndElement();

                                    tempWriter.WriteCharEntity('\n');
                                }

                                string typeRef = typeIterator.Current.GetAttribute("ref", "");

                                if (typeRef != "")
                                {
                                    typeRef += ".html";
                                }

                                XPathNodeIterator typeIterator2 = typeIterator.Clone();
                                typeIterator2.Current.MoveToFirstChild();

                                string type = typeIterator.Current.GetAttribute("name", "");

                                XPathNodeIterator typeValuesIterator = inputDocumentManager.Select(typeIterator.Current, "xhtml:Values");
                                string _valuesType = "";

                                if (typeValuesIterator.MoveNext())
                                {
                                    _valuesType = typeValuesIterator.Current.GetAttribute("type", "");
                                }

                                if (type != "")
                                {
                                    if (_valuesType != "additional")
                                    {
                                        if (type.IndexOf(":") == -1)
                                        {
                                            if (typeType == "choice")
                                            {
                                                string anchorID = (displayName != lastElementName ? GetID(displayName) + "__" : "") + GetID(lastElementName.Replace("/", "__")) + "__" + GetID(type);

                                                if (infra == "true")
                                                {
                                                    anchorID = anchorID.Replace("Infrastructure", "");
                                                }

                                                WriteAnchor(anchorID);
                                            }

                                            if (infra == "true")
                                            {
                                                type = type.Replace("Infrastructure", "");
                                            }

                                            writer.WriteStartElement("a");
                                            writer.WriteAttributeString("href", typeRef + "#" + type);
                                            writer.WriteAttributeString("class", "type");
                                            writer.WriteString(type);
                                            writer.WriteEndElement();

                                            tempWriter.WriteString(type);
                                        }
                                        else if (type.IndexOf("xs:") == 0)
                                        {
                                            writer.WriteStartElement("a");
                                            writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#" + type.Substring("xs:".Length));
                                            writer.WriteAttributeString("class", "type");
                                            writer.WriteString(type);
                                            writer.WriteEndElement();

                                            tempWriter.WriteString(type);
                                        }
                                        else
                                        {
                                            writer.WriteStartElement("code");
                                            writer.WriteString(type);
                                            writer.WriteEndElement();

                                            tempWriter.WriteString(type);
                                        }
                                    }

                                    typeValuesIterator = inputDocumentManager.Select(typeIterator.Current, "xhtml:Values");

                                    if (typeValuesIterator.MoveNext())
                                    {
                                        XPathNodeIterator valueIterator = inputDocumentManager.Select(typeValuesIterator.Current, "xhtml:Value");
                                        _valuesType = typeValuesIterator.Current.GetAttribute("type", "");

                                        if (_valuesType != "additional")
                                        {
                                            writer.WriteStartElement("div");
                                            writer.WriteAttributeString("class", "type_indent");
                                        }

                                        writer.WriteStartElement("span");
                                        writer.WriteAttributeString("class", "title");

                                        if (_valuesType == "additional")
                                        {
                                            writer.WriteStartElement("br");
                                            writer.WriteEndElement();
                                            writer.WriteString("additional values:");

                                            tempWriter.WriteCharEntity('\n');
                                            tempWriter.WriteCharEntity('\n');
                                            tempWriter.WriteString("additional values:");
                                        }
                                        else if (_valuesType == "subset")
                                        {
                                            writer.WriteStartElement("br");
                                            writer.WriteEndElement();
                                            writer.WriteString("subset:");

                                            tempWriter.WriteCharEntity('\n');
                                            tempWriter.WriteCharEntity('\n');
                                            tempWriter.WriteString("subset:");
                                        }

                                        writer.WriteEndElement();
                                        writer.WriteStartElement("br");
                                        writer.WriteEndElement();

                                        tempWriter.WriteCharEntity('\n');
                                        tempWriter.WriteCharEntity('\n');

                                        writer.WriteStartElement("dl");

                                        while (valueIterator.MoveNext())
                                        {
                                            XPathNodeIterator codeIterator = inputDocumentManager.Select(valueIterator.Current, "xhtml:Code");
                                            XPathNodeIterator textIterator2 = inputDocumentManager.Select(valueIterator.Current, "xhtml:Text");
                                            XPathNodeIterator descriptionIterator2 = inputDocumentManager.Select(valueIterator.Current, "xhtml:Description");

                                            string valueValue = "";

                                            if (codeIterator.MoveNext())
                                            {
                                                writer.WriteElementString("dt", codeIterator.Current.Value);

                                                tempWriter.WriteString(codeIterator.Current.Value);
                                            }

                                            if (_valuesType != "subset")
                                            {
                                                if (textIterator2.MoveNext())
                                                {
                                                    if (valueValue.Length > 0) valueValue += (char)0x2014;
                                                    valueValue += textIterator2.Current.Value;
                                                }

                                                if (descriptionIterator2.MoveNext())
                                                {
                                                    if (valueValue.Length > 0) valueValue += (char)0x2014;
                                                    valueValue += descriptionIterator2.Current.Value;
                                                }
                                            }

                                            if (valueValue.Length > 0)
                                            {
                                                writer.WriteElementString("dd", valueValue);

                                                tempWriter.WriteString(" = ");
                                                tempWriter.WriteString(valueValue);
                                            }

                                            tempWriter.WriteCharEntity('\n');
                                        }

                                        writer.WriteEndElement();

                                        //writer.WriteStartElement("br");
                                        //writer.WriteEndElement();

                                        if (_valuesType != "additional")
                                        {
                                            writer.WriteEndElement();
                                        }
                                    }

                                    {
                                        XPathNodeIterator facetsIterator = inputDocumentManager.Select(elementIterator.Current, "./xhtml:Facets/*");

                                        string minInclusive = "";
                                        string maxInclusive = "";
                                        string minExclusive = "";
                                        string maxExclusive = "";
                                        string totalDigits = "";
                                        string fractionDigits = "";
                                        string length = "";
                                        string minLength = "";
                                        string maxLength = "";
                                        string pattern = "";

                                        while (facetsIterator.MoveNext())
                                        {
                                            string localName = facetsIterator.Current.LocalName;

                                            if (localName == "minInclusive")
                                            {
                                                minInclusive = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "maxInclusive")
                                            {
                                                maxInclusive = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            if (localName == "minExclusive")
                                            {
                                                minExclusive = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "maxExclusive")
                                            {
                                                maxExclusive = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "totalDigits")
                                            {
                                                totalDigits = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "fractionDigits")
                                            {
                                                fractionDigits = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "length")
                                            {
                                                length = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "minLength")
                                            {
                                                minLength = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "maxLength")
                                            {
                                                maxLength = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                            else if (localName == "pattern")
                                            {
                                                pattern = facetsIterator.Current.GetAttribute("value", "");
                                            }
                                        }

                                        if (minInclusive != "" || maxInclusive != "" || minExclusive != "" || maxExclusive != "" || totalDigits != "" || fractionDigits != "" || length != "" || minLength != "" || maxLength != "" || pattern != "")
                                        {
                                            writer.WriteStartElement("br");
                                            writer.WriteEndElement();

                                            tempWriter.WriteCharEntity('\n');
                                            tempWriter.WriteCharEntity('\n');

                                            writer.WriteStartElement("table");

                                            if (minInclusive != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-minInclusive");
                                                writer.WriteString("xs:minInclusive");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", minInclusive);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:minInclusive = " + minInclusive);
                                                tempWriter.WriteCharEntity('\n');
                                            }

                                            if (maxInclusive != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-maxInclusive");
                                                writer.WriteString("xs:maxInclusive");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", maxInclusive);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:maxInclusive = " + maxInclusive);
                                                tempWriter.WriteCharEntity('\n');
                                            }

                                            if (minExclusive != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-minExclusive");
                                                writer.WriteString("xs:minExclusive");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", minExclusive);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:minExclusive = " + minExclusive);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            if (maxExclusive != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-maxExclusive");
                                                writer.WriteString("xs:maxExclusive");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", maxExclusive);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:maxExclusive = " + maxExclusive);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            if (fractionDigits != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-fractionDigits");
                                                writer.WriteString("xs:fractionDigits");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", fractionDigits);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:fractionDigits = " + fractionDigits);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            if (totalDigits != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-totalDigits");
                                                writer.WriteString("xs:totalDigits");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", totalDigits);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:totalDigits = " + totalDigits);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            if (pattern != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-pattern");
                                                writer.WriteString("xs:pattern");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", pattern);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:pattern = " + pattern);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            if (length != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-length");
                                                writer.WriteString("xs:length");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", length);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:length = " + length);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            if (minLength != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-length");
                                                writer.WriteString("xs:minLength");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", minLength);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:minLength = " + minLength);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            if (maxLength != "")
                                            {
                                                writer.WriteStartElement("tr");
                                                writer.WriteStartElement("td");
                                                writer.WriteStartElement("a");
                                                writer.WriteAttributeString("href", "http://www.w3.org/TR/xmlschema-2/#rf-length");
                                                writer.WriteString("xs:maxLength");
                                                writer.WriteEndElement();
                                                writer.WriteEndElement();
                                                writer.WriteElementString("td", maxLength);
                                                writer.WriteEndElement();

                                                tempWriter.WriteString("xs:maxLength = " + maxLength);
                                                tempWriter.WriteCharEntity('\n');

                                            }

                                            writer.WriteEndElement();
                                        }
                                    }

                                }
                                else if (typeIterator2.Current.NodeType != XPathNodeType.Text)
                                {
                                    //	writer.WriteAttributeString("class", "type");

                                    writer.WriteStartElement("div");
                                    writer.WriteAttributeString("class", "type");

                                    StringWriter stringWriter = new StringWriter();
                                    XmlTextWriter textWriter = new XmlTextWriter(stringWriter);
                                    textWriter.Formatting = Formatting.Indented;
                                    textWriter.WriteNode(new Developmentor.Xml.NavigatorReader(typeIterator2.Current), true);
                                    textWriter.Close();

                                    string writtenType = stringWriter.ToString().Replace(" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"", "");

                                    writer.WriteString(writtenType);

                                    tempWriter.WriteString(writtenType);

                                    writer.WriteEndElement();
                                }
                                else
                                {
                                    System.Console.WriteLine("???");
                                }

                                typed = true;

                                firstTypeValue = false;
                            }
                        }

                        string valuesType = "";

                        XPathNodeIterator valuesIterator = inputDocumentManager.Select(elementIterator.Current, "./xhtml:Values");

                        if (valuesIterator.MoveNext())
                        {
                            valuesType = valuesIterator.Current.GetAttribute("type", "");
                        }

                        bool wasntTyped = !typed;

                        if (!typed || valuesType == "additional" || valuesType == "subset")
                        {
                            typed = false;

                            XPathNodeIterator valueIterator = inputDocumentManager.Select(valuesIterator.Current, "xhtml:Value");

                            while (valueIterator.MoveNext())
                            {
                                if (firstValue)
                                {
                                    if (wasntTyped)
                                    {
                                        writer.WriteAttributeString("class", "type"); /* for td */
                                    }

                                    writer.WriteStartElement("span");
                                    writer.WriteAttributeString("class", "title");

                                    if (valuesType == "additional")
                                    {
                                        writer.WriteStartElement("br");
                                        writer.WriteEndElement();
                                        writer.WriteStartElement("br");
                                        writer.WriteEndElement();
                                        writer.WriteString("additional values:");

                                        tempWriter.WriteCharEntity('\n');
                                        tempWriter.WriteCharEntity('\n');
                                        tempWriter.WriteString("additional values:");
                                    }
                                    else if (valuesType == "subset")
                                    {
                                        writer.WriteStartElement("br");
                                        writer.WriteEndElement();
                                        writer.WriteStartElement("br");
                                        writer.WriteEndElement();
                                        writer.WriteString("subset:");

                                        tempWriter.WriteCharEntity('\n');
                                        tempWriter.WriteCharEntity('\n');
                                        tempWriter.WriteString("subset:");
                                    }
                                    else
                                    {
                                        writer.WriteString("values:");

                                        tempWriter.WriteString("values:");
                                    }

                                    writer.WriteEndElement();
                                    writer.WriteStartElement("br");
                                    writer.WriteEndElement();

                                    tempWriter.WriteCharEntity('\n');
                                    tempWriter.WriteCharEntity('\n');

                                    writer.WriteStartElement("dl");
                                }

                                XPathNodeIterator codeIterator = inputDocumentManager.Select(valueIterator.Current, "xhtml:Code");
                                XPathNodeIterator textIterator2 = inputDocumentManager.Select(valueIterator.Current, "xhtml:Text");
                                XPathNodeIterator descriptionIterator2 = inputDocumentManager.Select(valueIterator.Current, "xhtml:Description");

                                string valueValue = "";

                                if (codeIterator.MoveNext())
                                {
                                    writer.WriteElementString("dt", codeIterator.Current.Value);

                                    tempWriter.WriteString(codeIterator.Current.Value);
                                }

                                if (valuesType != "subset")
                                {
                                    if (textIterator2.MoveNext())
                                    {
                                        if (valueValue.Length > 0) valueValue += (char)0x2014;
                                        valueValue += textIterator2.Current.Value;
                                    }

                                    if (descriptionIterator2.MoveNext())
                                    {
                                        if (valueValue.Length > 0) valueValue += (char)0x2014;
                                        valueValue += descriptionIterator2.Current.Value;
                                    }
                                }

                                if (valueValue.Length > 0)
                                {
                                    writer.WriteElementString("dd", valueValue);

                                    tempWriter.WriteString(" = " + valueValue);
                                }

                                tempWriter.WriteCharEntity('\n');

                                firstValue = false;
                                typed = true;
                            }

                            if (typed)
                            {
                                writer.WriteEndElement();
                            }
                        }

                        if (wasntTyped && !typed)
                        {
                            //Handle the <List> tag in the input XML
                            //deal with elements that are SIF Lists or ActionLists.  There are two modes:list, actionlist.
                            if (listIterator.MoveNext())
                            {
                                writer.WriteAttributeString("class", "type");

                                string mode = listIterator.Current.GetAttribute("mode", "");

                                writer.WriteStartElement("a");
                                writer.WriteAttributeString("class", "type");
                                //create the link to the list or actionlist description in the spec.
                                writer.WriteAttributeString("href", "DataModel.html#" + GetID(mode));

                                writer.WriteString(mode);

                                tempWriter.WriteString(mode);

                                XPathNodeIterator keyIterator = inputDocumentManager.Select(listIterator.Current, "xhtml:Key");

                                string keyString = "";

                                while (keyIterator.MoveNext())
                                {
                                    if (keyString.Length == 0)
                                    {
                                        keyString += "(" + keyIterator.Current.Value;
                                    }
                                    else
                                    {
                                        keyString += ", " + keyIterator.Current.Value;
                                    }
                                }

                                if (keyString.Length > 0)
                                {
                                    keyString += ")";

                                    //if (mode == "ActionList")
                                    {
                                        writer.WriteString(" " + keyString);

                                        tempWriter.WriteString(" " + keyString);
                                    }
                                }

                                writer.WriteEndElement();
                            }
                            else
                            {
                                XPathNodeIterator externalTypeIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:ExternalType");

                                if (externalTypeIterator.MoveNext())
                                {
                                    string type = externalTypeIterator.Current.GetAttribute("type", "");
                                    string name = externalTypeIterator.Current.GetAttribute("name", "");
                                    string prefix = externalTypeIterator.Current.GetAttribute("prefix", "");

                                    writer.WriteAttributeString("class", "type");
                                    writer.WriteStartElement("div");
                                    writer.WriteAttributeString("class", "type");
                                    writer.WriteString(prefix + ":" + name);
                                    writer.WriteEndElement();

                                    tempWriter.WriteString(prefix + ":" + name);
                                }
                                else
                                {
                                    writer.WriteAttributeString("class", "emptycell");
                                    writer.WriteCharEntity((char)0xA0);
                                }
                            }
                        }

                        writer.WriteEndElement();

                        writer.WriteEndElement();

                        tempWriter.Close();
                        ssRow[5] = tempSSWriter.ToString();

                        WriteSpreadsheetRow(ssRow);
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    WriteCaption(writer, "Table", displayNumber + "-" + tableNumber.ToString(), displayName);

                    ++tableNumber;

                    exampleIterator = inputDocumentManager.Select(objectIterator.Current, "child::node()[local-name(.)='Section' or local-name(.)='Example']");

                    int exampleNumber = 1;
                    int subsectionNumber = 1;

                    while (exampleIterator.MoveNext())
                    {
                        if (exampleIterator.Current.LocalName == "Example")
                        {
                            WriteExample(exampleIterator, displayNumber, exampleNumber++, displayName);
                        }
                        else
                        {
                            GenerateSection(exampleIterator, level + 2, displayNumber + "." + subsectionNumber++, "");
                        }
                    }
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="section"></param>
        /// <param name="level"></param>
        /// <param name="sectionNumber"></param>
        void GenerateDataModel(XPathNodeIterator section, int level, string sectionNumber)
        {
            int nextSectionNumber = 1;

            XPathNodeIterator sectionIterator = inputDocumentManager.Select(section.Current, "xhtml:Section");

            //this is a recursive call to GenerateSection
            while (sectionIterator.MoveNext())
            {
                GenerateSection(sectionIterator, level, sectionNumber + "." + nextSectionNumber.ToString(), "");

                ++nextSectionNumber;
            }

            //Build an array of groups
            //Groups represent the WorkGroups, Task Forces, and Committee that maintain a specific
            //section of the spec. 
            ArrayList groups = new ArrayList();

            XPathNodeIterator groupIterator = inputDocumentManager.Select(navigator, "//xhtml:Group/@name");
            XPathNodeIterator objectIterator;

            while (groupIterator.MoveNext())
            {
                groups.Add(groupIterator.Current.Value);
            }

            groups.Sort();

            /*
            //
            //Hard code for treating Common Elements like a group
            //
            //

            int elementSectionNumber = 1;

            string displayName = "Common Elements";
            string displayID = GetID(displayName);

            WriteAnchor(displayID);

            string displayNumber = sectionNumber + "." + nextSectionNumber.ToString();

            AddContents(level, displayNumber, displayName, displayID);

            displayName = displayNumber + " " + displayName;

            writer.WriteElementString(GetHTag(level), displayName);

            //Build an array of common elements
            XPathNodeIterator commonElementIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Data Model']/xhtml:CommonElements/xhtml:CommonElement/@name");

            ArrayList commonElements = new ArrayList();

            while (commonElementIterator.MoveNext())
            {
                //System.Console.WriteLine(commonElementIterator.Current.Value);

                commonElements.Add(commonElementIterator.Current.Value);
            }

            commonElements.Sort();

            foreach (string commonElement in commonElements)
            {
                //writer.WriteElementString(GetHTag(level), sectionNumber + "." + nextSectionNumber.ToString() + "." + elementSectionNumber.ToString() + " " + commonElement);

                objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Data Model']/xhtml:CommonElements/xhtml:CommonElement[@name='" + commonElement + "']");

                GenerateElementTable(objectIterator, level, sectionNumber + "." + nextSectionNumber.ToString(), elementSectionNumber);

                ++elementSectionNumber;
            }

            */
            //end of hardcode section for Common Elements

            ++nextSectionNumber;

            ArrayList nextGroups = new ArrayList();

            bool firstGroup = true;

            foreach (string group in groups)
            {
                /*
                 * Skip the first item in the list when making the copy.
                 * This will make the first item in the nextGroup to be the next item of the original list
                 */
                if (!firstGroup)
                {
                    nextGroups.Add(group);
                }

                firstGroup = false;
            }

            nextGroups.Add("CommonTypes");

            int groupNumber = 0;

            foreach (string group in groups)
            {
                groupIterator = inputDocumentManager.Select(navigator, "//xhtml:Group[@name='" + group + "']");

                while (groupIterator.MoveNext())
                {
                    string displayName2 = groupIterator.Current.GetAttribute("name", "");
                    string sort = groupIterator.Current.GetAttribute("sort", "");
                    string displayID2 = GetID(displayName2);

                    WriteAnchor(displayID2);

                    string displayNumber2 = sectionNumber + "." + nextSectionNumber.ToString();

                    //close out the previour html document
                    //in the Data Model Section each Group gets a separate html file. 
                    EndDocument(displayID2);

                    StartDocument(new FileInfo(this.path).Directory + "\\" + displayID2 + ".html", displayID2, this.rootTitle + " - " + displayName2, GetID((string)nextGroups[groupNumber++]));

                    AddContents(level, displayNumber2, displayName2, displayID2);

                    displayName2 = displayNumber2 + " " + displayName2;

                    writer.WriteElementString(GetHTag(level), displayName2);

                    WriteComments(groupIterator);


                    //------------------------------------------------------

                    XPathNodeIterator textIterator = inputDocumentManager.Select(groupIterator.Current, "./xhtml:Intro/child::node()");

                    while (textIterator.MoveNext())
                    {
                        writer.WriteNode(new Developmentor.Xml.NavigatorReader(textIterator.Current), false);
                    }

                    WriteBackgroundLink(displayID2, "");


                    //----------------------------------------

                    int objectSectionNumber = 1;

                    ArrayList objectNames = new ArrayList();

                    //----------------------------------------------

                    objectIterator = inputDocumentManager.Select(groupIterator.Current, "./xhtml:CommonElement/@name");

                    while (objectIterator.MoveNext())
                    {
                        objectNames.Add(objectIterator.Current.Value);
                    }

                    if (sort != "false")
                    {
                        objectNames.Sort();
                    }

                    foreach (string objectName in objectNames)
                    {
                        //System.Console.WriteLine(objectName);

                        objectIterator = inputDocumentManager.Select(groupIterator.Current, "./xhtml:CommonElement[@name='" + objectName + "']");

                        GenerateElementTable(objectIterator, level, sectionNumber + "." + nextSectionNumber.ToString(), objectSectionNumber);

                        ++objectSectionNumber;
                    }

                    objectNames.Clear();



                    //------------------------------------

                    objectIterator = inputDocumentManager.Select(groupIterator.Current, ".//xhtml:DataObject/@name");

                    while (objectIterator.MoveNext())
                    {
                        objectNames.Add(objectIterator.Current.Value);
                    }

                    if (sort != "false")
                    {
                        objectNames.Sort();
                    }

                    //Write the Element Table for the Data Object
                    foreach (string objectName in objectNames)
                    {
                        //System.Console.WriteLine(objectName);

                        objectIterator = inputDocumentManager.Select(groupIterator.Current, ".//xhtml:DataObject[@name='" + objectName + "']");

                        GenerateElementTable(objectIterator, level, sectionNumber + "." + nextSectionNumber.ToString(), objectSectionNumber);

                        ++objectSectionNumber;
                    }

                    ++nextSectionNumber;
                }
            }
        }


        /// <summary>
        /// Generates the document layout and content for the Common Elements, Messages, and Data Objects of the
        /// Infrastructure Chapter.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="sectionNumber"></param>
        void GenerateInfrastructure(int level, string sectionNumber)
        {
            int nextSectionNumber = 1;


            //INFRASTRUCTURE COMMON ELEMENTS
            int elementSectionNumber = 1;

            string displayName = "Common Elements";
            string displayID = GetID(displayName);

            WriteAnchor(displayID);

            string displayNumber = sectionNumber + "." + nextSectionNumber.ToString();

            AddContents(level, displayNumber, displayName, displayID);

            displayName = displayNumber + " " + displayName;

            writer.WriteElementString(GetHTag(level), displayName);

            XPathNodeIterator commonElementIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:CommonElements/xhtml:CommonElement/@name");

            ArrayList commonElements = new ArrayList();

            while (commonElementIterator.MoveNext())
            {
                //System.Console.WriteLine(commonElementIterator.Current.Value);

                commonElements.Add(commonElementIterator.Current.Value);
            }

            //commonElements.Sort();

            XPathNodeIterator objectIterator;

            foreach (string commonElement in commonElements)
            {
                //writer.WriteElementString(GetHTag(level), sectionNumber + "." + nextSectionNumber.ToString() + "." + elementSectionNumber.ToString() + " " + commonElement);

                objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:CommonElements/xhtml:CommonElement[@name='" + commonElement + "']");

                GenerateElementTable(objectIterator, level, sectionNumber + "." + nextSectionNumber.ToString(), elementSectionNumber);

                ++elementSectionNumber;
            }

            ++nextSectionNumber;


            //INFRASTRUCTURE MESSAGES
            displayName = "Messages";
            displayID = GetID(displayName);

            WriteAnchor(displayID);

            displayNumber = sectionNumber + "." + nextSectionNumber.ToString();

            AddContents(level, displayNumber, displayName, displayID);

            displayName = displayNumber + " " + displayName;

            writer.WriteElementString(GetHTag(level), displayName);

            ArrayList objectNames = new ArrayList();

            objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:Messages/xhtml:Message/@name");

            while (objectIterator.MoveNext())
            {
                objectNames.Add(objectIterator.Current.Value);
            }

            //objectNames.Sort();

            int objectSectionNumber = 1;

            foreach (string objectName in objectNames)
            {
                //System.Console.WriteLine(objectName);

                objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:Messages/xhtml:Message[@name='" + objectName + "']");

                GenerateElementTable(objectIterator, level, sectionNumber + "." + nextSectionNumber.ToString(), objectSectionNumber);

                ++objectSectionNumber;
            }

            ++nextSectionNumber;


            //INFRASTRUCTURE OBJECTS
            displayName = "Objects";
            displayID = GetID(displayName);

            WriteAnchor(displayID);

            displayNumber = sectionNumber + "." + nextSectionNumber.ToString();

            AddContents(level, displayNumber, displayName, displayID);

            displayName = displayNumber + " " + displayName;

            writer.WriteElementString(GetHTag(level), displayName);

            objectNames.Clear();

            objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:DataObjects/xhtml:DataObject/@name");

            while (objectIterator.MoveNext())
            {
                objectNames.Add(objectIterator.Current.Value);
            }

            //objectNames.Sort();

            objectSectionNumber = 1;

            foreach (string objectName in objectNames)
            {
                //System.Console.WriteLine(objectName);

                objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:DataObjects/xhtml:DataObject[@name='" + objectName + "']");

                GenerateElementTable(objectIterator, level, sectionNumber + "." + nextSectionNumber.ToString(), objectSectionNumber);

                ++objectSectionNumber;
            }

            ++nextSectionNumber;
        }



        /// <summary>
        /// Vince(2010/02/26): This is a new section to accomodate Zone Services.
        /// TODO - Break Services Section into multiple pards. 
        /// </summary>
        /// <param name="section"></param>
        /// <param name="level"></param>
        /// <param name="sectionNumber"></param>
        void GenerateZoneServices(XPathNodeIterator section, int level, string sectionNumber)
        {
            XPathNodeIterator contentsIterator = inputDocumentManager.Select(section.Current, "*[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");
            int nextSectionNumber = 1;

            while (contentsIterator.MoveNext())
            {

                if (level == 2) 
                {
                    //EndDocument(displayID);
                    //StartDocument(new FileInfo(this.path).Directory + "\\" + displayID2 + ".html", displayID2, this.rootTitle + " - " + displayName2, GetID((string)nextGroups[groupNumber++]));  
                } 
                
                GenerateSection(contentsIterator, level + 1, sectionNumber + "." + nextSectionNumber.ToString(), "");

                ++nextSectionNumber;
    
            }

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="example"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="exampleNumber"></param>
        /// <param name="name"></param>
        void WriteExample(XPathNodeIterator example, string sectionNumber, int exampleNumber, string name)
        {
            string exampleIntl = example.Current.GetAttribute("intl", "");

            if (exampleIntl != "" && exampleIntl != documentGlobalSettings.Intl)
            {
                return;
            }

            string exampleName = example.Current.GetAttribute("name", "");

            if (exampleName != "")
            {
                name = exampleName;
            }

            AddExample(writer, sectionNumber + "-" + exampleNumber.ToString(), name);

            WriteNewLine();

            writer.WriteStartElement("div");
            writer.WriteAttributeString("class", "example_parent");

            writer.WriteStartElement("div");
            writer.WriteAttributeString("class", "example");

            WriteNewLine();

            XPathNodeIterator example2 = inputDocumentManager.Select(example.Current, "child::node()");

            char[] trim_chars = { '\r', '\n', '\t' };

            while (example2.MoveNext())
            {
                StringWriter exampleStringWriter = new StringWriter();
                XmlTextWriter exampleWriter = new XmlTextWriter(exampleStringWriter);
                exampleWriter.Formatting = Formatting.Indented;
                exampleWriter.WriteNode(new Developmentor.Xml.NavigatorReader(example2.Current), true);
                exampleWriter.Close();

                string exampleResult = exampleStringWriter.ToString();
                exampleResult = exampleResult.TrimStart(trim_chars);

                writer.WriteString(exampleResult);

                //SaveExample(this.outputPath + @"temp\examples\", this.docId, sectionNumber + "-" + exampleNumber.ToString(), name, exampleResult);
            }

            WriteNewLine();

            writer.WriteEndElement();
            writer.WriteEndElement();

            WriteCaption(writer, "Example", sectionNumber + "-" + exampleNumber.ToString(), name);
        }

        /// <summary>
        ///   VP(2.24.2010): I currently think this method writes a line in the object table and at the same
        ///   time, it adds to the spreadsheet. Not sure why it is call WriteMessage.
        /// 
        ///   Called by GenerateSection.
        /// </summary>
        /// <param name="iterator"></param>
        /// <param name="level"></param>
        /// <param name="number"></param>
        void WriteMessage(XPathNodeIterator iterator, int level, string number)
        {
            string name = iterator.Current.GetAttribute("name", "");

            AddTable(writer, number, name);

            writer.WriteStartElement("table");
            writer.WriteAttributeString("class", "message");

            writer.WriteStartElement("thead");

            writer.WriteStartElement("tr");
            writer.WriteAttributeString("class", "message_name");
            writer.WriteStartElement("td");
            writer.WriteAttributeString("colspan", "5");
            writer.WriteString(name);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("tr");
            writer.WriteElementString("th", "Parameter");
            writer.WriteElementString("th", "minOccurs");
            writer.WriteElementString("th", "maxOccurs");
            writer.WriteElementString("th", "Type");
            writer.WriteElementString("th", "Description");
            writer.WriteEndElement();

            writer.WriteEndElement();

            //Get the Item nodes for processing
            XPathNodeIterator itemIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

            int items = 0;

            while (itemIterator.MoveNext())
            {
                ++items;

                string minOccurs = "1";
                string maxOccurs = "1";
                string parameterName = "";


                writer.WriteStartElement("tr");

                XPathNodeIterator parameterIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:Parameter");
                XPathNodeIterator typeIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:Type");
                XPathNodeIterator descriptionIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:Description");

                if (parameterIterator.MoveNext())
                {
                    parameterName = parameterIterator.Current.Value;
                    minOccurs = parameterIterator.Current.GetAttribute("minOccurs", "");
                    maxOccurs = parameterIterator.Current.GetAttribute("maxOccurs", "");
                }


                writer.WriteElementString("td", parameterName != "" ? parameterName : "\xA0");
                writer.WriteElementString("td", minOccurs != "" ? minOccurs : "\xA0");
                writer.WriteElementString("td", maxOccurs != "" ? maxOccurs : "\xA0");

                writer.WriteStartElement("td");

                if (typeIterator.MoveNext())
                {
                    WriteValue(typeIterator);
                }
                else
                {
                    writer.WriteString("\xA0");
                }

                writer.WriteEndElement();

                writer.WriteStartElement("td");

                if (descriptionIterator.MoveNext())
                {
                    WriteValue(descriptionIterator);
                }
                else
                {
                    writer.WriteString("\xA0");
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            if (items == 0)
            {
                writer.WriteStartElement("tr");
                writer.WriteElementString("td", "\xA0");
                writer.WriteElementString("td", "\xA0");
                writer.WriteElementString("td", "\xA0");
                writer.WriteElementString("td", "\xA0");
                writer.WriteElementString("td", "\xA0");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        void StartSpreadsheetExternalCodeSets()
        {
            ssWriter.WriteRaw("<Worksheet ss:Name = 'External Code Sets'>\r\n");

            ssStringWriter = new StringWriter();
            ssTempWriter = new XmlTextWriter(ssStringWriter);
            ssRows = ssCols = 0;
        }

        /// <summary>
        ///Somewhere in all this crap code the "Id" of the codeset gets the spaces taken out,
        ///The "group" (e.g., UK) prepended and the word "Type" is appended. For example, a codeset title
        ///named "The Codeset" becomes ref='UKTheCodesetType' by convention.
        /// This 'ref' becomes the value that a human now has to use in the input as the reference for the codeset.  That is
        ///of course, if this stupid convention is detected somehow.  There is no documentation, much less about this.
        ///How about having an id and a title as separate fields? Too much to ask?
        /// </summary>
        /// <param name="appendixIterator"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="level"></param>
        void WriteCodeSetAppendix(XPathNodeIterator appendixIterator, string sectionNumber, int level)
        {
            XPathNodeIterator introIterator = inputDocumentManager.Select(appendixIterator.Current, "./xhtml:Intro/child::node()");

            while (introIterator.MoveNext())
            {
                writer.WriteNode(new Developmentor.Xml.NavigatorReader(introIterator.Current), false);
            }

            SortedDictionary<string, string> sources = new SortedDictionary<string, string>();

            XPathNodeIterator sourceIterator = inputDocumentManager.Select(appendixIterator.Current, ".//xhtml:Grouping");

            while (sourceIterator.MoveNext())
            {
                XPathNodeIterator codeIterator = inputDocumentManager.Select(sourceIterator.Current, "@code");
                XPathNodeIterator nameIterator = inputDocumentManager.Select(sourceIterator.Current, "@name");

                codeIterator.MoveNext();

                string value = codeIterator.Current.Value;
                string key = value;

                if (nameIterator.MoveNext())
                {
                    key = nameIterator.Current.Value;
                }
                // bug here
                try
                {
                    sources.Add(key, value);
                }
                catch (Exception exception)
                {
                    if (log.IsErrorEnabled) log.Error("Generate HTML error: key=" + key + ", value=" + value, exception);
                    throw exception;
                }
            }

            foreach (KeyValuePair<string, string> entry in sources)
            {
                string source = entry.Value;
                string sourceName = entry.Key;

                string displayID2 = GetID(source);

                WriteAnchor(displayID2);

                sourceIterator = inputDocumentManager.Select(appendixIterator.Current, ".//xhtml:Grouping[@code='" + source + "']");
                sourceIterator.MoveNext();

                string sourceReference = sourceIterator.Current.GetAttribute("ref", "");

                AddContents(level + 1, "", sourceName, displayID2);

                writer.WriteStartElement(GetHTag(level + 1));

                writer.WriteString(sourceName);

                if (sourceReference != "")
                {
                    writer.WriteRaw(" [<a href='References.html#" + sourceReference + "'>" + sourceReference + "</a>]");
                }

                writer.WriteEndElement();

                sourceIterator = inputDocumentManager.Select(appendixIterator.Current, ".//xhtml:Grouping[@code='" + source + "']");

                while (sourceIterator.MoveNext())
                {
                    string sort = sourceIterator.Current.GetAttribute("sort", "");

                    ArrayList ids = new ArrayList();

                    XPathNodeIterator codesetIterator = inputDocumentManager.Select(sourceIterator.Current, "xhtml:CodeSet/xhtml:ID");

                    while (codesetIterator.MoveNext())
                    {
                        ids.Add(codesetIterator.Current.Value);
                    }

                    if (sort != "false")
                    {
                        ids.Sort();
                    }

                    foreach (string id in ids)
                    {
                        codesetIterator = inputDocumentManager.Select(sourceIterator.Current, "xhtml:CodeSet[xhtml:ID='" + id + "']");
                        codesetIterator.MoveNext();

                        XPathNodeIterator idIterator = inputDocumentManager.Select(sourceIterator.Current, "xhtml:CodeSet[xhtml:ID='" + id + "']/xhtml:ID");
                        idIterator.MoveNext();

                        string codesetReference = idIterator.Current.GetAttribute("ref", "");

                        WriteAnchor(GetID(source + id) + "Type");

                        AddContents(level + 2, "", id, GetID(source + id) + "Type");

                        writer.WriteStartElement(GetHTag(level + 2));
                        writer.WriteString(id);

                        if (codesetReference != "")
                        {
                            writer.WriteRaw(" [<a href='References.html#" + codesetReference + "'>" + codesetReference + "</a>]");
                        }

                        writer.WriteEndElement();

                        introIterator = inputDocumentManager.Select(codesetIterator.Current, "xhtml:Intro/child::node()");

                        while (introIterator.MoveNext())
                        {
                            writer.WriteNode(new Developmentor.Xml.NavigatorReader(introIterator.Current), false);
                        }

                        ArrayList codes = new ArrayList();

                        writer.WriteStartElement("table");
                        writer.WriteAttributeString("class", "codeset");

                        XPathNodeIterator valueIterator = inputDocumentManager.Select(codesetIterator.Current, ".//xhtml:Value");

                        while (valueIterator.MoveNext())
                        {
                            XPathNodeIterator codeIterator = inputDocumentManager.Select(valueIterator.Current, "xhtml:Code");

                            if (codeIterator.MoveNext())
                            {
                                codes.Add(codeIterator.Current.Value);
                            }
                        }

                        codes.Sort(this.codeComparer);

                        string[] codeSetRow = new string[5];

                        bool firstCode = true;

                        foreach (string code in codes)
                        {
                            valueIterator = inputDocumentManager.Select(codesetIterator.Current, ".//xhtml:Value[xhtml:Code='" + code.Replace("'", "&apos;") + "']");

                            if (valueIterator.MoveNext())
                            {
                                XPathNodeIterator codeIterator = inputDocumentManager.Select(valueIterator.Current, "xhtml:Code");
                                XPathNodeIterator textIterator = inputDocumentManager.Select(valueIterator.Current, "xhtml:Text");
                                XPathNodeIterator descriptionIterator = inputDocumentManager.Select(valueIterator.Current, "xhtml:Description");

                                if (codeIterator.MoveNext())
                                {
                                    string codeValue = codeIterator.Current.Value;

                                    codeSetRow[0] = source;
                                    codeSetRow[1] = id;
                                    codeSetRow[2] = codeValue;
                                    codeSetRow[3] = "";
                                    codeSetRow[4] = "";

                                    if (!firstCode)
                                    {
                                        codeSetRow[0] = codeSetRow[1] = "";
                                    }

                                    writer.WriteStartElement("tr");
                                    writer.WriteStartElement("td");
                                    writer.WriteElementString("code", codeValue);
                                    writer.WriteEndElement();
                                    writer.WriteStartElement("td");

                                    bool hasText = textIterator.MoveNext() && textIterator.Current.Value.Trim() != "";
                                    bool hasDescription = descriptionIterator.MoveNext() && descriptionIterator.Current.Value.Trim() != "";

                                    if (hasText && hasDescription)
                                    {
                                        string text = textIterator.Current.Value;
                                        string description = descriptionIterator.Current.Value;

                                        codeSetRow[3] = text;
                                        codeSetRow[4] = description;

                                        writer.WriteStartElement("dl");
                                        writer.WriteElementString("dt", text);
                                        writer.WriteElementString("dd", description);
                                        writer.WriteEndElement();
                                    }
                                    else if (hasText)
                                    {
                                        string text = textIterator.Current.Value;
                                        codeSetRow[3] = text;
                                        writer.WriteString(text);
                                    }
                                    else if (hasDescription)
                                    {
                                        string description = descriptionIterator.Current.Value;
                                        codeSetRow[4] = description;
                                        writer.WriteString(description);
                                    }

                                    writer.WriteEndElement();
                                    writer.WriteEndElement();

                                    WriteSpreadsheetRow(codeSetRow);

                                    firstCode = false;
                                }
                            }
                        }

                        writer.WriteEndElement();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheetExternalCodeSets()
        {
            ssTempWriter.Close();
            ssTempWriter = null;

            ssWriter.WriteRaw("<Table x:FullRows = '1' x:FullColumns = '1' ss:ExpandedRowCount = '" + ssRows + "' ss:ExpandedColumnCount = '" + ssCols + "'>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='150'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='300'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='100'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='500'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw(ssStringWriter.ToString());
            ssWriter.WriteRaw("</Table>\r\n");

            EndSpreadsheetWorksheet();
        }

        /// <summary>
        /// 
        /// </summary>
        void StartSpreadsheetCodeSets()
        {
            ssWriter.WriteRaw("<Worksheet ss:Name = 'Code Sets'>\r\n");

            ssStringWriter = new StringWriter();
            ssTempWriter = new XmlTextWriter(ssStringWriter);
            ssRows = ssCols = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheetWorksheet()
        {
            ssWriter.WriteRaw("<WorksheetOptions xmlns = 'urn:schemas-microsoft-com:office:excel'>\r\n");
            ssWriter.WriteRaw("<Print>\r\n");
            ssWriter.WriteRaw("<ValidPrinterInfo/>\r\n");
            ssWriter.WriteRaw("<HorizontalResolution>1200</HorizontalResolution>\r\n");
            ssWriter.WriteRaw("<VerticalResolution>1200</VerticalResolution>\r\n");
            ssWriter.WriteRaw("</Print>\r\n");
            ssWriter.WriteRaw("<ProtectObjects>False</ProtectObjects>\r\n");
            ssWriter.WriteRaw("<ProtectScenarios>False</ProtectScenarios>\r\n");
            ssWriter.WriteRaw("</WorksheetOptions>\r\n");

            ssWriter.WriteRaw("</Worksheet>\r\n");
        }

        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheetCodeSets()
        {
            ssTempWriter.Close();
            ssTempWriter = null;

            ssWriter.WriteRaw("<Table x:FullRows = '1' x:FullColumns = '1' ss:ExpandedRowCount = '" + ssRows + "' ss:ExpandedColumnCount = '" + ssCols + "'>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='150'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='300'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='100'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='500'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw(ssStringWriter.ToString());
            ssWriter.WriteRaw("</Table>\r\n");

            EndSpreadsheetWorksheet();
        }

        /// <summary>
        /// 
        /// </summary>
        void StartSpreadsheetCommonTypes()
        {
            ssWriter.WriteRaw("<Worksheet ss:Name = 'Common Types'>\r\n");

            ssStringWriter = new StringWriter();
            ssTempWriter = new XmlTextWriter(ssStringWriter);
            ssRows = ssCols = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        void EndSpreadsheetCommonTypes()
        {
            ssTempWriter.Close();
            ssTempWriter = null;

            ssWriter.WriteRaw("<Table x:FullRows = '1' x:FullColumns = '1' ss:ExpandedRowCount = '" + ssRows + "' ss:ExpandedColumnCount = '" + ssCols + "'>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='50'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='300'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:AutoFitWidth='0' ss:Width='100'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw("<Column ss:StyleID='s25' ss:AutoFitWidth='0' ss:Width='700'/>\r\n");
            ssWriter.WriteRaw(ssStringWriter.ToString());
            ssWriter.WriteRaw("</Table>\r\n");

            EndSpreadsheetWorksheet();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appendixIterator"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="level"></param>
        void WriteCommonTypesAppendix2(XPathNodeIterator appendixIterator, string sectionNumber, int level)
        {
            XPathNodeIterator introIterator = inputDocumentManager.Select(appendixIterator.Current, "./xhtml:Intro/child::node()");

            while (introIterator.MoveNext())
            {
                writer.WriteNode(new Developmentor.Xml.NavigatorReader(introIterator.Current), false);
            }

            int elementSectionNumber = 1;

            XPathNodeIterator commonElementIterator = inputDocumentManager.Select(appendixIterator.Current, ".//xhtml:CommonElement/@name");

            ArrayList commonElements = new ArrayList();

            while (commonElementIterator.MoveNext())
            {
                //System.Console.WriteLine(commonElementIterator.Current.Value);

                commonElements.Add(commonElementIterator.Current.Value);
            }

            commonElements.Sort();

            foreach (string commonElement in commonElements)
            {
                //writer.WriteElementString(GetHTag(level), sectionNumber + "." + nextSectionNumber.ToString() + "." + elementSectionNumber.ToString() + " " + commonElement);

                XPathNodeIterator typeIterator = inputDocumentManager.Select(appendixIterator.Current, ".//xhtml:CommonElement[@name='" + commonElement + "']");

                GenerateElementTable(typeIterator, level, sectionNumber, elementSectionNumber);

                ++elementSectionNumber;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="backgroundDirectory"></param>
        /// <param name="level"></param>
        void WriteBackgroundDirectory(string backgroundDirectory, int level)
        {

            /*
             * When processing the background folder there are directories the system should not go into.
             * The list of folders should be a configuration file in a later update
             */
            DirectoryInfo directoryInfo = new DirectoryInfo(backgroundDirectory);

            switch (directoryInfo.Name)
            {
                case ".svn":
                    return;
            }


            if (level > 1)
            {
                if (directoryInfo.Name == "exclude")
                {
                    return;
                }

                writer.WriteStartElement("li");
                writer.WriteAttributeString("class", "folder");

                string link = GetID(directoryInfo.FullName.Substring((this.outputPath + @"background\").Length).Replace("\\", "__"));

                WriteAnchor(link);

                writer.WriteString(directoryInfo.Name);
            }

            string[] directories = Directory.GetDirectories(backgroundDirectory);
            string[] files = Directory.GetFiles(backgroundDirectory);

            if (directories.Length > 0 || files.Length > 0)
            {
                writer.WriteStartElement("ul");

                foreach (string directory in directories)
                {
                    WriteBackgroundDirectory(directory, level + 1);
                }

                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);

                    if ((fileInfo.Attributes & FileAttributes.Hidden) != 0)
                    {
                        continue;
                    }

                    writer.WriteStartElement("li");
                    writer.WriteAttributeString("class", "file");

                    string link = fileInfo.FullName.Substring(this.outputPath.Length).Replace("\\", "/");

                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("href", link);
                    writer.WriteString(fileInfo.Name);
                    writer.WriteString(" (" + fileInfo.LastWriteTime.ToShortDateString() + ")");
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            if (level > 1)
            {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="index"></param>
        void WriteIndex(XmlTextWriter writer, ref ArrayList index)
        {
            WriteIndex(writer, ref index, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="index"></param>
        /// <param name="sort"></param>
        void WriteIndex(XmlTextWriter writer, ref ArrayList index, bool sort)
        {
            int count = 0;

            writer.WriteStartElement("table");
            writer.WriteAttributeString("class", "index");

            SortedDictionary<string, ContentsItem> sorted = new SortedDictionary<string, ContentsItem>();

            foreach (ContentsItem item in index)
            {
                if (sort)
                {
                    try
                    {
                        sorted.Add(item.name, item);
                    }
                    catch (Exception TheException)
                    {
                        if (log.IsErrorEnabled) log.Error("item.name = " + item.name, TheException);
                        throw TheException;
                    }
                    continue;
                }

                writer.WriteStartElement("tr");

                writer.WriteStartElement("td");
                writer.WriteAttributeString("class", "index-number");
                WriteContentsNumber(item);
                writer.WriteEndElement();

                writer.WriteStartElement("td");
                writer.WriteAttributeString("class", "index-name");
                WriteContentsName(item);
                writer.WriteEndElement();

                writer.WriteEndElement();

                ++count;
            }

            if (sort)
            {
                foreach (string name in sorted.Keys)
                {
                    ContentsItem item = sorted[name];

                    writer.WriteStartElement("tr");

                    writer.WriteStartElement("td");
                    writer.WriteAttributeString("class", "index-name");
                    WriteContentsName(item);
                    writer.WriteEndElement();

                    writer.WriteStartElement("td");
                    writer.WriteAttributeString("class", "index-number");
                    WriteContentsNumber(item);
                    writer.WriteEndElement();

                    writer.WriteEndElement();

                    ++count;
                }
            }

            writer.WriteEndElement();

            if (sort)
            {
                writer.WriteStartElement("p");
                writer.WriteStartElement("em");
                writer.WriteString(count + " Total");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="index"></param>
        void WriteMultiIndex(XmlTextWriter writer, ref ArrayList index)
        {
            int count = 0;

            writer.WriteStartElement("table");
            writer.WriteAttributeString("class", "index");

            SortedDictionary<string, SortedDictionary<string, ContentsItem>> sorted = new SortedDictionary<string, SortedDictionary<string, ContentsItem>>();

            foreach (ContentsItem item in index)
            {
                string id = item.number + "-" + item.id;

                if (!sorted.ContainsKey(item.name))
                {
                    SortedDictionary<string, ContentsItem> numbers = new SortedDictionary<string, ContentsItem>(indexNumberComparer);
                    numbers.Add(id, item);
                    sorted.Add(item.name, numbers);
                    continue;
                }

                sorted[item.name].Add(id, item);
            }

            foreach (string name in sorted.Keys)
            {
                SortedDictionary<string, ContentsItem> numbers = sorted[name];

                writer.WriteStartElement("tr");

                bool first = true;

                foreach (string id in numbers.Keys)
                {
                    ContentsItem item = numbers[id];

                    if (first)
                    {
                        writer.WriteStartElement("td");
                        writer.WriteAttributeString("class", "index-name");
                        WriteContentsName(item);
                        writer.WriteEndElement();

                        writer.WriteStartElement("td");
                        writer.WriteAttributeString("class", "index-number");
                    }

                    if (!first)
                    {
                        writer.WriteString(", ");
                    }

                    ++count;

                    WriteContentsNumber(item);

                    first = false;
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("p");
            writer.WriteStartElement("em");
            writer.WriteString(count + " Total");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appendixIterator"></param>
        /// <param name="sectionNumber"></param>
        /// <param name="level"></param>
        void WriteGenericAppendix(XPathNodeIterator appendixIterator, string sectionNumber, int level)
        {
            int sectionNumber2 = 1;

            XPathNodeIterator sectionIterator = inputDocumentManager.Select(appendixIterator.Current, "*");

            while (sectionIterator.MoveNext())
            {
                if (sectionIterator.Current.LocalName != "Section")
                {
                    writer.WriteNode(new Developmentor.Xml.NavigatorReader(sectionIterator.Current), true);
                }
                else
                {
                    GenerateSection(sectionIterator, level + 1, sectionNumber + "." + sectionNumber2.ToString(), "");

                    ++sectionNumber2;
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="index"></param>
        /// <param name="type"></param>
        /// <param name="number"></param>
        /// <param name="name"></param>
        void AddIndexItem(XmlTextWriter writer, ref ArrayList index, string type, string number, string name)
        {
            AddIndexItem(writer, ref index, type, number, name, "");
        }

       

        /// <summary>
        /// 
        /// </summary>
        /// <param name="group"></param>
        /// <param name="objectName"></param>
        void WriteBackgroundLink(string group, string objectName)
        {
            if (Directory.Exists(this.outputPath + @"background\" + group))
            {
                bool objectFound = false;

                string link = "BackgroundSupplementaryDocumentationNonNormative.html#" + group;

                if (objectName != "" && Directory.Exists(this.outputPath + @"background\" + group + @"\" + objectName))
                {
                    link += "__" + objectName;
                    objectFound = true;
                }
                else if (objectName != "")
                {
                    return;
                }

                writer.WriteStartElement("p");
                writer.WriteAttributeString("class", "emphasized_note");
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", link);
                writer.WriteString("Click here for non-normative background/supplementary documentation ");

                if (!objectFound)
                {
                    writer.WriteString("from this group.");
                }
                else
                {
                    writer.WriteString("on this object.");
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="markup"></param>
        /// <returns></returns>
        string Textify(string markup)
        {
            StringBuilder result = new StringBuilder(markup.Length);

            for (int i = 0; i < markup.Length; ++i)
            {
                char c = markup[i];

                if (c == '<')
                {
                    while (++i < markup.Length && markup[i] != '>')
                    {

                    }
                }
                else if (c == ' ' || c == '\r' || c == '\n' || c == '\t')
                {
                    while (++i < markup.Length)
                    {
                        c = markup[i];

                        if (c == ' ' || c == '\r' || c == '\n' || c == '\t')
                        {
                            continue;
                        }

                        --i;

                        result.Append(' ');

                        break;
                    }
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        void WriteSpreadsheetRow(string[] columns)
        {
            if (ssTempWriter == null) return;

            ++ssRows;

            ssCols = Math.Max(ssCols, columns.Length);

            ssTempWriter.WriteRaw("<Row>\r\n");

            foreach (string c in columns)
            {
                ssTempWriter.WriteRaw("<Cell>");
                ssTempWriter.WriteRaw("<Data ss:Type='String'>");
                //ssTempWriter.WriteString(c);
                ssTempWriter.WriteRaw(c);
                ssTempWriter.WriteRaw("</Data>");
                ssTempWriter.WriteRaw("</Cell>\r\n");
            }

            ssTempWriter.WriteRaw("</Row>\r\n");
        }

        byte[] newLine = new byte[] { (byte)'\r', (byte)'\n' };

        /// <summary>
        /// 
        /// </summary>
        void WriteNewLine()
        {
            writer.Flush();
            writer.BaseStream.Write(newLine, 0, newLine.Length);
        }

        void WriteValue(XPathNodeIterator iterator)
        {
            XPathNodeIterator childIterator = inputDocumentManager.Select(iterator.Current, "child::node()");

            while (childIterator.MoveNext())
            {
                writer.WriteNode(new Developmentor.Xml.NavigatorReader(childIterator.Current), false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        void WriteContentsNumber(ContentsItem item)
        {
            writer.WriteStartElement("a");
            writer.WriteAttributeString("href", item.docId + ".html#" + item.id);
            writer.WriteString(item.type + "\xA0" + item.number.Replace("-", "\u2011"));
            writer.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        void WriteContentsName(ContentsItem item)
        {
            if (item.name != "")
            {
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", item.docId + ".html#" + item.id);
                writer.WriteString(item.name);
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteString("\xA0");
            }
        }
    }
}
