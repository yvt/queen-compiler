using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public struct CodeLocation
    {
        public CodeSourceFile SourceFile { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}:{2})", SourceFile, Line, Column);
        }
    }
}
