 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Collections;
using System.IO;

namespace GenerateSpec.Generator
{
    /// <summary>
    /// Generate XSDs from the input file.
    /// Also generate the schema diagrams.  
    /// </summary>
    public class GenerateXsd
    {
        XmlTextWriter rootWriter;
        XmlTextWriter writer;
        Dictionary<string, int> keyNames = new Dictionary<string, int>();
        XPathDocument doc;
        XPathNavigator navigator;
        bool inCommonElement = false;
        bool inInfrastructure = false;
        string objectNameOverride;
        string currentGroup;
        Hashtable mEmptyTypes = new Hashtable();
        XmlNamespaceManager namespaceManager;

        Draw.Diagrammer diagrammer = new Draw.Diagrammer();
        Draw.Node rootNode;
        Draw.Node currentNode;
        Draw.Attribute currentAttribute;

        bool actionListConstraints = true;
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentGlobalSettings"></param>
        /// <param name="XsdSettings"></param>
        /// <param name="documentManager"></param>
        public GenerateXsd(DocumentGlobalSettings documentGlobalSettings, GenerateXsdSettings XsdSettings, InputDocumentManager documentManager)
        {
            generateXsdSettings = XsdSettings;
            this.documentGlobalSettings = documentGlobalSettings;

            namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("xhtml", "http://www.w3.org/1999/xhtml");
            namespaceManager.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            namespaceManager.AddNamespace("sif3", "http://www.sifassociation.org/datamodel/us/3.x");


            inputDocumentManager = documentManager;
        }

        private InputDocumentManager inputDocumentManager;

        /// <summary>
        /// 
        /// </summary>
        public InputDocumentManager InputDocumentManager
        {
            get { return inputDocumentManager; }
            
        }

        private DocumentGlobalSettings documentGlobalSettings;

        /// <summary>
        /// 
        /// </summary>
        public DocumentGlobalSettings DocumentGlobalSettings
        {
            get { return documentGlobalSettings; }
            set { documentGlobalSettings = value; }
        }

        private GenerateXsdSettings generateXsdSettings;

        /// <summary>
        /// 
        /// </summary>
        public GenerateXsdSettings GenerateXsdSettings
        {
            get { return generateXsdSettings; }
        }

        //VP
        //This property I think distinguishes whether the xsd definition is for a SIF object versus a type definition
        //or a code set, etc. 
        bool inDataObject = false;
        //a Service body object must be a data object but not the other way arount
        //TODO: Fix the crude hack that uses this variable.
        bool inServiceBodyObject = false;

        /// <summary>
        /// This is the root method for generating XSDs and XSD fragments. It calls sub-methods to create
        /// the different kinds of XSDs such as DataModel, Infrastructure, and Service Body Definition XSDs. 
        /// </summary>
        public void Generate()
        {
            Initialize();
            /* removed March 2016 by vp
            GenerateInfrastructureXSD();
            if (generateXsdSettings.IsServiceBodyDefinition || !generateXsdSettings.IsDataModelXSD)
            {
                GenerateZoneServiceBodyXSD();
            }
            if (!generateXsdSettings.IsServiceBodyDefinition)
            {
                GenerateDataModelXSD();
            }
            GenerateCodeSetsXSD();
            GenerateExternalCodeSetsXSD();
            GenerateCommonTypesXSD();
             */

            //-------------------------------------
            //Data Model XSD Set
            //-------------------------------------
            if (generateXsdSettings.IsDataModelXSD)
            {
                GenerateInfrastructureXSD();
                GenerateCommonElements();
                GenerateDataModelXSD();
                GenerateCodeSetsXSD();
                GenerateExternalCodeSetsXSD();
                GenerateCommonTypesXSD();
            }
            //-------------------------------------
            //SIF_Message XSD Set
            //-------------------------------------
            else if (!generateXsdSettings.IsServiceBodyDefinition)
            {
                GenerateInfrastructureXSD();
                GenerateZoneServiceBodyXSD();
                GenerateCommonElements();
                GenerateDataModelXSD();
                GenerateCodeSetsXSD();
                GenerateExternalCodeSetsXSD();
                GenerateCommonTypesXSD();
            }
            //-------------------------------------
            //Zone Service XSD Set
            // Zone Services are not used - THIS CODE IS OBSOLETE
            //-------------------------------------
            else if (generateXsdSettings.IsServiceBodyDefinition)
            {
                GenerateInfrastructureXSD();
                GenerateCommonElements();
                GenerateZoneServiceBodyXSD();
                GenerateCodeSetsXSD();
                GenerateExternalCodeSetsXSD();
                GenerateCommonTypesXSD();
            }

            if (generateXsdSettings.SingleSchema) EndXSDFile();
        }

        /// <summary>
        /// 
        /// </summary>
        void Initialize()
        {
            keyNames.Clear(); // generate XSD only

            rootWriter = writer = null; // specification generate only

            doc = new XPathDocument(inputDocumentManager.InputDocumentPath); // Generate Spec WS and GenerateInfrastructureSpreadSheet use it directly but it is used to generate navigators 
            navigator = doc.CreateNavigator();  // used in everything: maybe each generator needs to create its own navigator and keep it local to the generator

            ReadBuildComment();
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// Second-level method.  Called by Generate(). 
        /// Creates the SIF_Message file.
        /// </summary>
        void GenerateInfrastructureXSD()
        {
            ArrayList includes = new ArrayList();

            //-------------------------------------------------------------------------
            //This method really only generates the SIF_Message file.  Other items
            //like the infrastructure messages, common elements, and types are generated
            //separately. 
            //Builds the includes array which will be written to the SIF_Message File
            //so that SIF_Message will reference the separate, per object, XSD files
            //-------------------------------------------------------------------------
            XPathNodeIterator objectIterator;
            XPathNodeIterator commonElementIterator;
            
            //do this if we are using includes
            if (!generateXsdSettings.SingleSchema)
            {
                includes.Add("CommonTypes.xsd");
                includes.Add("CommonElements.xsd");


                //Handles the infrastructure data objects like ZoneStatus, AgentACL, and LogEntry
                objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:DataObjects/xhtml:DataObject");

                while (objectIterator.MoveNext())
                {
                    includes.Add("Infrastructure/" + objectIterator.Current.GetAttribute("name", "").Trim() + ".xsd");
                }

                //Handles data objects by working group  
                if (generateXsdSettings.IsDataModelXSD || !generateXsdSettings.IsServiceBodyDefinition)
                {
                    XPathNodeIterator groupIterator = inputDocumentManager.Select(navigator, "//xhtml:Group");
                    while (groupIterator.MoveNext())
                    {
                        string groupName = GetID(groupIterator.Current.GetAttribute("name", "").Trim());

                        groupName = groupName.Replace("WorkingGroup", "");
                        groupName = groupName.Replace("TaskForce", "");

                        objectIterator = inputDocumentManager.Select(groupIterator.Current, "./xhtml:DataObject");
                        while (objectIterator.MoveNext())
                        {
                            includes.Add(groupName + "/" + objectIterator.Current.GetAttribute("name", "").Trim() + ".xsd");
                        }
                    }
                }
                else if (generateXsdSettings.IsServiceBodyDefinition)
                {
                    XPathNodeIterator groupIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Zone Services']/xhtml:Section");
                    while (groupIterator.MoveNext())
                    {
                         string groupName = GetID(groupIterator.Current.GetAttribute("name", "").Trim());

                         objectIterator = inputDocumentManager.Select(groupIterator.Current, ".//xhtml:DataObject");
                         while (objectIterator.MoveNext())
                         {
                            includes.Add(groupName + "/" + objectIterator.Current.GetAttribute("name", "").Trim() + ".xsd");
                         }

                    }
                }

                //External Code Sets
                XPathNodeIterator sourceIterator = inputDocumentManager.Select(navigator, "//xhtml:Appendix[@name='External Code Sets']//xhtml:Grouping");

                while (sourceIterator.MoveNext())
                {
                    string code = sourceIterator.Current.GetAttribute("code", "");

                    includes.Add("ExternalCodeSets/" + code + ".xsd");
                }

                //Code Sets
                XPathNodeIterator sourceIterator2 = inputDocumentManager.Select(navigator, "//xhtml:Appendix[@name='Code Sets']//xhtml:Grouping");

                while (sourceIterator2.MoveNext())
                {
                    string code = sourceIterator2.Current.GetAttribute("code", "");

                    includes.Add("CodeSets/" + code + ".xsd");
                }
            }  //End of code that is run if we are using includes.

            //----------------------------------------
            //Write the includes to SIF_Message then
            //  create the rest of the SIF_Message file.
            //----------------------------------------
            XPathNodeIterator namespaceIterator = null;
            XPathNodeIterator importIterator = null;

            if (generateXsdSettings.SingleSchema)
            {
                namespaceIterator = inputDocumentManager.Select(navigator, ".//xhtml:Namespace");
                importIterator = inputDocumentManager.Select(navigator, ".//xhtml:Import");
            }

            StartXSDFile("", "SIF_Message", (string[])includes.ToArray(typeof(string)), namespaceIterator, importIterator);

            if (generateXsdSettings.SingleSchema)
            {
                WriteNewLine();
                writer.WriteComment("Infrastructure Common Elements");
                WriteNewLine();
            }

            commonElementIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:CommonElements/xhtml:CommonElement");

            while (commonElementIterator.MoveNext())
            {
                this.inInfrastructure = true;
                this.inCommonElement = true;
                GenerateDefinition(commonElementIterator, true/*commonElementIterator.Current.GetAttribute("name", "") == "SIF_Message"*/);
                this.inCommonElement = false;
                this.inInfrastructure = false;
            }

            if (generateXsdSettings.SingleSchema)
            {
                WriteNewLine();
                writer.WriteComment("Infrastructure Messages");
                WriteNewLine();
            }

            XPathNodeIterator messageIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']//xhtml:Message");

            while (messageIterator.MoveNext())
            {
                this.inInfrastructure = true;
                GenerateDefinition(messageIterator, true/*false*/);
                this.inInfrastructure = false;
            }

            if (!generateXsdSettings.SingleSchema) EndXSDFile();

            if (generateXsdSettings.SingleSchema)
            {
                WriteNewLine();
                writer.WriteComment("Infrastructure Data Objects");
                WriteNewLine();
            }

            //Handles the infrastructure data objects like ZoneStatus, AgentACL, and LogEntry
            objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']/xhtml:DataObjects/xhtml:DataObject");

            while (objectIterator.MoveNext())
            {
                this.inInfrastructure = true;
                this.inDataObject = true;


                if (!generateXsdSettings.SingleSchema) StartXSDFile("Infrastructure", objectIterator.Current.GetAttribute("name", "").Trim());

                GenerateDefinition(objectIterator, true);

                //This is where the second copy of these objects gets written.  Don't know why. One of the copies is never used. 
                //Therefore it is being commented out the zzzInfrastructureType is eliminated and the single reference
                //has been changed to zzzType. 
                //if (!this.isDataModelXSD)
               /* {
                    this.inDataObject = false;
                    this.objectNameOverride = objectIterator.Current.GetAttribute("name", "") + "Infrastructure";
                    GenerateDefinition(objectIterator, false);
                    this.objectNameOverride = null;
                } */

                if (!generateXsdSettings.SingleSchema) EndXSDFile();

                this.inDataObject = false;
                this.inInfrastructure = false;
            }
        }


        void GenerateCommonElements()
        {
            //------------------------------------
            //Handle Data Model Common Elements
            //------------------------------------
            if (!generateXsdSettings.SingleSchema) StartXSDFile("", "CommonElements");

            if (generateXsdSettings.SingleSchema)
            {
                WriteNewLine();
                writer.WriteComment("Data Model Common Elements");
                WriteNewLine();
            }

            XPathNodeIterator commonElementIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Data Model']/xhtml:CommonElements/xhtml:CommonElement");

            while (commonElementIterator.MoveNext())
            {
                this.inDataObject = false; 
                this.inCommonElement = true;

                GenerateDefinition(commonElementIterator, true/*false*/);

                this.inCommonElement = false;
            }

            if (!generateXsdSettings.SingleSchema) EndXSDFile();

        }

        /// <summary>
        /// Second-level method.  Called by Generate(). 
        /// </summary>
        void GenerateDataModelXSD()
        {
            this.inDataObject = true;

            //------------------------------------------------------------
            //Handle Data Model objects by Working Group or Task Force.
            //------------------------------------------------------------
            XPathNodeIterator groupIterator = inputDocumentManager.Select(navigator, "//xhtml:Group");

            while (groupIterator.MoveNext())
            {
                string groupName = GetID(groupIterator.Current.GetAttribute("name", "").Trim());

                if (generateXsdSettings.SingleSchema)
                {
                    WriteNewLine();
                    writer.WriteComment(groupName);
                    WriteNewLine();
                }

                groupName = groupName.Replace("WorkingGroup", "");
                groupName = groupName.Replace("TaskForce", "");

                this.currentGroup = groupName;    

                //------------------------------
                //Handle each Data Model Object Within a Group or Task Force.
                //------------------------------
                XPathNodeIterator objectIterator = inputDocumentManager.Select(groupIterator.Current, "./xhtml:DataObject");

                while (objectIterator.MoveNext())
                {
                    XPathNodeIterator namespaceIterator = null;
                    XPathNodeIterator importIterator = null;

                    if (!generateXsdSettings.SingleSchema)
                    {
                        namespaceIterator = inputDocumentManager.Select(objectIterator.Current, ".//xhtml:Namespace");
                        importIterator = inputDocumentManager.Select(objectIterator.Current, ".//xhtml:Import");
                    }

                    if (!generateXsdSettings.SingleSchema) StartXSDFile(groupName, objectIterator.Current.GetAttribute("name", "").Trim(), null, namespaceIterator, importIterator);

                    this.inDataObject = true;
                    GenerateDefinition(objectIterator, true);
                    this.inDataObject = false;

                    if (!generateXsdSettings.SingleSchema) EndXSDFile();
                }
            }

            this.inDataObject = false;
        }

        
        /// <summary>
        /// Second-level method.  Called by Generate(). 
        /// This method is similar to (based upon) the Data Model XSD generation method
        /// in that groups (e.g., Assessment, SRE) are used to organize the input and output.
        /// 
        /// Zone Services are not used so THIS CODE IS OBSOLETE.
        /// </summary>
        void GenerateZoneServiceBodyXSD()
        {
            //set these flags for logic later on
            this.inServiceBodyObject = true;
            this.inDataObject = true;

           
            //--------------------------------------------
            //Handle each Zone Service section as a group
            //--------------------------------------------
            XPathNodeIterator groupIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Zone Services']/xhtml:Section");

            while (groupIterator.MoveNext())
            {
                string groupName = GetID(groupIterator.Current.GetAttribute("name", "").Trim());

                if (generateXsdSettings.SingleSchema)
                {
                    WriteNewLine();
                    writer.WriteComment(groupName);
                    WriteNewLine();
                }

                this.currentGroup = groupName;

                //-------------------------------------
                //Generate each Zone Service Data Object
                //-------------------------------------
                XPathNodeIterator objectIterator = inputDocumentManager.Select(groupIterator.Current, ".//xhtml:DataObject");

                while (objectIterator.MoveNext())
                {
                    XPathNodeIterator namespaceIterator = null;
                    XPathNodeIterator importIterator = null;

                    if (!generateXsdSettings.SingleSchema)
                    {
                        namespaceIterator = inputDocumentManager.Select(objectIterator.Current, ".//xhtml:Namespace");
                        importIterator = inputDocumentManager.Select(objectIterator.Current, ".//xhtml:Import");
                    }

                    if (!generateXsdSettings.SingleSchema) StartXSDFile(groupName, objectIterator.Current.GetAttribute("name", "").Trim(), null, namespaceIterator, importIterator);


                    GenerateDefinition(objectIterator, true);
                    

                    if (!generateXsdSettings.SingleSchema) EndXSDFile();
                }
            }

            this.inServiceBodyObject = false;
            this.inDataObject = false;
        }



        /// <summary>
        /// Second-level method.  Called by Generate(). 
        /// </summary>
        void GenerateCodeSetsXSD()
        {
            XPathNodeIterator sourceIterator = inputDocumentManager.Select(navigator, "//xhtml:Appendix[@name='Code Sets']//xhtml:Grouping");
            GenerateCodeSetsXSD(sourceIterator, "CodeSets");
        }

        /// <summary>
        /// This is the core code set writer.  It writes single elements or type definitions and not
        /// complete xml objects. 
        /// </summary>
        /// <param name="sourceIterator"></param>
        /// <param name="fileName"></param>
        void GenerateCodeSetsXSD(XPathNodeIterator sourceIterator, string fileName)
        {
           
            while (sourceIterator.MoveNext())
            {
                string code = sourceIterator.Current.GetAttribute("code", "");

                if (!generateXsdSettings.SingleSchema) StartXSDFile(fileName, code);

                XPathNodeIterator codesetIterator = inputDocumentManager.Select(sourceIterator.Current, "xhtml:CodeSet");

                while (codesetIterator.MoveNext())
                {
                    XPathNodeIterator idIterator = inputDocumentManager.Select(codesetIterator.Current, "xhtml:ID");

                    if (idIterator.MoveNext())
                    {
                        String typeName = code + GetID(idIterator.Current.Value) + "Type";
                        String baseType = "xs:token";

                        writer.WriteStartElement("xs:simpleType");
                        writer.WriteAttributeString("name", typeName);
                        writer.WriteStartElement("xs:restriction");
                        writer.WriteAttributeString("base", baseType);

                        
                        XPathNodeIterator codeIterator = inputDocumentManager.Select(codesetIterator.Current, ".//xhtml:Value/xhtml:Code");

                        while (codeIterator.MoveNext())
                        {
                            String codeValue = codeIterator.Current.Value.Trim();

                            writer.WriteStartElement("xs:enumeration");
                            writer.WriteAttributeString("value", codeValue);

                            if (generateXsdSettings.Annotate)
                            {
                                XPathNodeIterator textIterator = inputDocumentManager.Select(codeIterator.Current, "../xhtml:Text");

                                if (textIterator.MoveNext())
                                {
                                    string description = textIterator.Current.Value;

                                    writer.WriteStartElement("xs:annotation");
                                    writer.WriteStartElement("xs:documentation");
                                    writer.WriteString(description);
                                    writer.WriteEndElement();
                                    writer.WriteEndElement();
                                }
                            }

                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }

                if (!generateXsdSettings.SingleSchema) EndXSDFile();
            }
        }

        /// <summary>
        /// Second-level method.  Called by Generate(). 
        /// This method uses GenerateCodeSetsXSD() to do its work. 
        /// </summary>
        void GenerateExternalCodeSetsXSD()
        {
            XPathNodeIterator sourceIterator = inputDocumentManager.Select(navigator, "//xhtml:Appendix[@name='External Code Sets']//xhtml:Grouping");
            GenerateCodeSetsXSD(sourceIterator, "ExternalCodeSets");
        }

        /// <summary>
        /// Second-level method.  Called by Generate().
        /// Creates an iterator for sets of common elements or common types.  For each type or element it calls GenerateDefinition()
        ///   or it writes the node using WriteNode(). 
        /// </summary>
        void GenerateCommonTypesXSD()
        {
            //--------------------------------------------
            if (!generateXsdSettings.SingleSchema) StartXSDFile("", "CommonTypes");

            //Select a set of common types
            XPathNodeIterator typeIterator = inputDocumentManager.Select(navigator, "//xhtml:Appendix[@name='Common Types']/xhtml:CommonTypes/child::node()[local-name(.)='simpleType' or local-name(.)='complexType']");

            while (typeIterator.MoveNext())
            {
                string typeName = typeIterator.Current.GetAttribute("name", "");

                writer.WriteNode(new Developmentor.Xml.NavigatorReader(typeIterator.Current), true);

                WriteNewLine();
            }

            
            //----------------------------------------------
            //This handles Appendix A
            //----------------------------------------------

            //Select a set of common elements
            XPathNodeIterator commonElementIterator = inputDocumentManager.Select(navigator, "//xhtml:Appendix[@name='Common Types']//xhtml:CommonElement");

            while (commonElementIterator.MoveNext())
            {
                string dm = commonElementIterator.Current.GetAttribute("dm", "");

                if (dm == "true")
                {
                    this.inDataObject = true;
                }

                this.inCommonElement = true;

                GenerateDefinition(commonElementIterator, false, true);

                this.inCommonElement = false;

                this.inDataObject = false;
            }
            
            //-------------------------------------------------
            if (!generateXsdSettings.IsDataModelXSD)
            {
                //select some special cases
                XPathNodeIterator objectIterator = inputDocumentManager.Select(navigator, "//xhtml:Section[@name='Infrastructure']//xhtml:CommonElement[@name='SIF_Query' or @name='SIF_ExtendedQuery' or @name='SIF_ExtendedQueryResults' or @name='SIF_Error' or @name='SIF_Header']");

                while (objectIterator.MoveNext())
                {
                    this.inDataObject = true;
                    this.inCommonElement = true;

                    this.objectNameOverride = objectIterator.Current.GetAttribute("name", "") + "DataModelType";
                    GenerateDefinition(objectIterator, false, true);
                    this.objectNameOverride = null;

                    this.inCommonElement = false;
                    this.inDataObject = false;
                }
            }

            //-----------------------------------------------
            if (!generateXsdSettings.IsDataModelXSD && !generateXsdSettings.IsSIFMessage2XSD && mEmptyTypes.Count > 0)
            {
                foreach (String key in mEmptyTypes.Keys)
                {
                    writer.WriteComment(key);
                    writer.WriteRaw("\r\n\r\n" + (string)mEmptyTypes[key] + "\r\n\r\n");
                }
            }

            //--------------------------------------------------
            if (!generateXsdSettings.SingleSchema) EndXSDFile();
        }


        /// <summary>
        /// 
        /// </summary>
        void EndXSDFile()
        {
            
            writer.Flush();

            writer.WriteEndElement();

            writer.Close();
        }

      

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        void StartXSDFile(string folder, string name)
        {
            StartXSDFile(folder, name, null, null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        /// <param name="includes"></param>
        /// <param name="namespaceIterator"></param>
        /// <param name="importIterator"></param>
        void StartXSDFile(string folder, string name, string[] includes, XPathNodeIterator namespaceIterator, XPathNodeIterator importIterator)
        {
            string tempName = name; 

            if (!Directory.Exists(generateXsdSettings.OutputXSDPath + folder))
            {
                Directory.CreateDirectory(generateXsdSettings.OutputXSDPath + folder);
            }

           
            //check for a ':' in the name and replace with an underscore b/c the filename cant contain a ':' 
            if (tempName.Contains(":"))
            {
                tempName = tempName.Replace(":", "_");
            }


            string path = generateXsdSettings.OutputXSDPath + folder + "\\" + tempName + ".xsd";


            writer = new XmlTextWriter(new StreamWriter(path));
            writer.Formatting = Formatting.Indented;

            if (buildComment != "")
            {
                writer.WriteComment(buildComment);
            }

            writer.WriteStartElement("xs:schema");
            //writer.WriteAttributeString("xmlns", "http://www.w3.org/2001/XMLSchema");
            writer.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");

            //TODO Figure this out and remove the possibility that markward.com is referenced in our spec
            if (!generateXsdSettings.AddNilAttributes)
            {
                writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            }
            else
            {
                writer.WriteAttributeString("xmlns:xsi", "http://markward.com");
            }

            writer.WriteAttributeString("targetNamespace", documentGlobalSettings.SifNamespace);
            writer.WriteAttributeString("xmlns:sif", documentGlobalSettings.SifNamespace);

            if (namespaceIterator != null)
            {
                while (namespaceIterator.MoveNext())
                {
                    string prefix = namespaceIterator.Current.GetAttribute("prefix", "");
                    string _namespace = namespaceIterator.Current.GetAttribute("namespace", "");

                    writer.WriteAttributeString("xmlns:" + prefix, _namespace);
                }
            }

            writer.WriteAttributeString("elementFormDefault", "qualified");


            //add the XML namespace import statement
            writer.WriteStartElement("xs:import");
            writer.WriteAttributeString("namespace", "http://www.w3.org/XML/1998/namespace");
            //writer.WriteAttributeString("schemaLocation", "http://www.w3.org/2001/xml.xsd");
            writer.WriteAttributeString("schemaLocation", "imports/xml/xml.xsd");
            writer.WriteEndElement();
            /*
            writer.WriteStartElement("xs:import");
            writer.WriteAttributeString("namespace", "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance.xsd");
            writer.WriteEndElement();
            */

            //TODO get rid of this one too
            if (generateXsdSettings.AddNilAttributes)
            {
                writer.WriteStartElement("xs:import");
                writer.WriteAttributeString("namespace", "http://markward.com");
                writer.WriteAttributeString("schemaLocation", "nil.xsd");
                writer.WriteEndElement();
            }

            //not sure when this is used VP:2010-06-16
            if (importIterator != null)
            {
                while (importIterator.MoveNext())
                {
                    string _namespace = importIterator.Current.GetAttribute("namespace", "");
                    string schemaLocation = importIterator.Current.GetAttribute("schemaLocation", "");

                    writer.WriteStartElement("xs:import");
                    writer.WriteAttributeString("namespace", _namespace);
                    writer.WriteAttributeString("schemaLocation", schemaLocation);
                    writer.WriteEndElement();
                }
            }

            //write the includes in SIF_Message.  The includes are passed into this method with includes[]
            if (includes != null)
            {
                foreach (string include in includes)
                {
                    writer.WriteStartElement("xs:include");
                    writer.WriteAttributeString("schemaLocation", include);
                    writer.WriteEndElement();
                }
            }
        }

        //why is this here? it should probably be in the method below.
        byte[] newLine = new byte[] { (byte)'\r', (byte)'\n' };


        /// <summary>
        /// 
        /// </summary>
        void WriteNewLine()
        {
            writer.Flush();
            writer.BaseStream.Write(newLine, 0, newLine.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        void WriteNewLines()
        {
            WriteNewLines(2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        void WriteNewLines(int i)
        {
            while (i-- > 0)
            {
                WriteNewLine();
            }
        }


        /// <summary>
        /// Overidden by GenerateDefinition(XPathNodeIterator rootIterator, bool isElement, bool isTyped)
        /// </summary>
        /// <param name="rootIterator"></param>
        /// <param name="isElement"></param>
        void GenerateDefinition(XPathNodeIterator rootIterator, bool isElement)
        {
            GenerateDefinition(rootIterator, isElement, false);
        }

        /// <summary>
        /// Encapsulates the generation of a complete object XSD. 
        /// This is used for complete SIF objects. The code set and common types
        /// methods write the XSDs without calling a sub-method and they generate single
        /// elements or types instead of xml objects. 
        /// 
        /// The top level iterator iterates through items 
        /// </summary>
        /// <param name="rootIterator"></param>
        /// <param name="isElement"></param>
        /// <param name="isTyped"></param>
        void GenerateDefinition(XPathNodeIterator rootIterator, bool isElement, bool isTyped)
        {
            string name = rootIterator.Current.GetAttribute("name", "");
            string originalName = name;
            string tempName = "";     //this is a local variable used to change namespaced elements to underscores.

            if (this.objectNameOverride != null)
            {
                name = this.objectNameOverride;
            }

            //System.Console.WriteLine("Generating XSD definition for " + name + "...");

            /* handle Choice and Sequence Vince: uh... no... not here.*/
            XPathNodeIterator itemIterator = inputDocumentManager.Select(rootIterator.Current, ".//xhtml:Item[@intl = false() or @intl='" + documentGlobalSettings.Intl + "']");

            ArrayList items = new ArrayList();
            bool first = true;

            //-----------------------------------------------------------
            //Load up the items array with elements, attributes, and types.
            //-----------------------------------------------------------
            while (itemIterator.MoveNext())
            {
                //one iterator below is always empty and the other has members for each time through the loop.
                XPathNodeIterator elementNameIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:Element");
                XPathNodeIterator attributeNameIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:Attribute");

                //This section translates the xml input language into another convention where the levels are more
                //tightly specified using a slash notation and attributes are identified with an @ at the beginning 
                //of the name.
                string itemName = "";

                if (elementNameIterator.MoveNext() && elementNameIterator.Current.Value.Trim() != "")
                {
                    itemName = elementNameIterator.Current.Value.Trim();
                }
                else if (attributeNameIterator.MoveNext() && attributeNameIterator.Current.Value.Trim() != "")
                {
                    itemName = "@" + attributeNameIterator.Current.Value.Trim();
                }
                else
                {
                    continue;
                }

                if (first && (itemName != name && itemName != originalName))
                {
                    items.Add(new object[] { name, null });
                    first = false;
                }

                if (itemName[0] != '@')
                {
                    if (!first)
                    {
                        items.Add(new object[] { '/' + itemName, itemIterator.Clone() });
                    }
                    else
                    {
                        items.Add(new object[] { itemName, itemIterator.Clone() });
                    }
                }
                else
                {
                    items.Add(new object[] { itemName, itemIterator.Clone() });
                }

                first = false;
            }

            if (first)
            {
                items.Add(new object[] { name, null });
            }

            //-----------------------------------------------------------
            //Create itemArray which is a 2 dimensional array. 
            //-----------------------------------------------------------
            object[][] itemArray = (object[][])items.ToArray(typeof(object[]));

            /*
                        for (int i = 0; i < itemArray.Length; ++i)
                        {
                            string itemName = (string)itemArray[i][0];

                            System.Console.WriteLine(itemName);
                        }
            */

            //---------------------------------------
            //Pass the itemArray off to be written out
            //---------------------------------------
            //write a sif object or common type (or common element: deprecated)
            WriteDefinition(name, itemArray, isTyped, isElement, rootIterator);

            //write the corresponding collection object. the collection object holds a collection of the 
            //object data structures. 
            //only do this if it is a SIF object
            if (isElement && !inCommonElement)
            {
                WriteCollectionDefinition(name);
            }


            //-------------------------------------------
            //Generate the diagram for the object or type
            //-------------------------------------------
            if (isElement || isTyped)
            {
                if (rootNode != null && generateXsdSettings.Diagram)
                {
                    tempName = (string)itemArray[0][0];  //location 0,0 happens to be where the name of the object is
                    //check for a ':' in the name and replace with an underscore
                    if (tempName.Contains(":"))
                    {
                        tempName = tempName.Replace(":", "");
                    }

                    diagrammer.Draw(rootNode, documentGlobalSettings.OutputDiagramPath + tempName + ".png");
                }
            }

        }



        /// <summary>
        /// Writes out an entire object, common element, common type, message, etc. 
        /// The IF statements handle various kind of objects, etc. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="itemArray"></param>
        /// <param name="isTyped"></param>
        /// <param name="isElement"></param>
        /// <param name="rootIterator"></param>
        void WriteDefinition(string name, object[][] itemArray, bool isTyped, bool isElement, XPathNodeIterator rootIterator)
        {
            currentNode = null;

            XPathNodeIterator iterator = (XPathNodeIterator)itemArray[0][1];

            if (generateXsdSettings.SingleSchema)
            {
                WriteNewLine();
                //write a comment line in the file with the name of the object.
                writer.WriteComment(name);
                WriteNewLine();
            }

            string typedName = name;

            if (!isTyped)
            {
                typedName += "Type";
            }

            string drawTypeName = typedName;

            if (typedName == name)
            {
                drawTypeName = "";
            }

            //-----------------
            //SEQUENCE
            //-----------------
            if (IsSequence(itemArray))
            {
                rootNode = currentNode = new Draw.Element(name, drawTypeName);

                //open xs:complexType tag
                writer.WriteStartElement("xs:complexType");
                writer.WriteAttributeString("name", typedName);
                WriteAnnotation(iterator);
                {
                    int itemArrayLength = itemArray.Length;
                    WriteSequence(itemArray, 1, ref itemArrayLength, 1);
                    WriteAttributes(itemArray, 1);
                }

                //close xs:complexType tag
                writer.WriteEndElement();
            }

            //-----------------
            //CHOICE
            //-----------------
            //This code is ONLY REACHED BY Infrastructure objects and messages.  Data Model and service body definitions are handled
            //within the Sequence Code Block above
            else if (IsChoice(itemArray))
            {
                rootNode = currentNode = new Draw.Element(name, drawTypeName);

                //open xs:complexType tag
                writer.WriteStartElement("xs:complexType");

                writer.WriteAttributeString("name", typedName);
                //WriteAnnotation(iterator);

                if (iterator != null)
                {
                    WriteType(itemArray, 0, "", true);
                    //WriteAttributes(itemArray, 1);
                }

                //close xs:complexType tag
                writer.WriteEndElement();
            }

            //-----------------
            //EMPTY
            //  This is used for system messages that have no payload such as
            //  SIF_Ping, SIF_Sleep, SIF_GetMessage, etc. 
            //-----------------
            else if (IsEmpty(itemArray))
            {
                rootNode = currentNode = new Draw.Element(name, "EMPTY");

                writer.WriteStartElement("xs:complexType");
                writer.WriteAttributeString("name", typedName);
                WriteAnnotation(iterator);
                WriteAttributes(itemArray, 1);
                writer.WriteEndElement();
            }

            //-----------------
            //HAS ATTRIBUTES
            //-----------------
            else if (HasAttributes(itemArray))
            {
                rootNode = currentNode = new Draw.Element(name, drawTypeName);

                writer.WriteStartElement("xs:complexType");
                writer.WriteAttributeString("name", typedName);

                if (iterator != null)
                {   
                    WriteType(itemArray, 0, "", true);
                }

                writer.WriteEndElement();
            }

            //-----------------
            //EVERYTHING ELSE
            //-----------------
            else
            {
                bool complexContent = false;

                XPathNodeIterator complexContentIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Type/child::node()[namespace-uri(.)='http://www.w3.org/2001/XMLSchema']");

                if (complexContentIterator.MoveNext())
                {
                    complexContent = true;
                }

                XPathNodeIterator complexIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Type/@complex");

                string complexValue = "";

                if (complexIterator.MoveNext())
                {
                    complexValue = complexIterator.Current.Value;
                }

                bool isComplex = (complexValue == "restriction" || complexValue == "extension");

                if (isComplex)
                {
                    rootNode = currentNode = new Draw.Element(name, drawTypeName);

                    writer.WriteStartElement("xs:complexType");
                    writer.WriteAttributeString("name", typedName);
                    writer.WriteStartElement("xs:complexContent");
                }
                else if (complexContent)
                {
                    rootNode = currentNode = new Draw.Element(name, drawTypeName);

                    writer.WriteStartElement("xs:complexType");
                    writer.WriteAttributeString("name", typedName);
                }
                else
                {
                    rootNode = currentNode = new Draw.Element(name, drawTypeName);

                    writer.WriteStartElement("xs:simpleType");
                    writer.WriteAttributeString("name", typedName);
                }

                //WriteAnnotation(iterator);

                if (iterator != null)
                {
                    if (isComplex)
                    {
                        WriteType(itemArray, 0, complexValue, true);
                    }
                    else if (complexContent)
                    {
                        WriteType(itemArray, 0, "", true);
                    }
                    else
                    {
                        WriteType(itemArray, 0, "restriction", true);
                    }
                }

                if (isComplex)
                {
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            WriteNewLine();

            //When writing the no-includes type of XSD we use the Garden of Eden design pattern (like Venetian Blind but has a single
            //global root element, in our case it is SIF_Message. In this pattern the element is defined as a type, e.g., StudentPersonalType,
            //with all the detail. Then the element is defined using the type, e.g., StudentPersonal is of type StudentPersonalType. This allows
            //for reuse of objects and elements within other objects and/or elements.
            //*********************************************************************************
            //This part writes the element in terms of the element type previously written out.
            //**********************************************************************************
            if (isElement)
            {
                string elementName = (string)itemArray[0][0];

                writer.WriteStartElement("xs:element");
                writer.WriteAttributeString("name", elementName);

                //don't do this if the element is not in the default namespace (sif:). If an element is not in the default
                //namespace, then the input xml should have a qualified name with the prefix, e.g., sif3:Assessment. 

                if (elementName.Contains(":"))  //if the name contains a colon the it must already be using a namespace prefix
                    { writer.WriteAttributeString("type", elementName + "Type"); }
                else
                    { writer.WriteAttributeString("type", "sif:" + elementName + "Type"); } 
                

                XPathNodeIterator elementIterator = (XPathNodeIterator)itemArray[0][1];

                if (elementIterator != null)
                {
                    if (actionListConstraints || generateXsdSettings.ListWithKeysConstraints)
                    {
                        string modePredicate = GetListModePredicate();

                        XPathNodeIterator listIterator = inputDocumentManager.Select(elementIterator.Current, "xhtml:List[" + modePredicate + "]");

                        if (listIterator.MoveNext())
                        {
                            string[] listKeys = GetListKeys(listIterator);

                            if (listKeys != null)
                            {
                                WriteListKeys(elementName, listKeys);
                            }
                        }
                    }
                }

                if (inDataObject && rootIterator != null)
                {
                    string[] objectKeys = GetListKeys(rootIterator);

                    if (objectKeys != null)
                    {
                        WriteObjectKeys(elementName, objectKeys);
                    }
                }

                writer.WriteEndElement();

                WriteNewLine();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        void WriteCollectionDefinition(string name)
        {
            writer.WriteStartElement("xs:complexType");
                writer.WriteAttributeString("name", name+"CollectionType");
                writer.WriteStartElement("xs:sequence");
                    writer.WriteStartElement("xs:element");
                        writer.WriteAttributeString("name", name);
                        writer.WriteAttributeString("type", "sif:"+name+"Type");
                        writer.WriteAttributeString("minOccurs", "0");
                        writer.WriteAttributeString("maxOccurs", "unbounded");
                    writer.WriteEndElement();
                writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement("xs:element");
                writer.WriteAttributeString("name", name+"s");
                writer.WriteAttributeString("type", "sif:"+name+"CollectionType");
            writer.WriteEndElement();
            WriteNewLine();
        }
         //output should look like this
         /* <xs:complexType name="personCollectionType">
		        <xs:sequence>
			        <xs:element name="person" type="personType" minOccurs="0" maxOccurs="unbounded"/>
		        </xs:sequence>
	        </xs:complexType>
	        <xs:element name="persons" type="personCollectionType"/>
          */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementName"></param>
        /// <param name="keys"></param>
        void WriteObjectKeys(string elementName, string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                return;
            }

            string Selector = ".";

            if (generateXsdSettings.IsDataModelXSD || generateXsdSettings.IsSIFMessage2XSD || (inInfrastructure && !inDataObject))
            {
                writer.WriteStartElement("xs:key");
            }
            else
            {
                writer.WriteStartElement("xs:unique");
            }

            writer.WriteAttributeString("name", GetListKeyName(elementName));
            writer.WriteStartElement("xs:selector");
            writer.WriteAttributeString("xpath", Selector);
            writer.WriteEndElement();

            foreach (string nextKeyPath in keys)
            {
                string field = nextKeyPath.Replace("/", "/sif:");

                if (field[0] != '@')
                {
                    field = "./sif:" + field;
                }

                writer.WriteStartElement("xs:field");
                writer.WriteAttributeString("xpath", field);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementName"></param>
        /// <returns></returns>
        string GetListKeyName(string elementName)
        {
            if (!keyNames.ContainsKey(elementName))
            {
                keyNames.Add(elementName, 0);
            }

            int value = keyNames[elementName];
            keyNames[elementName] = ++value;

            return elementName + "Key" + value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementName"></param>
        /// <param name="keys"></param>
        void WriteListKeys(string elementName, string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                return;
            }

            string keyPath = keys[0];

            int index = keyPath.LastIndexOf('/');

            if (index == -1)
            {
                index = keyPath.Length;
            }

            string Selector = keyPath.Substring(0, index);
            Selector = Selector.Replace("/", "/sif:");
            Selector = "./sif:" + Selector;

            if (generateXsdSettings.IsDataModelXSD || (inInfrastructure && !inDataObject))
            {
                writer.WriteStartElement("xs:key");
            }
            else
            {
                writer.WriteStartElement("xs:unique");
            }

            writer.WriteAttributeString("name", GetListKeyName(elementName));
            writer.WriteStartElement("xs:selector");
            writer.WriteAttributeString("xpath", Selector);
            writer.WriteEndElement();

            if (keys.Length > 1)
            {
                //Is this incomplete?
                //Selector = Selector;
            }

            foreach (string nextKeyPath in keys)
            {
                index = nextKeyPath.LastIndexOf('/');

                if (index == -1)
                {
                    index = nextKeyPath.Length;
                }

                string field = "";

                if (index < nextKeyPath.Length)
                {
                    field = nextKeyPath.Substring(index + 1);
                }

                if (field.Length == 0)
                {
                    field = ".";
                }
                else if (field[0] != '@')
                {
                    field = "./sif:" + field;
                }

                writer.WriteStartElement("xs:field");
                writer.WriteAttributeString("xpath", field);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listOrObjectIterator"></param>
        /// <returns></returns>
        string[] GetListKeys(XPathNodeIterator listOrObjectIterator)
        {
            XPathNodeIterator keyIterator = inputDocumentManager.Select(listOrObjectIterator.Current, "xhtml:Key");

            ArrayList keys = new ArrayList();

            while (keyIterator.MoveNext())
            {
                keys.Add(keyIterator.Current.Value);
            }

            if (keys.Count > 0)
            {
                return (string[])keys.ToArray(typeof(string));
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string GetListModePredicate()
        {
            string modePredicate = "";

            if (actionListConstraints)
            {
                modePredicate += "@mode='ActionList'";
            }

            if (generateXsdSettings.ListWithKeysConstraints)
            {
                if (modePredicate.Length > 0)
                {
                    modePredicate += " or ";
                }

                modePredicate += "@mode='List'";
            }

            return modePredicate;
        }

        /// <summary>
        /// Tests the itemArray to see if it is a sequence
        /// (Vince: not sure this will stand. It looks for a backslash at the beginning of the item name)
        /// </summary>
        /// <param name="itemArray"></param>
        /// <returns>True or False</returns>
        bool IsSequence(object[][] itemArray)
        {
            for (int i = 0; i < itemArray.Length; ++i)
            {
                string name = (string)itemArray[i][0];

                if (name[0] == '/')
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iterator"></param>
        void WriteAnnotation(XPathNodeIterator iterator)
        {
            if (!generateXsdSettings.Annotate || iterator == null) return;

            XPathNodeIterator descriptionIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Description/child::node()");

            if (descriptionIterator.MoveNext())
            {
                //writer.WriteNode(new Developmentor.Xml.NavigatorReader(descriptionIterator.Current), true);
                writer.WriteStartElement("xs:annotation");
                writer.WriteStartElement("xs:documentation");
                writer.WriteString(descriptionIterator.Current.Value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        bool needNilAttribute = false;



        /// <summary>
        /// Writes the XML Attributes for an Element. 
        /// Takes an itemArray and a start index.  Assumption is that the startIndex points to an attribute.
        /// Writes out attributes until the next item is not an attribute. 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="startIndex"></param>
        void WriteAttributes(object[][] itemArray, int startIndex)
        {
            if (generateXsdSettings.AddNilAttributes && this.needNilAttribute)
            {
                writer.WriteStartElement("xs:attribute");
                writer.WriteAttributeString("ref", "xsi:nil");
                writer.WriteAttributeString("use", "optional");
                writer.WriteAttributeString("default", "false");
                writer.WriteEndElement();
            }

            ArrayList attributes = new ArrayList();

            if (startIndex < 0 || startIndex >= itemArray.Length) return;

            for (int i = startIndex; i < itemArray.Length; ++i)
            {
                string name = (string)itemArray[i][0];
                XPathNodeIterator iterator = (XPathNodeIterator)itemArray[i][1];

                if (name[0] != '@') break;

                name = name.Substring(1);

                XmlTextWriter tempWriter = writer;
                StringWriter stringWriter = null;

                if (name == "xmlns" || name.IndexOf("xmlns:") == 0 || name.IndexOf("xsi:") == 0)
                {
                    stringWriter = new StringWriter();
                    writer = new XmlTextWriter(stringWriter);
                    writer.Formatting = Formatting.Indented;
                }

                writer.WriteStartElement("xs:attribute");

                if (name.IndexOf(":") == -1)
                {
                    writer.WriteAttributeString("name", name);
                }
                else
                {
                    writer.WriteAttributeString("ref", name);
                }

                XPathNodeIterator characteristicsIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Characteristics");

                string use = "required";

                if (characteristicsIterator.MoveNext())
                {
                    if (characteristicsIterator.Current.Value == "O" || characteristicsIterator.Current.Value == "C")
                    {
                        use = "optional";
                    }
                }

                //open the xs:attribute tag
                writer.WriteAttributeString("use", use);

                currentAttribute = new Draw.Attribute(name, "");

                if (use != "required")
                {
                    currentAttribute.MinOccurs = "0";
                }

                //write the type inside of the tag
                WriteType(itemArray, i);

                attributes.Add(currentAttribute);
                currentAttribute = null;

               
                //WriteAnnotation(iterator);

                //close the xs:attribute tag
                writer.WriteEndElement();

                if (name == "xmlns" || name.IndexOf("xmlns:") == 0 || name.IndexOf("xsi:") == 0)
                {
                    writer.Close();
                    writer = tempWriter;
                    tempWriter.WriteComment(stringWriter.ToString());
                }
            }

            if (currentNode is Draw.Element)
            {
                (currentNode as Draw.Element).Attributes = (Draw.Attribute[])attributes.ToArray(typeof(Draw.Attribute));
            }
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        void WriteType(object[][] itemArray, int index)
        {
            WriteType(itemArray, index, "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="complexDerivation"></param>
        void WriteType(object[][] itemArray, int index, string complexDerivation)
        {
            WriteType(itemArray, index, complexDerivation, false);
        }

        /// <summary>
        /// Writes an XSD sequence fragment. 
        /// Also includes logic to write xs:choice for data model objects
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="level"></param>
        void WriteSequence(object[][] itemArray, int startIndex, ref int endIndex, int level)
        {
            Draw.Node parentNode = currentNode;

            bool inChoice = false;
            bool lastChoiceChild = false;
            Draw.Element choiceElement = null;
            Draw.Node choiceParent = null;
            int choiceStartIndex = -1;
            //marks the level of a choice tag. Can be used to tell if we are whithin
            //a choice tag at the same or deeper level. 
            int localChoiceLevel = -1; 

            
            //OUTERMOST TAG FOR THIS METHOD
            //open xs:sequence tag
            writer.WriteStartElement("xs:sequence");

            string lastName = "";
            bool wroteEndElement = true;
            int i;
            int lastSlash = -1;

            int attributeIndex = -1;
            int lastIndexAtLevel = -1;

            //----------------------------------------------------------
            //Loop through the itemArray 
            //----------------------------------------------------------
            for (i = startIndex; i < endIndex; ++i)
            {
                string itemName = (string)itemArray[i][0];
                XPathNodeIterator item = (XPathNodeIterator)itemArray[i][1];

                //If this item is an attribute rather than an element save the index location
                //and go to the top of the loop.
                if (itemName[0] == '@')
                {
                    if (i > 1 && attributeIndex == -1)
                    {
                        attributeIndex = i;
                    }

                    continue;
                }

                //Look for the index position (zero-based) of the last slash in the name.  -1 = not found. 
                lastSlash = itemName.LastIndexOf('/');

                //if there are no slashes in the itemName then we have a root-level element
                //and we go to the top of the loop. 
                if (lastSlash == -1)
                {
                    continue;
                }

                //----------------------------------------------------------
                //One - Handles elements that are at the current level
                //----------------------------------------------------------
                if (lastSlash + 1 == level)
                {
     
                    if (!wroteEndElement)
                    {
                        if (lastIndexAtLevel != -1)
                        {
                            string itemName2 = (string)itemArray[lastIndexAtLevel][0];
                            XPathNodeIterator item2 = (XPathNodeIterator)itemArray[lastIndexAtLevel][1];

                            string elementName2 = itemName2.Substring(level);

                            if (item2 != null)
                            {
                                //looks like this handles lists
                                if (actionListConstraints || generateXsdSettings.ListWithKeysConstraints)
                                {
                                    string modePredicate = GetListModePredicate();

                                    XPathNodeIterator listIterator = inputDocumentManager.Select(item2.Current, "xhtml:List[" + modePredicate + "]");

                                    if (listIterator.MoveNext())
                                    {
                                        string[] listKeys = GetListKeys(listIterator);

                                        if (listKeys != null)
                                        {
                                            WriteListKeys(elementName2, listKeys);
                                        }
                                    }
                                }
                            }
                        }

                       
                        //close xs:element tag of the previous element at this level
                        writer.WriteEndElement();

                    }

                    //----------------------------------------------------------------------------------------------------
                    //Handle xs:choice by looking back at the parent element.  If it is a Choice tag then go into this routine.
                    //  This handles choices of elements not choices of types as in an enumeration. 
                    //----------------------------------------------------------------------------------------------------
                    lastChoiceChild = false;
                    lastIndexAtLevel = i;

                    {   
                        //the following statement does not work well when we are at level 2 or deeper because
                        //from the input xml perspective all <item> tags are at the root unless they are
                        //nested within the <choice> tag.  Therefore any <item>, no matter what the level, that
                        //is nested inside a <choice> tag will have <choice> as its parent. 
                        
                        XPathNodeIterator parentIterator = inputDocumentManager.Select(item.Current, "..");

                        if (parentIterator.MoveNext() && parentIterator.Current.LocalName == "Choice")
                        {
                            XPathNodeIterator previousIterator = inputDocumentManager.Select(item.Current, "preceding-sibling::xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                            bool firstChild = !previousIterator.MoveNext();
                            
                            //the IF below may need to be revised if we ever have nested xs:choice tags
                            if (firstChild)
                            {
                                //if a new choice tag is detected then save the alter the value of choiceLevel to be the level of the choice tag.
                                //choiceLevel will be passed down to the next level. 
                                localChoiceLevel = level; 

                                //This allows the input xml to specify the choice as optional,
                                //e.g., optional="true"
                                string optional = parentIterator.Current.GetAttribute("optional", "");

                                //if (!this.inDataObject) //**this was a bug that kept data model objects from getting xs:choice in their schemas
                                {   
                                    //open xs:choice tag
                                    writer.WriteStartElement("xs:choice");

                                    //if the entire choice is optional then mark as such.
                                    if (optional == "true")
                                    {
                                        writer.WriteAttributeString("minOccurs", "0");
                                    }
                                }

                                choiceStartIndex = i;
                                choiceElement = new Draw.Element("", "");
                                choiceElement.ContentModel = Draw.ContentModel.CHOICE;
                                if (optional == "true") choiceElement.MinOccurs = "0";
                                (parentNode as Draw.Element).children.Add(choiceElement);
                                choiceParent = parentNode;
                                parentNode = choiceElement;

                                inChoice = true;
                            }
                        }
                    }

                    string elementName = itemName.Substring(level);

                    
                    Draw.Element element = new Draw.Element(elementName, "");
                    (parentNode as Draw.Element).children.Add(element);
                    currentNode = element;

                    string minOccurs = "";
                    string maxOccurs = "";
                    string nillable = "";

                    string diagramMinOccurs = "";

                    //----------------------------------------------------------
                    //Handle translating the SIF characteristics to XSD language
                    //This section sets the values
                    //----------------------------------------------------------
                    this.needNilAttribute = false;

                    XPathNodeIterator characteristicsIterator = inputDocumentManager.Select(item.Current, "xhtml:Characteristics");

                    if (characteristicsIterator.MoveNext())
                    {
                        string characteristics = characteristicsIterator.Current.Value.Trim();

                        //--------------------------
                        if (characteristics == "M")
                        { 
                            //This traps the case where a data model object is in the 
                            //SIF_Message file.  The Not-IsDataModelXSD won't work when we
                            //have more than two types: infrastructure or data model
                            if (this.inDataObject && !generateXsdSettings.IsDataModelXSD && !this.inServiceBodyObject)
                            {
                                minOccurs = "0";
                            }

                            //make sure common elements in SIF_Message are optional
                            if (this.inCommonElement && !generateXsdSettings.IsDataModelXSD && !this.inInfrastructure)
                            {
                                minOccurs = "0";
                            }

                            //make sure Service Body elements in SIF_Message (where SingleSchema is true) are optional
                            if (this.inServiceBodyObject && !generateXsdSettings.IsDataModelXSD && generateXsdSettings.SingleSchema)
                            {
                                minOccurs = "0";
                            }


                            //the above logic is repeated below for the "MR" case

                        }
                        //--------------------------
                        else if (characteristics == "O")
                        {
                            minOccurs = "0";

                            if (!this.inInfrastructure)
                            {
                                if (!generateXsdSettings.AddNilAttributes) nillable = "true";
                                this.needNilAttribute = true;
                            }

                            diagramMinOccurs = "0";
                        }
                        //--------------------------
                        //TODO - needs work
                        else if (characteristics == "C")
                        {
                            if (this.inDataObject || !inChoice)
                            {
                                minOccurs = "0";

                                if (!this.inInfrastructure)
                                {
                                    if (!generateXsdSettings.AddNilAttributes) nillable = "true";
                                    this.needNilAttribute = true;
                                }
                                if (!inChoice)
                                {
                                    diagramMinOccurs = "0";
                                }
                            }

                        }
                        //--------------------------
                        else if (characteristics == "MR")
                        {
                            maxOccurs = "unbounded";

                            //This traps the case where a data model object is in the 
                            //SIF_Message file.  The Not-IsDataModelXSD won't work when we
                            //have more than two types: infrastructure or data model
                            if (this.inDataObject && !generateXsdSettings.IsDataModelXSD && !this.inServiceBodyObject)
                            {
                                minOccurs = "0";
                            }

                            //make sure common elements in SIF_Message are optional
                            if (this.inCommonElement && !generateXsdSettings.IsDataModelXSD && !this.inInfrastructure)
                            {
                                minOccurs = "0";
                            }

                            //make sure Service Body elements in SIF_Message (where SingleSchema is true) are optional
                            if (this.inServiceBodyObject && !generateXsdSettings.IsDataModelXSD && generateXsdSettings.SingleSchema)
                            {
                                minOccurs = "0";
                            }


                            //if (this.inDataObject && !generateXsdSettings.IsDataModelXSD)
                            //{
                            //    minOccurs = "0";
                            //}
                        }
                        //--------------------------
                        else if (characteristics == "OR")
                        {
                            minOccurs = "0";
                            maxOccurs = "unbounded";
                            diagramMinOccurs = "0";
                        }
                        //--------------------------
                        else if (characteristics == "CR")
                        {
                            maxOccurs = "unbounded";

                            if (this.inDataObject || !inChoice)
                            {
                                minOccurs = "0";

                                if (!inChoice)
                                {
                                    diagramMinOccurs = "0";
                                }
                            }
                        }
                        //--------------------------
                        else
                        {
                            System.Console.WriteLine("invalid characteristics " + characteristics + " in " + elementName + "!!!");
                        }
                    }
                    //-----------------------------------------
                    // no characteristic defined for the element
                    //------------------------------------------
                    else
                    {
                        System.Console.WriteLine("no characteristics in " + elementName + "!!!");
                    }

                    //----------------------------------------------------------
                    //This section takes the values and writes out the XSD language
                    //----------------------------------------------------------
                    XPathNodeIterator externalTypeIterator = inputDocumentManager.Select(item.Current, "xhtml:ExternalType");
                    XPathNodeIterator typeIterator = inputDocumentManager.Select(item.Current, "xhtml:Type[@ref and @name and not(contains(@name, ':'))]/@name");

                    bool externallyTyped = externalTypeIterator.MoveNext();
                    bool isRootSIFElement = typeIterator.MoveNext() && IsRootSIFElement(typeIterator.Current.Value);

                    if (isRootSIFElement)
                    {
                        if (HasAttributes(itemArray, i + 1))
                        {
                            isRootSIFElement = false;
                        }
                        else if (typeIterator.Current.Value != elementName)
                        {
                            isRootSIFElement = false;
                        }
                    }

                    if (nillable == "")
                    {
                        XPathNodeIterator elementIterator = inputDocumentManager.Select(item.Current, "xhtml:Element");

                        if (elementIterator.MoveNext())
                        {
                            if (elementIterator.Current.GetAttribute("nillable", "") == "true")
                            {
                                nillable = "true";
                            }
                        }
                    }

                    if (!externallyTyped)
                    {
                        //open xs:element tag for the current element
                        writer.WriteStartElement("xs:element");

                        if (!isRootSIFElement || nillable != "")
                        {
                            if (elementName.IndexOf(" ") == -1)
                            {
                                //write the attributes to the element tag
                                writer.WriteAttributeString("name", elementName);
                            }
                        }
                        else
                        {
                            externallyTyped = true;

                            string name = typeIterator.Current.Value;

                            writer.WriteAttributeString("ref", "sif:" + name);

                            currentNode.Type = "sif:" + name;
                        }
                    }
                    else
                    {
                        string type = externalTypeIterator.Current.GetAttribute("type", "");
                        string name = externalTypeIterator.Current.GetAttribute("name", "");
                        string prefix = externalTypeIterator.Current.GetAttribute("prefix", "");

                        //open xs:element tag
                        writer.WriteStartElement("xs:element");
                        writer.WriteAttributeString("ref", prefix + ":" + name);

                        currentNode.Type = prefix + ":" + name;
                    }

                    if (minOccurs != "")
                    {
                        writer.WriteAttributeString("minOccurs", minOccurs);
                    }

                    if (maxOccurs != "")
                    {
                        writer.WriteAttributeString("maxOccurs", maxOccurs);
                    }

                    if (nillable != "")
                    {
                        writer.WriteAttributeString("nillable", nillable);
                    }

                    if (currentNode != null && diagramMinOccurs != "")
                    {
                        currentNode.MinOccurs = diagramMinOccurs;
                    }

                    if (currentNode != null && maxOccurs != "")
                    {
                        currentNode.MaxOccurs = maxOccurs;
                    }

                    wroteEndElement = false;

                    if (!externallyTyped)
                    {
                        //WriteType() also closes the xs:element tag
                        WriteType(itemArray, i);

                        this.needNilAttribute = false;
                        //WriteAttributes(itemArray, i + 1);
                        attributeIndex = -1;
                    }

                  
                    //WriteAnnotation(item);

                   
                }

                //----------------------------------------------------------
                //Two - If the next element has a deeper level than
                // the current level, then we recursively call WriteSequence() at the 
                // next lower level. 
                //----------------------------------------------------------
                else if (lastSlash + 1 > level)
                {
                    int endChildIndex = endIndex;

                    //open xs:complexType tag
                    writer.WriteStartElement("xs:complexType");
                    {
                        WriteSequence(itemArray, i, ref endChildIndex, level + 1);

                        if (attributeIndex > 1)
                        {
                            WriteAttributes(itemArray, attributeIndex);
                        }

                        attributeIndex = -1;
                    }
                    //close xs:complexType Tag
                    writer.WriteEndElement();

                    if (lastIndexAtLevel != -1)
                    {
                        string itemName2 = (string)itemArray[lastIndexAtLevel][0];
                        XPathNodeIterator item2 = (XPathNodeIterator)itemArray[lastIndexAtLevel][1];

                        string elementName2 = itemName2.Substring(level);

                        if (item2 != null)
                        {
                            if (actionListConstraints || generateXsdSettings.ListWithKeysConstraints)
                            {
                                string modePredicate = GetListModePredicate();

                                XPathNodeIterator listIterator = inputDocumentManager.Select(item2.Current, "xhtml:List[" + modePredicate + "]");

                                if (listIterator.MoveNext())
                                {
                                    string[] listKeys = GetListKeys(listIterator);

                                    if (listKeys != null)
                                    {
                                        WriteListKeys(elementName2, listKeys);
                                    }
                                }
                            }
                        }
                    }

                    //close xs:element tag
                    writer.WriteEndElement();
                    wroteEndElement = true;

                    if (choiceStartIndex != -1)
                    {
                        for (int j = endChildIndex - 1; j >= choiceStartIndex; --j)
                        {
                            if (IsLastChoiceChild(itemArray, j))
                            {
                                parentNode = choiceParent;
                                choiceStartIndex = -1;
                                break;
                            }
                        }
                    }

                    i = endChildIndex - 1;
                }

                //----------------------------------------------------------
                //Three - If the next element is at a higher level than the current level
                //   then we break out of the loop.
                //----------------------------------------------------------
                else if (lastSlash + 1 < level)
                {
                    break;
                }

              //End of one, two, and three
              lastName = itemName;

              //----------------------------------------------------------
              //Handle the last choice in a choice set
              //----------------------------------------------------------

               //the following statement does not work well when we are at level 2 or deeper because
              //from the input xml perspective all <item> tags are at the root unless they are
              //nested within the <choice> tag.  Therefore any <item>, no matter what the level, that
              //is nested inside a <choice> tag will have <choice> as its parent. 
            
              XPathNodeIterator parentIterator2 = inputDocumentManager.Select(item.Current, "..");

              if (parentIterator2.MoveNext() && parentIterator2.Current.LocalName == "Choice")
              {
                  //XPathNodeIterator previousIterator = inputDocumentManager.Select(item.Current, "preceding-sibling::xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");
                  XPathNodeIterator nextIterator = inputDocumentManager.Select(item.Current, "following-sibling::xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                  //bool firstChild = !previousIterator.MoveNext();
                  //the statement below is problematic because it will be true even if
                  //we are at level 2, 3, etc. 
                  lastChoiceChild = !nextIterator.MoveNext();


                  //The lastChoiceChild test is not enough here. We don't want to go into this block if we are not at the same 
                  //  level as the choice we are dealing with.
                  if (lastChoiceChild && localChoiceLevel == level)
                  {
                      //if (!this.inDataObject) //this is commented out as above in this method because data objects are treated like all other objects and messages in the WriteSequence() method. 
                      {
                          //close xs:choice tag
                          writer.WriteEndElement();
                      }

                      if (choiceElement != null)
                      {
                          parentNode = choiceParent;
                      }

                      inChoice = false;
                      choiceStartIndex = -1;
                  }


              }


             



            } //----------End of itemArray Loop---------------------
              //----------------------------------------------------



            //----------------------------------------------------------
            //Do some cleanup, and then pop up to the 
            //   level above if there is one. 
            //----------------------------------------------------------

            endIndex = i;

            if (!wroteEndElement)
            {
                //close xs:element tag
                writer.WriteEndElement();
            }

            //OUTERMOST TAG FOR THIS METHOD
            //close xs:sequence tag
            writer.WriteEndElement();

            /*
			if (level > 1)
			{
				//Ending complex type
				writer.WriteEndElement();
			}
			*/

            if (parentNode != null)
            {
                //(parentNode as Draw.Element).Children = (Draw.Element[])elements.ToArray(typeof(Draw.Element));
                currentNode = parentNode;
            }
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        bool IsLastChoiceChild(object[][] itemArray, int index)
        {
            XPathNodeIterator item = (XPathNodeIterator)itemArray[index][1];

            XPathNodeIterator parentIterator = inputDocumentManager.Select(item.Current, "..");

            if (parentIterator.MoveNext() && parentIterator.Current.LocalName == "Choice")
            {
                XPathNodeIterator nextIterator = inputDocumentManager.Select(item.Current, "following-sibling::xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                return !nextIterator.MoveNext();
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool IsRootSIFElement(string type)
        {
            XPathNodeIterator iterator = inputDocumentManager.Select(navigator, "//xhtml:CommonElement[not(@type='true') and @name='" + type + "']");

            if (iterator.MoveNext())
            {
                return true;
            }

            iterator = inputDocumentManager.Select(navigator, "//xhtml:DataObject[@name='" + type + "']");

            if (iterator.MoveNext())
            {
                return true;
            }

            iterator = inputDocumentManager.Select(navigator, "//xhtml:Message[@name='" + type + "']");

            return iterator.MoveNext();
        }

        /// <summary>
        /// Tests the itemArray to see if it is an array of choices.
        /// </summary>
        /// <param name="itemArray"></param>
        /// <returns>true or false</returns>
        bool IsChoice(object[][] itemArray)
        {
            XPathNodeIterator iterator = (XPathNodeIterator)itemArray[0][1];

            if (iterator != null && inputDocumentManager.Select(iterator.Current, "xhtml:Choice").MoveNext())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <param name="typeIterator"></param>
        /// <param name="hasAttributes"></param>
        void WriteEmptyType(object[][] itemArray, int index, XPathNodeIterator item, XPathNodeIterator typeIterator, bool hasAttributes)
        {
            if (currentNode != null)
            {
                currentNode.Type = "EMPTY";
            }

           
            WriteAnnotation(item);
            writer.WriteStartElement("xs:complexType");

            if (hasAttributes)
            {
                WriteAttributes(itemArray, index + 1);
            }

            writer.WriteEndElement();

        }


        /// <summary>
        /// Writes a type information to the XSD. 
        /// This can be a type declaration within a tag or a complete node depending upon the situation.
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="complexDerivation"></param>
        /// <param name="suppress"></param>
        void WriteType(object[][] itemArray, int index, string complexDerivation, bool suppress)
        {
            XPathNodeIterator item = (XPathNodeIterator)itemArray[index][1];
            string itemName = (string)itemArray[index][0];

            if (item == null) return;

            XPathNodeIterator valuesIterator = inputDocumentManager.Select(item.Current, "xhtml:Values[@type='subset' or @type='additional']");

            if (valuesIterator.MoveNext())
            {
                if (valuesIterator.Current.GetAttribute("type", "") == "subset")
                {
                    WriteValuesSubsetType(item, itemName, itemArray, index, suppress);
                }
                else
                {
                    WriteValuesAdditionalType(item, itemName, itemArray, index, suppress);
                }

                return;
            }

            bool isChoice = false;
            bool isUnion = false;

            XPathNodeIterator typeIterator = inputDocumentManager.Select(item.Current, "child::node()[local-name(.) = 'Choice' or local-name(.) = 'Union']");

            if (typeIterator.MoveNext())
            {
                isChoice = (typeIterator.Current.LocalName == "Choice");
                isUnion = (typeIterator.Current.LocalName == "Union");
            }

            typeIterator = inputDocumentManager.Select(item.Current, "xhtml:Type");

            bool isAttribute = (itemName[0] == '@');
            bool hasAttributes = !isAttribute && HasAttributes(itemArray, index + 1);

            //What is going on here? 
            if (!isAttribute && generateXsdSettings.AddNilAttributes && this.needNilAttribute)
            {
                hasAttributes = true;
            }

            string typeAttribute = "type";
            string complex = "";

            if (typeIterator.MoveNext())
            {
                complex = typeIterator.Current.GetAttribute("complex", "");
            }

            typeIterator = inputDocumentManager.Select(item.Current, "xhtml:Type");

            if (complexDerivation != "")
            {
                typeAttribute = "base";

                if (!isUnion)
                {
                    writer.WriteStartElement("xs:" + complexDerivation);
                }
            }
            else if (complex != "")
            {

            }

            if (itemName.IndexOf(" ") != -1)
            {
                typeAttribute = "ref";
            }

            if (typeIterator.MoveNext())
            {
                string type = typeIterator.Current.GetAttribute("name", "");


                XPathNodeIterator definedIterator = inputDocumentManager.Select(typeIterator.Current, "child::node()[namespace-uri(.)='http://www.w3.org/2001/XMLSchema']");

                if (definedIterator.MoveNext())
                {
                    string diagramOutputType = "xs:" + definedIterator.Current.LocalName + "...";

                    if (currentAttribute != null)
                    {
                        currentAttribute.Type = diagramOutputType;
                    }
                    else if (currentNode != null)
                    {
                        currentNode.Type = diagramOutputType;
                    }

                    if (definedIterator.Current.LocalName == "any")
                    {
                        if (!suppress) writer.WriteStartElement("xs:complexType");

                        if (hasAttributes && !generateXsdSettings.IsDataModelXSD && (inDataObject || (inCommonElement && !inInfrastructure)) && !generateXsdSettings.IsSIFMessage2XSD)
                        {
                            StringWriter stringWriter = new StringWriter();
                            XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);
                            xmlWriter.Formatting = Formatting.Indented;

                            xmlWriter.WriteStartElement("xs:sequence");
                            xmlWriter.WriteNode(new Developmentor.Xml.NavigatorReader(definedIterator.Current), false);
                            xmlWriter.WriteEndElement();

                            xmlWriter.Close();

                            string anySequence = stringWriter.ToString();

                            int minIndex = anySequence.IndexOf("minOccurs=");

                            if (minIndex != -1)
                            {
                                anySequence = anySequence.Substring(0, minIndex + 11) + "0" + anySequence.Substring(minIndex + 12);
                            }
                            else
                            {
                                minIndex = anySequence.IndexOf("/>");

                                if (minIndex != -1)
                                {
                                    anySequence = anySequence.Substring(0, minIndex) + " minOccurs=\"0\" maxOccurs=\"1\"" + anySequence.Substring(minIndex);
                                }
                            }

                            writer.WriteRaw("\r\n" + anySequence + "\r\n");
                        }
                        else
                        {
                            writer.WriteStartElement("xs:sequence");
                            writer.WriteNode(new Developmentor.Xml.NavigatorReader(definedIterator.Current), false);
                            writer.WriteEndElement();
                        }

                        if (hasAttributes)
                        {
                            WriteAttributes(itemArray, index + 1);
                        }

                        if (!suppress) writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteNode(new Developmentor.Xml.NavigatorReader(definedIterator.Current), false);
                    }
                }
                else if (type.IndexOf("xs:") == 0)
                {
                    WriteXSType(itemArray, index, item, typeIterator, hasAttributes, suppress, complex, type, typeAttribute);
                }
                else if (type.IndexOf(":") == -1)
                {
                    if (type != "EMPTY")
                    {
                        WriteSIFType(itemArray, index, item, typeIterator, hasAttributes, suppress, complex, type, typeAttribute);
                    }
                    else
                    {
                        WriteEmptyType(itemArray, index, item, typeIterator, hasAttributes);
                    }
                }
            }
            else
            {
                typeIterator = inputDocumentManager.Select(item.Current, "child::node()[local-name(.) = 'Choice' or local-name(.) = 'Union' or local-name(.) = 'Values']");

                if (typeIterator.MoveNext())
                {
                    if (typeIterator.Current.LocalName == "Union")
                    {
                        /*
                        if (hasAttributes)
                        {
                            System.Console.WriteLine(itemName);
                        }
                        */
                        WriteUnionType(itemArray, index, item, typeIterator, hasAttributes, suppress);
                    }
                    else if (typeIterator.Current.LocalName == "Choice")
                    {
                        WriteChoiceType(itemArray, index, item, typeIterator, hasAttributes, suppress);
                    }
                    else if (typeIterator.Current.LocalName == "Values")
                    {
                        /*
                        if (hasAttributes)
                        {
                            System.Console.WriteLine(itemName);
                        }
                        */
                        WriteValuesType(itemArray, index, item, typeIterator, complexDerivation);
                    }
                }
            }

            if (complexDerivation != "" && !isUnion)
            {
                //close xs:element tag after writing the type information
                writer.WriteEndElement();
            }
        }



        /// <summary>
        /// Called by WriteType()
        /// Writes an xs:choice node of types NOT a choice of elements.
        /// A choice of elements is written by different code embedded in WriteSequence
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <param name="typeIterator"></param>
        /// <param name="hasAttributes"></param>
        /// <param name="suppress"></param>
        void WriteChoiceType(object[][] itemArray, int index, XPathNodeIterator item, XPathNodeIterator typeIterator, bool hasAttributes, bool suppress)
        {
            WriteAnnotation(item);
            if (!suppress) writer.WriteStartElement("xs:complexType");
            writer.WriteStartElement("xs:choice");

            typeIterator = inputDocumentManager.Select(typeIterator.Current, "xhtml:Type");

            ArrayList elements = new ArrayList();

            while (typeIterator.MoveNext())
            {
                string type = typeIterator.Current.GetAttribute("name", "");
                string infra = typeIterator.Current.GetAttribute("infra", "");

                string outputType = "";
                string outputName = "";
                string diagramOutputType = "";

                bool isRef = false;

                if (type.IndexOf("xs:") == 0)
                {
                    //outputType = type.Substring("xs:".Length);
                    outputType = type;
                    outputName = outputType;
                    diagramOutputType = outputType;
                }
                /*
            else if (inputDocumentManager.Select(typeIterator.Current, "*").MoveNext())
            {

            }
            */
                else if (type.IndexOf(":") == -1)
                {
                    outputType = type;
                    outputName = type;

                    isRef = IsRootSIFElement(type);

                    if (isRef)
                    {
                        ////Is this incomplete?
                        //isRef = isRef;
                    }

                    if (!isRef)
                    {
                        if (infra == "true")
                        {
                            outputName = outputName.Replace("Infrastructure", "");
                        }

                        if (outputType.LastIndexOf("Type") != outputType.Length - 4 || typeIterator.Current.GetAttribute("typed", "") == "false")
                        {
                            outputType += "Type";
                        }
                        else
                        {
                            //Is this incomplete?
                            //outputType = outputType;
                        }
                    }

                    diagramOutputType = outputType;
                    outputType = "sif:" + outputType;

                    if (infra == "true")
                    {
                        diagramOutputType = diagramOutputType.Replace("Infrastructure", "");
                    }
                }

                elements.Add(new Draw.Element(outputName, diagramOutputType));

                writer.WriteStartElement("xs:element");
                /*
                if (outputName == diagramOutputType && outputName.LastIndexOf("Type") == outputName.Length - 4)
                {
					
                }
                else*/
                {
                    if (!isRef)
                    {
                        writer.WriteAttributeString("name", outputName);
                    }
                    else
                    {
                        writer.WriteAttributeString("ref", outputType);
                    }
                }

                if (!isRef)
                {
                    writer.WriteAttributeString("type", outputType);
                }

             
                //WriteAnnotation(item);
                writer.WriteEndElement();
            }

            (currentNode as Draw.Element).ContentModel = Draw.ContentModel.CHOICE;
            (currentNode as Draw.Element).Children = (Draw.Element[])elements.ToArray(typeof(Draw.Element));

            writer.WriteEndElement();

            if (hasAttributes)
            {
                WriteAttributes(itemArray, index + 1);
            }

            if (!suppress) writer.WriteEndElement();
        }

        /// <summary>
        /// Writes an xs:union element.
        /// Called by WriteType()
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <param name="typeIterator"></param>
        /// <param name="hasAttributes"></param>
        /// <param name="suppress"></param>
        void WriteUnionType(object[][] itemArray, int index, XPathNodeIterator item, XPathNodeIterator typeIterator, bool hasAttributes, bool suppress)
        {
            string unionTypes = "";

            WriteAnnotation(item);

            if (!suppress && hasAttributes) writer.WriteStartElement("xs:complexType");
            if (!suppress && !hasAttributes) writer.WriteStartElement("xs:simpleType");

            if (hasAttributes)
            {
                writer.WriteStartElement("xs:simpleContent");
                writer.WriteStartElement("xs:extension");
                writer.WriteAttributeString("base", "xs:anyType");
                writer.WriteStartElement("xs:simpleType");
            }

            writer.WriteStartElement("xs:union");

            typeIterator = inputDocumentManager.Select(typeIterator.Current, "xhtml:Type");

            ArrayList elements = new ArrayList();

            while (typeIterator.MoveNext())
            {
                string type = typeIterator.Current.GetAttribute("name", "");

                string outputType = "";
                string outputName = "";
                string diagramOutputType = "";

                if (type.IndexOf("xs:") == 0)
                {
                    //outputType = type.Substring("xs:".Length);
                    outputType = type;
                    outputName = outputType;
                    diagramOutputType = outputType;
                }
                /*
            else if (inputDocumentManager.Select(typeIterator.Current, "*").MoveNext())
            {

            }
            */
                else if (type.IndexOf(":") == -1)
                {
                    outputType = type;
                    outputName = type;

                    if (outputType.LastIndexOf("Type") != outputType.Length - 4)
                    {
                        outputType += "Type";
                    }
                    else
                    {
                        //Is this incomplete?
                        //outputType = outputType;
                    }

                    diagramOutputType = outputType;
                    outputType = "sif:" + outputType;
                }

                //WriteAnnotation(item);
                writer.WriteStartElement("xs:simpleType");
                writer.WriteStartElement("xs:restriction");
                writer.WriteAttributeString("base", outputType);

                XPathNodeIterator valuesIterator = inputDocumentManager.Select(typeIterator.Current, "xhtml:Values");

                if (valuesIterator.MoveNext())
                {
                    WriteEnumeratedValues(valuesIterator);
                }

                writer.WriteEndElement();
                writer.WriteEndElement();

                if (unionTypes.Length > 0) unionTypes += " | ";
                unionTypes += diagramOutputType;
            }

            if (unionTypes.Length > 128)
            {
                unionTypes = unionTypes.Substring(0, unionTypes.IndexOf("|", 64)) + "| ...";
            }

            if (currentAttribute != null)
            {
                currentAttribute.Type = unionTypes;
            }
            else if (currentNode != null)
            {
                currentNode.Type = unionTypes;
            }

            writer.WriteEndElement();

            if (hasAttributes)
            {
                writer.WriteEndElement();
                WriteAttributes(itemArray, index + 1);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            if (!suppress) writer.WriteEndElement();
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="valuesIterator"></param>
        void WriteEnumeratedValues(XPathNodeIterator valuesIterator)
        {
            XPathNodeIterator codeIterator = inputDocumentManager.Select(valuesIterator.Current, "xhtml:Value//xhtml:Code");

            while (codeIterator.MoveNext())
            {
                string enumValue = codeIterator.Current.Value.Trim();

                writer.WriteStartElement("xs:enumeration");
                writer.WriteAttributeString("value", enumValue);

                if (generateXsdSettings.Annotate)
                {
                    XPathNodeIterator textIterator = inputDocumentManager.Select(codeIterator.Current, "../xhtml:Text");

                    if (textIterator.MoveNext())
                    {
                        writer.WriteStartElement("xs:annotation");
                        writer.WriteStartElement("xs:documentation");
                        writer.WriteString(textIterator.Current.Value);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <param name="typeIterator"></param>
        /// <param name="hasAttributes"></param>
        /// <param name="suppress"></param>
        /// <param name="complex"></param>
        /// <param name="type"></param>
        /// <param name="typeAttribute"></param>
        void WriteSIFType(object[][] itemArray, int index, XPathNodeIterator item, XPathNodeIterator typeIterator, bool hasAttributes, bool suppress, string complex, string type, string typeAttribute)
        {
            if (type.LastIndexOf("Type") == -1 || type.LastIndexOf("Type") != type.Length - 4 || typeIterator.Current.GetAttribute("typed", "") == "false")
            {
                type += "Type";
            }
            else
            {
                //Is this incomplete?
                //type = type;
            }

            if (!generateXsdSettings.IsDataModelXSD && !inInfrastructure)
            {
                if (type == "SIF_QueryType" || type == "SIF_ExtendedQueryType" || type == "SIF_ExtendedQueryResultsType" || type == "SIF_ErrorType" || type == "SIF_HeaderType")
                {
                    type = type.Substring(0, type.Length - 4) + "DataModelType";
                }
            }

            if (hasAttributes)
            {
                if (currentAttribute != null)
                {
                    currentAttribute.Type = type;
                }
                else if (currentNode != null)
                {
                    currentNode.Type = type;
                }

                if (!suppress) writer.WriteStartElement("xs:complexType");
               
                WriteAnnotation(item);

                bool isComplex = false;

                if (generateXsdSettings.AddNilAttributes && this.needNilAttribute && IsComplexType(type))
                {
                    isComplex = true;
                }

                bool kludge = false;

                if (complex != "restriction" && complex != "extension" && !isComplex)
                {
                    writer.WriteStartElement("xs:simpleContent");

                    if (!HasAttributes(type))
                    {
                        kludge = true;
                    }
                }
                else
                {
                    writer.WriteStartElement("xs:complexContent");
                }

                if (kludge && !mEmptyTypes.ContainsKey(type + "OrEmpty"))
                {
                    StringWriter stringWriter = new StringWriter();
                    XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);
                    xmlWriter.Formatting = Formatting.Indented;

                    xmlWriter.WriteStartElement("xs:simpleType");
                    xmlWriter.WriteAttributeString("name", type + "OrEmpty");
                    xmlWriter.WriteStartElement("xs:union");
                    xmlWriter.WriteStartElement("xs:simpleType");
                    xmlWriter.WriteStartElement("xs:restriction");
                    xmlWriter.WriteAttributeString("base", "sif:" + type);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteStartElement("xs:simpleType");
                    xmlWriter.WriteStartElement("xs:restriction");
                    xmlWriter.WriteAttributeString("base", "xs:string");
                    xmlWriter.WriteStartElement("xs:length");
                    xmlWriter.WriteAttributeString("value", "0");
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();

                    xmlWriter.Close();

                    string emptyDefinition = stringWriter.ToString();

                    mEmptyTypes.Add(type + "OrEmpty", emptyDefinition);
                }

                if (kludge && !generateXsdSettings.IsDataModelXSD && (inDataObject || (inCommonElement && !inInfrastructure)) && !generateXsdSettings.IsSIFMessage2XSD)
                {
                    type = type + "OrEmpty";
                }

                writer.WriteStartElement("xs:extension");
                writer.WriteAttributeString("base", "sif:" + type);
                WriteAttributes(itemArray, index + 1);
                writer.WriteEndElement();
                writer.WriteEndElement();

                if (!suppress) writer.WriteEndElement();
            }
            else
            {
                if (currentAttribute != null)
                {
                    currentAttribute.Type = type;
                }
                else if (currentNode != null)
                {
                    currentNode.Type = type;
                }

                writer.WriteAttributeString(typeAttribute, "sif:" + type);

               
                WriteAnnotation(item);

                string[] listKeys = GetListKey(type);

                if (listKeys != null)
                {
                    WriteListKeys(type.Substring(0, type.LastIndexOf("Type")), listKeys);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string[] GetListKey(string type)
        {
            XPathNodeIterator iterator = inputDocumentManager.Select(navigator, "//xhtml:CommonElement[@name='" + type + "']");
            XPathNodeIterator iteratorCopy = iterator.Clone();

            if (!iterator.MoveNext())
            {
                iterator = inputDocumentManager.Select(navigator, "//xhtml:DataObject[@name='" + type + "']");
                iteratorCopy = iterator.Clone();

                if (!iterator.MoveNext())
                {
                    if (type.LastIndexOf("Type") != -1)
                    {
                        type = type.Substring(0, type.LastIndexOf("Type"));

                        iterator = inputDocumentManager.Select(navigator, "//xhtml:CommonElement[@name='" + type + "']");
                        iteratorCopy = iterator.Clone();

                        if (!iterator.MoveNext())
                        {
                            iterator = inputDocumentManager.Select(navigator, "//xhtml:DataObject[@name='" + type + "']");
                            iteratorCopy = iterator.Clone();
                        }
                    }
                }
            }

            iterator = iteratorCopy;

            if (iterator.MoveNext())
            {
                

                XPathNodeIterator itemIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                if (itemIterator.MoveNext())
                {
                    if (actionListConstraints || generateXsdSettings.ListWithKeysConstraints)
                    {
                        string modePredicate = GetListModePredicate();

                        XPathNodeIterator listIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:List[" + modePredicate + "]");

                        if (listIterator.MoveNext())
                        {
                            return GetListKeys(listIterator);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool HasAttributes(string type)
        {
            XPathNodeIterator iterator = inputDocumentManager.Select(navigator, "//xhtml:CommonElement[@name='" + type + "']");
            XPathNodeIterator iteratorCopy = iterator.Clone();

            if (!iterator.MoveNext())
            {
                iterator = inputDocumentManager.Select(navigator, "//xhtml:DataObject[@name='" + type + "']");
                iteratorCopy = iterator.Clone();

                if (!iterator.MoveNext())
                {
                    if (type.LastIndexOf("Type") != -1)
                    {
                        type = type.Substring(0, type.LastIndexOf("Type"));

                        iterator = inputDocumentManager.Select(navigator, "//xhtml:CommonElement[@name='" + type + "']");
                        iteratorCopy = iterator.Clone();

                        if (!iterator.MoveNext())
                        {
                            iterator = inputDocumentManager.Select(navigator, "//xhtml:DataObject[@name='" + type + "']");
                            iteratorCopy = iterator.Clone();
                        }
                    }
                }
            }

            iterator = iteratorCopy;

            if (iterator.MoveNext())
            {
               

                XPathNodeIterator itemIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                while (itemIterator.MoveNext())
                {
                    XPathNodeIterator attributeIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:Attribute");

                    if (attributeIterator.MoveNext())
                    {
                        return true;
                    }

                    XPathNodeIterator elementIterator = inputDocumentManager.Select(itemIterator.Current, "xhtml:Element");

                    if (elementIterator.MoveNext())
                    {
                        if (elementIterator.Current.Value.Length > 0 && elementIterator.Current.Value[0] == '/')
                        {
                            return false;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool IsComplexType(string type)
        {
            XPathNodeIterator iterator = inputDocumentManager.Select(navigator, "//xhtml:CommonElement[@name='" + type + "']");
            XPathNodeIterator iteratorCopy = iterator.Clone();

            if (!iterator.MoveNext())
            {
                iterator = inputDocumentManager.Select(navigator, "//xhtml:DataObject[@name='" + type + "']");
                iteratorCopy = iterator.Clone();

                if (!iterator.MoveNext())
                {
                    if (type.LastIndexOf("Type") != -1)
                    {
                        type = type.Substring(0, type.LastIndexOf("Type"));

                        iterator = inputDocumentManager.Select(navigator, "//xhtml:CommonElement[@name='" + type + "']");
                        iteratorCopy = iterator.Clone();

                        if (!iterator.MoveNext())
                        {
                            iterator = inputDocumentManager.Select(navigator, "//xhtml:DataObject[@name='" + type + "']");
                            iteratorCopy = iterator.Clone();
                        }
                    }
                }
            }

            iterator = iteratorCopy;

            if (iterator.MoveNext())
            {
                int itemCount = 0;

                XPathNodeIterator itemIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']");

                while (itemIterator.MoveNext())
                {
                    ++itemCount;
                }

                if (itemCount > 1)
                {
                    return true;
                }

                if (itemCount == 1)
                {
                    XPathNodeIterator typeIterator = inputDocumentManager.Select(iterator.Current, "xhtml:Item[@intl=false() or @intl='" + documentGlobalSettings.Intl + "']/xhtml:Type");

                    if (typeIterator.MoveNext())
                    {
                        string name = typeIterator.Current.GetAttribute("name", "");

                        if (name.IndexOf(":") == -1)
                        {
                            return IsComplexType(name);
                        }
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <param name="typeIterator"></param>
        /// <param name="hasAttributes"></param>
        /// <param name="suppress"></param>
        /// <param name="complex"></param>
        /// <param name="type"></param>
        /// <param name="typeAttribute"></param>
        void WriteXSType(object[][] itemArray, int index, XPathNodeIterator item, XPathNodeIterator typeIterator, bool hasAttributes, bool suppress, string complex, string type, string typeAttribute)
        {
            if (hasAttributes && complex != "restriction" && complex != "extension")
            {
               
                WriteAnnotation(item);
                if (!suppress) writer.WriteStartElement("xs:complexType");
                writer.WriteStartElement("xs:simpleContent");

                XPathNodeIterator facetIterator = inputDocumentManager.Select(item.Current, "xhtml:Facets/*");

                if (!facetIterator.MoveNext())
                {
                    bool kludge = (type != "xs:hexBinary" && type != "xs:string" && type != "xs:normalizedString" && type != "xs:token");

                    string kludgeType = type.Replace("xs:", "XS") + "OrEmpty";

                    kludgeType = kludgeType.Substring(0, 2) + kludgeType.Substring(2, 1).ToUpper() + kludgeType.Substring(3);

                    if (kludge && !mEmptyTypes.ContainsKey(kludgeType))
                    {
                        StringWriter stringWriter = new StringWriter();
                        XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter);
                        xmlWriter.Formatting = Formatting.Indented;

                        xmlWriter.WriteStartElement("xs:simpleType");
                        xmlWriter.WriteAttributeString("name", kludgeType);
                        xmlWriter.WriteStartElement("xs:union");
                        xmlWriter.WriteStartElement("xs:simpleType");
                        xmlWriter.WriteStartElement("xs:restriction");
                        xmlWriter.WriteAttributeString("base", type);
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteStartElement("xs:simpleType");
                        xmlWriter.WriteStartElement("xs:restriction");
                        xmlWriter.WriteAttributeString("base", "xs:string");
                        xmlWriter.WriteStartElement("xs:length");
                        xmlWriter.WriteAttributeString("value", "0");
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();

                        xmlWriter.Close();

                        string emptyDefinition = stringWriter.ToString();

                        mEmptyTypes.Add(kludgeType, emptyDefinition);
                    }

                    if (kludge && !generateXsdSettings.IsDataModelXSD && (inDataObject || (inCommonElement && !inInfrastructure)) && !generateXsdSettings.IsSIFMessage2XSD)
                    {
                        type = "sif:" + kludgeType;
                    }

                    writer.WriteStartElement("xs:extension");
                    writer.WriteAttributeString("base", type/*type.Substring("xs:".Length)*/);

                    if (currentAttribute != null)
                    {
                        currentAttribute.Type = type;
                    }
                    else if (currentNode != null)
                    {
                        currentNode.Type = type;
                    }
                }
                else
                {
                    if (currentAttribute != null)
                    {
                        currentAttribute.Type = "\x2193" + type;
                    }
                    else if (currentNode != null)
                    {
                        currentNode.Type = "\x2193" + type;
                    }

                    writer.WriteStartElement("xs:restriction");
                    writer.WriteAttributeString("base", "xs:anyType");
                    writer.WriteStartElement("xs:simpleType");
                    writer.WriteStartElement("xs:restriction");
                    writer.WriteAttributeString("base", type/*type.Substring("xs:".Length)*/);

                    facetIterator = inputDocumentManager.Select(item.Current, "xhtml:Facets/*");

                    while (facetIterator.MoveNext())
                    {
                        writer.WriteNode(new Developmentor.Xml.NavigatorReader(facetIterator.Current), false);
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                WriteAttributes(itemArray, index + 1);

                writer.WriteEndElement();
                writer.WriteEndElement();
                if (!suppress) writer.WriteEndElement();
            }
            else if (hasAttributes)
            {
                if (currentAttribute != null)
                {
                    currentAttribute.Type = type;
                }
                else if (currentNode != null)
                {
                    currentNode.Type = type;
                }

              
                WriteAnnotation(item);
                if (!suppress) writer.WriteStartElement("xs:complexType");
                writer.WriteStartElement("xs:complexContent");

                writer.WriteStartElement("xs:extension");
                writer.WriteAttributeString("base", type/*type.Substring("xs:".Length)*/);
                WriteAttributes(itemArray, index + 1);
                writer.WriteEndElement();
                writer.WriteEndElement();
                if (!suppress) writer.WriteEndElement();
            }
            else
            {
                if (currentAttribute != null)
                {
                    currentAttribute.Type = type;
                }
                else if (currentNode != null)
                {
                    currentNode.Type = type;
                }

                XPathNodeIterator facetIterator = inputDocumentManager.Select(item.Current, "xhtml:Facets/*");

                if (!facetIterator.MoveNext())
                {
                    writer.WriteAttributeString(typeAttribute, type/*type.Substring("xs:".Length)*/);
                  
                    WriteAnnotation(item);
                }
                else
                {
                    if (currentAttribute != null)
                    {
                        currentAttribute.Type = "\x2193" + type;
                    }
                    else if (currentNode != null)
                    {
                        currentNode.Type = "\x2193" + type;
                    }

                    writer.WriteStartElement("xs:simpleType");
                    writer.WriteStartElement("xs:restriction");
                    writer.WriteAttributeString("base", type);

                  
                    WriteAnnotation(item);

                    facetIterator = inputDocumentManager.Select(item.Current, "xhtml:Facets/*");

                    while (facetIterator.MoveNext())
                    {
                        writer.WriteNode(new Developmentor.Xml.NavigatorReader(facetIterator.Current), false);
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemName"></param>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="suppress"></param>
        void WriteValuesAdditionalType(XPathNodeIterator item, string itemName, object[][] itemArray, int index, bool suppress)
        {
            bool isAttribute = (itemName[0] == '@');
            bool hasAttributes = !isAttribute && HasAttributes(itemArray, index + 1);

            XPathNodeIterator typeIterator = inputDocumentManager.Select(item.Current, "xhtml:Type");
            XPathNodeIterator valuesIterator = inputDocumentManager.Select(item.Current, "xhtml:Values");

            if (!typeIterator.MoveNext() || !valuesIterator.MoveNext()) return;

            string baseType = valuesIterator.Current.GetAttribute("base", "");
            string diagramBaseType = baseType;

            if (baseType == "")
            {
                baseType = "xs:token";
                diagramBaseType = baseType;
            }
            else if (baseType.IndexOf(":") == -1)
            {
                baseType = "sif:" + baseType;
            }

            XPathNodeIterator valueIterator = inputDocumentManager.Select(item.Current, "xhtml:Values/xhtml:Value/xhtml:Code");

            if (!suppress && hasAttributes) writer.WriteStartElement("xs:complexType");
            if (!suppress && !hasAttributes) writer.WriteStartElement("xs:simpleType");

            if (hasAttributes)
            {
                writer.WriteStartElement("xs:simpleContent");
                writer.WriteStartElement("xs:extension");
                writer.WriteAttributeString("base", "xs:anyType");
                writer.WriteStartElement("xs:simpleType");
            }

            writer.WriteStartElement("xs:union");

            string type = typeIterator.Current.GetAttribute("name", "").Trim();

            string outputType = "";
            string outputName = "";
            string diagramOutputType = "";

            if (type.IndexOf("xs:") == 0)
            {
                outputType = type;
                outputName = outputType;
                diagramOutputType = outputType;
            }
            else if (type.IndexOf(":") == -1)
            {
                outputType = type;
                outputName = type;

                if (outputType.LastIndexOf("Type") != outputType.Length - 4)
                {
                    outputType += "Type";
                }
                else
                {
                    //Is this incomplete?
                    //outputType = outputType;
                }

                diagramOutputType = outputType;
                outputType = "sif:" + outputType;
            }

           
            WriteAnnotation(item);
            writer.WriteStartElement("xs:simpleType");
            writer.WriteStartElement("xs:restriction");
            writer.WriteAttributeString("base", outputType);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("xs:simpleType");
            writer.WriteStartElement("xs:restriction");
            writer.WriteAttributeString("base", baseType);

            while (valueIterator.MoveNext())
            {
                string enumValue = valueIterator.Current.Value.Trim();

                writer.WriteStartElement("xs:enumeration");
                writer.WriteAttributeString("value", enumValue);

                if (generateXsdSettings.Annotate)
                {
                    XPathNodeIterator textIterator = inputDocumentManager.Select(valueIterator.Current, "../xhtml:Text");

                    if (textIterator.MoveNext())
                    {
                        writer.WriteStartElement("xs:annotation");
                        writer.WriteStartElement("xs:documentation");
                        writer.WriteString(textIterator.Current.Value);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();

            string unionTypes = diagramOutputType + " | " + "\x2193" + diagramBaseType;

            if (unionTypes.Length > 128)
            {
                unionTypes = unionTypes.Substring(0, unionTypes.IndexOf("|", 64)) + "| ...";
            }

            if (currentAttribute != null)
            {
                currentAttribute.Type = unionTypes;
            }
            else if (currentNode != null)
            {
                currentNode.Type = unionTypes;
            }

            writer.WriteEndElement();

            if (hasAttributes)
            {
                writer.WriteEndElement();
            }

            if (hasAttributes)
            {
                WriteAttributes(itemArray, index + 1);
            }

            if (hasAttributes)
            {
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            if (!suppress) writer.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="item"></param>
        /// <param name="valuesIterator"></param>
        /// <param name="complexDerivation"></param>
        void WriteValuesType(object[][] itemArray, int index, XPathNodeIterator item, XPathNodeIterator valuesIterator, string complexDerivation)
        {
            if (complexDerivation == "")
            {
               
                WriteAnnotation(item);
                writer.WriteStartElement("xs:simpleType");
                writer.WriteStartElement("xs:restriction");
            }

            string baseType = valuesIterator.Current.GetAttribute("base", "");
            string diagramBaseType = baseType;

            if (baseType == "")
            {
                baseType = "xs:token";
                diagramBaseType = baseType;
            }
            else if (baseType.IndexOf("xs:") == 0)
            {
                //baseType = baseType.Substring("xs:".Length);
            }
            else
            {
                baseType = "sif:" + baseType;
            }

            writer.WriteAttributeString("base", baseType);

            if (currentAttribute != null)
            {
                currentAttribute.Type = "\x2193" + diagramBaseType;
            }
            else if (currentNode != null)
            {
                currentNode.Type = "\x2193" + diagramBaseType;
            }

            if (complexDerivation != "")
            {
                WriteAnnotation(item);
            }

            WriteEnumeratedValues(valuesIterator);

            if (complexDerivation == "")
            {
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemName"></param>
        /// <param name="itemArray"></param>
        /// <param name="index"></param>
        /// <param name="suppress"></param>
        void WriteValuesSubsetType(XPathNodeIterator item, string itemName, object[][] itemArray, int index, bool suppress)
        {
            bool isAttribute = (itemName[0] == '@');
            bool hasAttributes = !isAttribute && HasAttributes(itemArray, index + 1);

            XPathNodeIterator typeIterator = inputDocumentManager.Select(item.Current, "xhtml:Type");
            XPathNodeIterator valuesIterator = inputDocumentManager.Select(item.Current, "xhtml:Values");

            if (!typeIterator.MoveNext() || !valuesIterator.MoveNext()) return;

            string baseType = typeIterator.Current.GetAttribute("name", "");
            string diagramBaseType = baseType;

            if (baseType == "")
            {
                baseType = "xs:token";
                diagramBaseType = baseType;
            }
            else if (baseType.IndexOf(":") == -1)
            {
                baseType = "sif:" + baseType;
            }

            XPathNodeIterator valueIterator = inputDocumentManager.Select(item.Current, "xhtml:Values/xhtml:Value/xhtml:Code");

            WriteAnnotation(item);
            writer.WriteStartElement("xs:simpleType");
            writer.WriteStartElement("xs:restriction");

            writer.WriteAttributeString("base", baseType);

            if (currentAttribute != null)
            {
                currentAttribute.Type = "\x2193" + diagramBaseType;
            }
            else if (currentNode != null)
            {
                currentNode.Type = "\x2193" + diagramBaseType;
            }

            while (valueIterator.MoveNext())
            {
                string enumValue = valueIterator.Current.Value.Trim();

                writer.WriteStartElement("xs:enumeration");
                writer.WriteAttributeString("value", enumValue);

                if (generateXsdSettings.Annotate)
                {
                    XPathNodeIterator textIterator = inputDocumentManager.Select(valueIterator.Current, "../xhtml:Text");

                    if (textIterator.MoveNext())
                    {
                        writer.WriteStartElement("xs:annotation");
                        writer.WriteStartElement("xs:documentation");
                        writer.WriteString(valueIterator.Current.Value);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemArray"></param>
        /// <returns></returns>
        bool IsEmpty(object[][] itemArray)
        {
            XPathNodeIterator iterator = (XPathNodeIterator)itemArray[0][1];

            if (iterator != null && inputDocumentManager.Select(iterator.Current, "xhtml:Type[@name='EMPTY']").MoveNext())
            {
                return true;
            }

            return false;
        }

        bool HasAttributes(object[][] itemArray)
        {
            for (int i = 0; i < itemArray.Length; ++i)
            {
                string name = (string)itemArray[i][0];

                if (name[0] == '@')
                {
                    return true;
                }
            }

            return false;
        }

        bool HasAttributes(object[][] itemArray, int index)
        {
            if (index < itemArray.Length)
            {
                string name = (string)itemArray[index][0];

                if (name[0] == '@')
                {
                    return true;
                }
            }

            return false;
        }
    }
}
