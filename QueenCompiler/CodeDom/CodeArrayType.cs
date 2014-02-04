using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeArrayType : CodeType
    {
        public CodeType ElementType { get; set; }
        public int Dimensions { get; set; }

        public CodeArrayType()
        {
            Dimensions = 1;
        }
    }
}
