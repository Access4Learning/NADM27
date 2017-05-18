using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateSpec.Generator
{
    /// <summary>
    /// 
    /// </summary>
    public class DocumentGlobalSettings
    {
        string outputPath;

        /// <summary>
        /// 
        /// </summary>
        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }

        string outputDiagramPath;

        /// <summary>
        /// 
        /// </summary>
        public string OutputDiagramPath
        {
            get { return outputDiagramPath; }
            set { outputDiagramPath = value; }
        }

        string sifNamespace;

        /// <summary>
        /// 
        /// </summary>
        public string SifNamespace
        {
            get { return sifNamespace; }
            set { sifNamespace = value; }
        }

        string intl;

        /// <summary>
        /// 
        /// </summary>
        public string Intl
        {
            get { return intl; }
            set { intl = value; }
        }

        string inputPath;

        /// <summary>
        /// 
        /// </summary>
        public string InputPath
        {
            get { return inputPath; }
            set { inputPath = value; }
        }

    }
}
