using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeFunctionStatement : CodeStatement
    {
        public CodeIdentifier Name { get; set; }
        public CodeType ReturnType { get; set; }
        public CodeBlock Statements { get; set; }
        public IList<CodeParameterDeclaration> Parameters { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsOverride { get; set; }
        public IList<CodeIdentifier> GenericParameters { get; set; }

        public CodeFunctionStatement()
        {
            Parameters = new List<CodeParameterDeclaration>();
            GenericParameters = new List<CodeIdentifier>();
        }

        public CodeClassStatement ObjectType
        {
            get
            {
                CodeObject obj = Parent;
                if (obj == null)
                    return null;
                obj = obj.Parent;
                if (obj == null)
                    return null;
                if (obj is CodeClassStatement)
                {
                    return (CodeClassStatement)obj;
                }
                else
                {
                    return null;
                }
            }
        }
        public bool IsMemberFunction
        {
            get
            {
                CodeObject obj = Parent;
                if (obj == null)
                    return false;
                obj = obj.Parent;
                if (obj == null)
                    return false;
                return obj is CodeClassStatement;
            }
        }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
