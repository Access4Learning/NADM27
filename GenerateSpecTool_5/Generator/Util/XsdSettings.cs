
namespace GenerateSpec.Generator.Util
{
    /// <summary>
    /// A properties-only object used to store settings for XSD generation.
    /// </summary>
    public class XsdSettings
    {
       
        string outputXSDPath;

        /// <summary>
        /// 
        /// </summary>
        public string OutputXSDPath
        {
            get { return outputXSDPath; }
            set { outputXSDPath = value; }
        }

        bool annotate = false;

        /// <summary>
        /// 
        /// </summary>
        public bool Annotate
        {
            get { return annotate; }
            set { annotate = value; }
        }

        bool isDataModelXSD = false;

        /// <summary>
        /// 
        /// </summary>
        public bool IsDataModelXSD
        {
            get { return isDataModelXSD; }
            set { isDataModelXSD = value; }
        }


        bool singleSchema = false;

        /// <summary>
        /// 
        /// </summary>
        public bool SingleSchema
        {
            get { return singleSchema; }
            set { singleSchema = value; }
        }

        bool isSIFMessage2XSD = false;

        /// <summary>
        /// 
        /// </summary>
        public bool IsSIFMessage2XSD
        {
            get { return isSIFMessage2XSD; }
            set { isSIFMessage2XSD = value; }
        }

        bool addNilAttributes = false;

        /// <summary>
        /// 
        /// </summary>
        public bool AddNilAttributes
        {
            get { return addNilAttributes; }
            set { addNilAttributes = value; }
        }

        bool diagram = false;

        /// <summary>
        /// 
        /// </summary>
        public bool Diagram
        {
            get { return diagram; }
            set { diagram = value; }
        }

        bool listWithKeysConstraints = false;

        /// <summary>
        /// 
        /// </summary>
        public bool ListWithKeysConstraints
        {
            get { return listWithKeysConstraints; }
            set { listWithKeysConstraints = value; }
        }


        bool isServiceBodyDefinition = false;

        /// <summary>
        /// 
        /// </summary>
        public bool IsServiceBodyDefinition
        {
            get { return isServiceBodyDefinition; }
            set { isServiceBodyDefinition = value; }
        }



    }
}
