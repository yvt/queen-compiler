using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language
{
    public class IntermediateCompilerException: Exception
    {
        public IntermediateCompilerException(string msg) :
            base(msg)
        {
        }
    }
}
