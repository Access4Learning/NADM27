using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateSpec.Generator
{
    /// <summary>
    /// 
    /// </summary>
    class CodeComparer : IComparer
    {
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

            return Comparer.Default.Compare(lhs, rhs);
        }
    }

}
