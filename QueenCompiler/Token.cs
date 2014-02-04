using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language
{
    enum TokenType
    {
        EndOfFile,
        EndOfLine,
        Identifier,
        TextLiteral,
        NumericLiteral,
        Operator,
        Comment
    };

    enum SymbolTokenType
    {
        Add, Subtract, Divide, Multiply, Modulus,
        Power,
        And, Or,
        LessThan, LessThanOrEqual,
        GreaterThan, GreaterThanOrEqual,
        Equality,
        Inequality,

        AdditionAssign, 
        SubtractAssign, 
        DivisionAssign,
        MultiplicationAssign,
        ModulusAssign,
        Swap,
        ConcatAssign,
        PowerAssign,

        /// <summary>
        /// : symbol that is used for the type specification.
        /// </summary>
        TypeSpecifier,

        /// <summary>
        /// @ symbol that is used to specify a global scope.
        /// </summary>
        GlobalScope,

        /// <summary>
        /// # symbol that is used to specify a sub-scope.
        /// </summary>
        Scope,

        /// <summary>
        /// ` symbol that is used to specify a generic type parameter.
        /// </summary>
        GenericTypeParameter,

        Comma,
        MemberAccess,
        ConditionalTernary,

        Assign,

        /// <summary>
        /// =# binary operator.
        /// </summary>
        TypeEquality,
        /// <summary>
        /// <># binary operator.
        /// </summary>
        TypeInequality,


        /// <summary>
        /// =& binary operator.
        /// </summary>
        ReferenceEquality,
        /// <summary>
        /// <>& binary operator.
        /// </summary>
        ReferenceInequality,

        /// <summary>
        /// $ binary operator.
        /// </summary>
        Cast,

        /// <summary>
        /// ~ binary operator.
        /// </summary>
        Concat,

        /// <summary>
        /// ! unary operator.
        /// </summary>
        Not,

        /// <summary>
        /// ## unary operator.
        /// </summary>
        Copy,

        ParentheseOpen,
        ParentheseClose,

        SquareBracketOpen,
        SquareBracketClose

    }

    struct TokenLocation
    {
        public int line, column;
    }

    abstract class Token
    {
        public TokenLocation Location { get; set; }
        public abstract bool Equals(string s);
        public virtual string Description
        {
            get
            {
                return this.ToString();
            }
        }
    }



    sealed class InvalidToken : Token
    {
        public string Text { get; set; }
        public override bool Equals(string s)
        {
            return s.Equals(Text);
        }
        public override string ToString()
        {
            return Text;
        }
    }

    sealed class EndOfLineToken : Token
    {
        public override bool Equals(string s)
        {
            return s.Equals("\n");
        }
        public override string ToString()
        {
            return "\n";
        }
        public override string Description
        {
            get
            {
                return Properties.Resources.TokenEndOfLine;
            }
        }
    }

    sealed class EndOfFileToken : Token
    {
        public override bool Equals(string s)
        {
            return s.Length == 0;
        }
        public override string ToString()
        {
            return "";
        }
        public override string Description
        {
            get
            {
                return Properties.Resources.TokenEndOfFile;
            }
        }
    }

    sealed class IdentifierToken : Token
    {
        public string Text { get; set; }
        public override bool Equals(string s)
        {
            return s == Text;
        }
        public override string ToString()
        {
            return Text;
        }
    }

    abstract class PrimitiveValueToken : Token
    {
        public string Text { get; set; }
        public abstract object Value { get; }
    }

    sealed class IntegerToken : PrimitiveValueToken
    {
        public long LongValue { get; set; }
        public override bool Equals(string s)
        {
            return s == Text;
        }
        public override string ToString()
        {
            return Text;
        }
        public override object Value
        {
            get
            {
                return LongValue;
            }
        }
    }

    sealed class FloatNumberToken : PrimitiveValueToken
    {
        public float FloatValue
        {
            get;
            set;
        }
        public override bool Equals(string s)
        {
            return s == Text;
        }
        public override string ToString()
        {
            return Text;
        }
        public override object Value
        {
            get
            {
                return FloatValue;
            }
        }
    }


    sealed class DoubleNumberToken : PrimitiveValueToken
    {
        public double DoubleValue
        {
            get; set; 
        }
        public override bool Equals(string s)
        {
            return s == Text;
        }
        public override string ToString()
        {
            return Text;
        }
        public override object Value
        {
            get
            {
                return DoubleValue;
            }
        }
    }


    sealed class StringToken : PrimitiveValueToken
    {
        public string StringValue
        {
            get;
            set;
        }
        public override bool Equals(string s)
        {
            return s == Text;
        }
        public override string ToString()
        {
            return Text;
        }
        public override object Value
        {
            get
            {
                return StringValue;
            }
        }
    }

    sealed class CharToken : PrimitiveValueToken
    {
        public char CharValue
        {
            get;
            set;
        }
        public override bool Equals(string s)
        {
            return s == Text;
        }
        public override string ToString()
        {
            return Text;
        }
        public override object Value
        {
            get
            {
                return CharValue;
            }
        }
    }

    sealed class SymbolToken : Token
    {
        private SymbolTokenType type;
        private string text;
        public SymbolTokenType SymbolType {
            get
            {
                return type;
            }
            set
            {
                switch (value)
                {
                    case SymbolTokenType.Add:
                        text = "+";
                        break;
                    case SymbolTokenType.Subtract:
                        text = "-";
                        break;
                    case SymbolTokenType.Multiply:
                        text = "*";
                        break;
                    case SymbolTokenType.Divide:
                        text = "/";
                        break;
                    case SymbolTokenType.Power:
                        text = "^";
                        break;
                    case SymbolTokenType.Concat:
                        text = "~";
                        break;
                    case SymbolTokenType.AdditionAssign:
                        text = ":+";
                        break;
                    case SymbolTokenType.SubtractAssign:
                        text = ":-";
                        break;
                    case SymbolTokenType.MultiplicationAssign:
                        text = ":*";
                        break;
                    case SymbolTokenType.DivisionAssign:
                        text = ":/";
                        break;
                    case SymbolTokenType.PowerAssign:
                        text = ":^";
                        break;
                    case SymbolTokenType.ConcatAssign:
                        text = ":~";
                        break;
                    case SymbolTokenType.And:
                        text = "&";
                        break;
                    case SymbolTokenType.Or:
                        text = "|";
                        break;
                    case SymbolTokenType.Modulus:
                        text = "%";
                        break;
                    case SymbolTokenType.ModulusAssign:
                        text = ":%";
                        break;
                    case SymbolTokenType.Cast:
                        text = "$";
                        break;
                    case SymbolTokenType.Comma:
                        text = ",";
                        break;
                    case SymbolTokenType.ConditionalTernary:
                        text = "?";
                        break;
                    case SymbolTokenType.Copy:
                        text = "##";
                        break;
                    case SymbolTokenType.Equality:
                        text = "=";
                        break;
                    case SymbolTokenType.Assign:
                        text = "::";
                        break;
                    case SymbolTokenType.Inequality:
                        text = "<>";
                        break;
                    case SymbolTokenType.TypeEquality:
                        text = "=#";
                        break;
                    case SymbolTokenType.TypeInequality:
                        text = "<>#";
                        break;
                    case SymbolTokenType.ReferenceEquality:
                        text = "=&";
                        break;
                    case SymbolTokenType.ReferenceInequality:
                        text = "<>&";
                        break;
                    case SymbolTokenType.GlobalScope:
                        text = "@";
                        break;
                    case SymbolTokenType.GreaterThan:
                        text = ">";
                        break;
                    case SymbolTokenType.LessThan:
                        text = "<";
                        break;
                    case SymbolTokenType.GreaterThanOrEqual:
                        text = ">=";
                        break;
                    case SymbolTokenType.LessThanOrEqual:
                        text = "<=";
                        break;
                    case SymbolTokenType.TypeSpecifier:
                        text = ":";
                        break;
                    case SymbolTokenType.Swap:
                        text = ":$";
                        break;
                    case SymbolTokenType.SquareBracketOpen:
                        text = "[";
                        break;
                    case SymbolTokenType.SquareBracketClose:
                        text = "]";
                        break;
                    case SymbolTokenType.ParentheseOpen:
                        text = "(";
                        break;
                    case SymbolTokenType.ParentheseClose:
                        text = ")";
                        break;
                    case SymbolTokenType.Scope:
                        text = "#";
                        break;
                    case SymbolTokenType.MemberAccess:
                        text = ".";
                        break;
                    case SymbolTokenType.Not:
                        text = "!";
                        break;
                    case SymbolTokenType.GenericTypeParameter:
                        text = "`";
                        break;
                    default:
                        throw new ArgumentException("Invalid symbol type.", "value");
                }
                type = value;
            }
        }
        public string Text
        {
            get
            {
                return text;
            }
        }
        public override bool Equals(string s)
        {
            return s.Equals(text);
        }
        public override string ToString()
        {
            return text;
        }
    }

    class CommentToken : Token
    {
        public string Text { get; set; }
        public override string ToString()
        {
            return Text;
        }
        public override bool Equals(string s)
        {
            return false;
        }
    }
}
