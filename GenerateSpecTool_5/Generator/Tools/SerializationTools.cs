using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace GenerateSpec.Tools
{
    /// <summary>
    /// Serialize and deserialize XML objects. 
    /// </summary>
    class SerializationTools
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Serialize(object item)
        {
            string res = "";
            StringBuilder sb = new StringBuilder("");
            using (StringWriter swriter = new StringWriter(sb))
            {
                XmlSerializer ser = null;
                using (TextWriter tw = swriter)
                {
                    using (XmlWriter xwriter = new XmlTextWriter(tw))
                    {
                        ser = new XmlSerializer(item.GetType());
                        ser.Serialize(xwriter, item);
                        res = swriter.ToString();

                        ser = null;
                        swriter.Close();
                        tw.Close();
                        xwriter.Close();
                        sb = null;
                    }
                }
            }

            return (res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlstring"></param>
        /// <param name="outputType"></param>
        /// <returns></returns>
        public static object Deserialize(string xmlstring, Type outputType)
        {
            XmlSerializer deserializer;
            StringReader sr;
            object message;
            using (sr = new StringReader(xmlstring))
            {
                deserializer = new XmlSerializer(outputType);
                message = deserializer.Deserialize(sr);
            }
            
            return message;
        }
    }
}
