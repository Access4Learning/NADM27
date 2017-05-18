using System;
using System.Collections;
using System.Collections.Generic;

namespace GenerateSpec.Tools
{
    /// <summary>
    /// This CodeComparer class implements the IComparer interface. An instance of the class is used to sort ArrayList of codes in codeset. 
    /// This method is used only once in the project. This probably code that is set and forget unless a major overhaul of code is needed.
    /// </summary>
    class CodeComparer : IComparer
    {
        /// <summary>
        /// The source code that does the work of implementing the interface
        /// </summary>
        public int Compare(object lhs, object rhs)
        {
            int lv;
            int rv;

            if (Int32.TryParse((string)lhs, out lv) && Int32.TryParse((string)rhs, out rv))
            {
                if (lv < rv) return -1;
                if (lv > rv) return 1;
                return 0;
            }
            //if none of the above applies the use the default
            return Comparer.Default.Compare(lhs, rhs);
        }
    }

	
	
	
	/// <summary>
    /// This IndexNumberComparer class implements the IComparer interface.  
    /// This method is used to create a "multiIndex". A multiIndex is for example a list of elements and all the places the element occurs in SIF data objects.
    /// This probably code that is set and forget unless a major overhaul of code is needed.
    /// </summary>
    class IndexNumberComparer : IComparer<string>
    {
        /// <summary>
        /// 
        /// </summary>
        CodeComparer codeComparer = new CodeComparer();
        char[] separators = new char[] { '.', '-' };

        public int Compare(string lhs, string rhs)
        {
            string[] lparts = lhs.Split(separators);
            string[] rparts = rhs.Split(separators);

            int i = 0;
            int j = 0;

            while (i < lparts.Length && j < rparts.Length)
            {
                int partResult = codeComparer.Compare(lparts[i], rparts[j]);

                if (partResult != 0)
                {
                    return partResult;
                }

                ++i;
                ++j;
            }

            if (i < lparts.Length) return 1;
            if (j < rparts.Length) return -1;
            return 0;
        }
    }
	
	
}

