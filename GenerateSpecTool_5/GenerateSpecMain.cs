using GenerateSpec.Generator;

namespace GenerateSpec
{
    /// <summary>
    /// The root class of the project.  
    /// </summary>
    class GenerateSpecMain
    {
        /// <summary>
        /// As per convention, this method launches the application  
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // for testing  only -------------------------------
            //args = new string[1];
            //args[0] = @".\Specification\SIF.Config_DataModel.xml";

            /*
             * Parameter 0: path to the specificationGenerator.xml file
             */
            string specificationGeneratorDocumentPath = args[0];

            GenerateSpecification(specificationGeneratorDocumentPath);

        }

        /// <summary>
        /// Launches the specification documents generation.
        /// </summary>
        /// <param name="specificationGeneratorDocumentPath">The path to the specification source files. These
        /// source files are to the docucments, not software.</param>
        private static void GenerateSpecification(string specificationGeneratorDocumentPath)
        {
            SifSpecificationGenerator theGenerator = new SifSpecificationGenerator(specificationGeneratorDocumentPath);


            theGenerator.Generate();
        }
    }
}
