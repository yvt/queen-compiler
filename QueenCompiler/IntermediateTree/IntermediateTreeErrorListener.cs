using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language
{
    public interface IntermediateTreeErrorListener
    {
        void IntermediateCompilationErrorReported(string message,
            CodeDom.CodeLocation location);
    }
}
