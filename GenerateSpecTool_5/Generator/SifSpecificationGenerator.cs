using System;
using System.IO;
using GenerateSpec.Tools;
using GenerateSpec.Generator.Util;


namespace GenerateSpec.Generator
{
    /// <summary>
    /// This class is called by the main class. It sets up and executes methods on GenerateHtml, GenerateXsd, and InputDocumentManager. 
    /// </summary>
    public class SifSpecificationGenerator
    {
        private sifSpecificationGeneratorConfig generatorConfig;

        /// <summary>
        /// 
        /// </summary>
        public sifSpecificationGeneratorConfig GeneratorConfig
        {
            get { return generatorConfig; }
        }

        private DocumentGlobalSettings globalSettings;

        private InputDocumentManager inputDocumentManager;

        /// <summary>
        /// The constructor for the class SifSpecificationGenerator
        /// </summary>
        /// <param name="DocumentPath"></param>
        public SifSpecificationGenerator(string DocumentPath)
        {
            //The following statements take a config/properties file that is in xml (called SIF.Config.xml) and reads
            //it into properties on the generatorConfig object. The various properties are used throughout
            string fileData = File.ReadAllText(DocumentPath);
            //this line takes the config file and deserializes it into an object.
            generatorConfig = (sifSpecificationGeneratorConfig)SerializationTools.Deserialize(fileData, typeof(sifSpecificationGeneratorConfig));
            fileData = "";

            // initialize the file paths for input and output
            globalSettings = new DocumentGlobalSettings();

            FileInfo documentPathInfo = new FileInfo(DocumentPath);

            globalSettings.OutputPath = Path.Combine(Path.GetDirectoryName(documentPathInfo.FullName), "out");

            documentPathInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(documentPathInfo.FullName), generatorConfig.globalSettings.inputDocument));

            if (documentPathInfo.Exists == false)
            {
                throw new Exception("Input document not found in path " + documentPathInfo.FullName);
            }

            globalSettings.InputPath = documentPathInfo.FullName;
            globalSettings.OutputDiagramPath = Path.Combine(globalSettings.OutputPath , @"diagrams\");
            globalSettings.SifNamespace = generatorConfig.globalSettings.sifNamespace;
            globalSettings.Intl = generatorConfig.globalSettings.localizationCode;
            globalSettings.SchemaVersion = generatorConfig.globalSettings.schemaVersion; 

            inputDocumentManager = new InputDocumentManager(globalSettings);

        }

        /// <summary>
        /// Performs XSD generation, HTML generation, and validation of xml examples. 
        /// </summary>
        public void Generate()
        {
            inputDocumentManager.BuildInput();

            //
            // for each xsd document process the entry
            //
            foreach (sifSpecificationGeneratorConfigXsdDocument xsdDocument in generatorConfig.xsdDocuments)
            {
                
                ProcessXsdDocument(xsdDocument);
            }

            //
            // for each html document process the entry
            //
            foreach (sifSpecificationGeneratorConfigHtmlDocument htmlDocument in generatorConfig.htmlDocuments)
            {
               ProcessHtmlDocument(htmlDocument); 
            }

            //
            // Validate Examples
            //
             //Why are html examples checked here?  ValidateExamples is defined, overridden, and called in GenerateHtml.cs
            GenerateHtml generatorHtml = new GenerateHtml(inputDocumentManager.DocumentGlobalSettings, inputDocumentManager);
            generatorHtml.OutputPath = globalSettings.OutputPath;
            generatorHtml.ValidateExamples();
        }


        /// <summary>
        /// Generate HTML documents
        /// </summary>
        /// <param name="htmlDocument">This input parameter is an object that contains html generation configuration information. Despite its name, it is not an html document.</param>
        private void ProcessHtmlDocument(sifSpecificationGeneratorConfigHtmlDocument htmlDocument)
        {
            GenerateHtml generatorHtml = new GenerateHtml(inputDocumentManager.DocumentGlobalSettings, inputDocumentManager);

            
            generatorHtml.RootTitle = htmlDocument.rootTitle;
            //generatorHtml.NextSectionHack = htmlDocument.nextSectionHack;
            generatorHtml.OutputPath = globalSettings.OutputPath;
            generatorHtml.SingleDocument = htmlDocument.singleDocument;
            generatorHtml.RootFileName = htmlDocument.rootDocumentFileName;
            generatorHtml.DocumentType = htmlDocument.documentType.ToString(); 

            generatorHtml.GenerateSpec();
        }

        /// <summary>
        /// Sets some propery values from the input object then calls the xsd generator
        /// </summary>
        /// <param name="xsdDocument"></param>
        private void ProcessXsdDocument(sifSpecificationGeneratorConfigXsdDocument xsdDocument)
        {
            XsdSettings xsdSettings = new XsdSettings();

            //xsdSettings.IsServiceBodyDefinition = xsdDocument.isServiceBodyXsd; 
            xsdSettings.Annotate = xsdDocument.annotate;
            xsdSettings.Diagram = xsdDocument.diagram;
            xsdSettings.IsDataModelXSD = xsdDocument.isDataModelXsd;
            xsdSettings.ListWithKeysConstraints = xsdDocument.listWithKeyConstraints;
            xsdSettings.IsSIFMessage2XSD = xsdDocument.isSifMessage2Xsd;
            xsdSettings.SingleSchema = xsdDocument.singleSchema;
            xsdSettings.OutputXSDPath = Path.Combine(globalSettings.OutputPath, @"XSD\" + xsdDocument.xsdTitle + @"\");

            //Generate the XSD documents using input parameters created above
            GenerateXsd generateXsd = new GenerateXsd(inputDocumentManager.DocumentGlobalSettings, xsdSettings, inputDocumentManager);
            generateXsd.Generate();
        }
    }
}
