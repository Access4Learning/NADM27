using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateSpec.Generator
{
    /// <summary>
    /// 
    /// </summary>
    class IndexNumberComparer : IComparer<string>
    {
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
