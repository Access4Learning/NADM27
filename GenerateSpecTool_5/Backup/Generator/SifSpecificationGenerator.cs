using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GenerateSpec.Tools;


namespace GenerateSpec.Generator 
{
    /// <summary>
    /// 
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

            inputDocumentManager = new InputDocumentManager(globalSettings);

        }

        /// <summary>
        /// 
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
               ProcessHtmlDocument(htmlDocument); //REMOVED FOR TESTING ONLY
            }

            //
            // Validate Examples
            //
             //Why are html examples checked here?  So it will only be done once?
            GenerateHtml generatorHtml = new GenerateHtml(inputDocumentManager.DocumentGlobalSettings, inputDocumentManager);
            generatorHtml.OutputPath = globalSettings.OutputPath;
            generatorHtml.ValidateExamples();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlDocument"></param>
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
            GenerateXsdSettings xsdSettings = new GenerateXsdSettings();

            xsdSettings.IsServiceBodyDefinition = xsdDocument.isServiceBodyXsd; 
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
