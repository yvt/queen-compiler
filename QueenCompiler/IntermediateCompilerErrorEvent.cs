using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language
{
    public delegate void IntermediateCompileErrorEventHandler(object sender, IntermediateCompileErrorEventArgs args);

    public class IntermediateCompileErrorEventArgs : EventArgs
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
        public CodeDom.CodeSourceFile SourceFile { get; set; }

        public IntermediateCompileErrorEventArgs()
        {
        }
        public IntermediateCompileErrorEventArgs(CodeDom.CodeSourceFile src, int line, int column, string message)
        {
            SourceFile = src;
            Line = line;
            Column = column;
            Message = message;
        }
    }
}
