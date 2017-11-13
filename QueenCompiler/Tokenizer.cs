using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Queen.Language.CodeDom;

namespace Queen.Language
{
    

    sealed class Tokenizer
    {
        int bufferPos = 0;
        int bufferLen = 0;
        char[] buffer = new char[65536];
        char unreadChar = '\0';
        TextReader reader;
        Token currentToken;
        TokenLocation location;
        int lastColumn = 1;
        char newLineChar = '\0';

        List<Token> prevTokens = new List<Token>();
        Token lastToken;

        public event TokenizerErrorEventHandler ErrorReported;


        public Tokenizer(TextReader reader)
        {
            this.reader = reader;
            location.column = 1;
            location.line = 1;
        }

        private bool IsIdentifierChar(char c)
        {
            // FIXME: support non-ascii char?
            // for C#: http://msdn.microsoft.com/ja-jp/library/aa664670(v=vs.71).aspx 
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c == '_') ||
                (c >= '0' && c <= '9');
        }

        private bool IsSymbolChar(char c)
        {
            switch (c)
            {
                case '!':
                case '+':
                case '-':
                case '*':
                case '/':
                case ':':
                case '#':
                case '@':
                case '(':
                case ')':
                case '>':
                case '<':
                case '=':
                case '&':
                case '%':
                case '^':
                case '[':
                case ']':
                case '.':
                case ',':
                case '~':
                case '$':
                case '?':
                case '|':
                case '`':
                    return true;
                default:
                    return false;
            }
        }

        private void ReportError(string message, TokenLocation loc)
        {
            ErrorReported(this, new TokenizerErrorEventArgs(
                loc.line, loc.column, message));
        }

        private TokenLocation GetLastLocation()
        {
            TokenLocation l = location;
            if (l.column == 1)
            {
                l.column = lastColumn;
                l.line--;
            }
            else
            {
                l.column--;
            }
            return l;
        }

        private void Unread(char c)
        {
            if (unreadChar != '\0')
            {
                throw new TokenizerException(Properties.Resources.InternalError);
            }
            unreadChar = c;
            if (c == '\n')
            {
                location.column = lastColumn;
                location.line--;
            }
            else
            {
                location.column--;
                if (location.column < 1)
                {
                    throw new TokenizerException(Properties.Resources.InternalError);
                }
            }
        }

        private char Read()
        {
            char c;
            if (unreadChar != '\0')
            {
                c = unreadChar;
                unreadChar = '\0';
                if (c == '\n')
                {
                    lastColumn = location.column;
                    location.line++;
                    location.column = 1;
                }
                else
                {
                    location.column++;
                }
                return c;
            }
            if (bufferPos >= bufferLen)
            {
                // fill buffer
                bufferLen = reader.Read(buffer, 0, buffer.Length);
                if (bufferLen == 0)
                {
                    return '\0';
                }
                bufferPos = 0;
            }
            c = buffer[bufferPos++];
            if (c == '\r' || c == '\n')
            {
                if (newLineChar == '\0')
                    newLineChar = c;
                if (c == newLineChar)
                {
                    lastColumn = location.column;
                    location.line++;
                    location.column = 1;
                    return '\n';
                }
                return Read();
            }
            location.column++;
            return c;
        }

        public Token Current
        {
            get {
                if (prevTokens.Count > 0)
                    return prevTokens[prevTokens.Count - 1];
                return currentToken;
            }
        }

        private void DoSymbol(char c)
        {
            TokenLocation savedLocation = GetLastLocation();
            SymbolToken token = new SymbolToken();
            currentToken = token;
            currentToken.Location = savedLocation;


            switch (c)
            {
                case '+':
                    token.SymbolType = SymbolTokenType.Add;
                    return;
                case '-':
                    token.SymbolType = SymbolTokenType.Subtract;
                    return;
                case '*':
                    token.SymbolType = SymbolTokenType.Multiply;
                    return;
                case '/':
                    token.SymbolType = SymbolTokenType.Divide;
                    return;
                case '^':
                    token.SymbolType = SymbolTokenType.Power;
                    return;
                case '%':
                    token.SymbolType = SymbolTokenType.Modulus;
                    return;
                case '(':
                    token.SymbolType = SymbolTokenType.ParentheseOpen;
                    return;
                case ')':
                    token.SymbolType = SymbolTokenType.ParentheseClose;
                    return;
                case '[':
                    token.SymbolType = SymbolTokenType.SquareBracketOpen;
                    return;
                case ']':
                    token.SymbolType = SymbolTokenType.SquareBracketClose;
                    return;
                case '@':
                    token.SymbolType = SymbolTokenType.GlobalScope;
                    return;
                case '&':
                    token.SymbolType = SymbolTokenType.And;
                    return;
                case '|':
                    token.SymbolType = SymbolTokenType.Or;
                    return;
                case '#':
                    c = Read();
                    switch (c)
                    {
                        case '#':
                            token.SymbolType = SymbolTokenType.Copy;
                            return;
                        default:
                            Unread(c);
                            token.SymbolType = SymbolTokenType.Scope;
                            return;
                    }
                case ',':
                    token.SymbolType = SymbolTokenType.Comma;
                    return;
                case '?':
                    token.SymbolType = SymbolTokenType.ConditionalTernary;
                    return;
                case '.':
                    token.SymbolType = SymbolTokenType.MemberAccess;
                    return;
                case '!':
                    token.SymbolType = SymbolTokenType.Not;
                    break;
                case '~':
                    token.SymbolType = SymbolTokenType.Concat;
                    return;
                case '$':
                    token.SymbolType = SymbolTokenType.Cast;
                    return;
                case '`':
                    token.SymbolType = SymbolTokenType.GenericTypeParameter;
                    return;
                case '=':
                    c = Read();
                    switch (c)
                    {
                        case '&':
                            token.SymbolType = SymbolTokenType.ReferenceEquality;
                            return;
                        case '#':
                            token.SymbolType = SymbolTokenType.TypeEquality;
                            return;
                        default:
                            Unread(c);
                            token.SymbolType = SymbolTokenType.Equality;
                            return;
                    }
                case '<':
                    c = Read();
                    switch (c)
                    {
                        case '>':
                            c = Read();
                            switch (c)
                            {
                                case '&':
                                    token.SymbolType = SymbolTokenType.ReferenceInequality;
                                    return;
                                case '#':
                                    token.SymbolType = SymbolTokenType.TypeInequality;
                                    return;
                                default:
                                    Unread(c);
                                    token.SymbolType = SymbolTokenType.Inequality;
                                    return;
                            }
                        case '=':
                            token.SymbolType = SymbolTokenType.LessThanOrEqual;
                            return;
                        default:
                            Unread(c);
                            token.SymbolType = SymbolTokenType.LessThan;
                            return;
                    }
                case '>':
                    c = Read();
                    switch (c)
                    {
                        case '=':
                            token.SymbolType = SymbolTokenType.GreaterThanOrEqual;
                            return;
                        default:
                            Unread(c);
                            token.SymbolType = SymbolTokenType.GreaterThan;
                            return;
                    }
                case ':':
                    c = Read();
                    switch (c)
                    {
                        case '+':
                            token.SymbolType = SymbolTokenType.AdditionAssign;
                            return;
                        case '-':
                            token.SymbolType = SymbolTokenType.SubtractAssign;
                            return;
                        case '*':
                            token.SymbolType = SymbolTokenType.MultiplicationAssign;
                            return;
                        case '/':
                            token.SymbolType = SymbolTokenType.DivisionAssign;
                            return;
                        case '%':
                            token.SymbolType = SymbolTokenType.ModulusAssign;
                            return;
                        case '^':
                            token.SymbolType = SymbolTokenType.PowerAssign;
                            return;
                        case '$':
                            token.SymbolType = SymbolTokenType.Swap;
                            return;
                        case ':':
                            token.SymbolType = SymbolTokenType.Assign;
                            return;
                        case '~':
                            token.SymbolType = SymbolTokenType.ConcatAssign;
                            return;
                        default:
                            Unread(c);
                            token.SymbolType = SymbolTokenType.TypeSpecifier;
                            return;
                    } // ':' + second char
                default:
                    throw new TokenizerException(Properties.Resources.InternalError);
            } // first char
        }

        private bool TryParseInteger(string str, int radix, out long outValue)
        {
            outValue = 0;
            try
            {
                long value = 0;
                for (int p = 0, len = str.Length; p < len; p++)
                {
                    value = checked(value * radix);

                    long digit = 0;
                    char c = str[p];
                    if (c >= '0' && c <= '9')
                    {
                        digit = c - '0';
                    }
                    else if (c >= 'a' && c <= 'z')
                    {
                        digit = c - 'a' + 10;
                    }
                    else
                    {
                        throw new TokenizerException(Properties.Resources.InternalError);
                    }
                    if (digit >= radix)
                    {
                        return false;
                    }

                    value = checked(value + digit);
                }
                outValue = value;
                return true;
            }
            catch (ArithmeticException)
            {
                return false;
            }
        }

        private bool TryParseReal(string str, int radix, out double outValue)
        {
            int fractPos = str.IndexOf('.');
            outValue = 0.0;
            
            if (fractPos < 0)
                fractPos = str.Length;

            double value = 0.0;
            double factor;
            double scale = (double)radix;

            factor = 1.0;
            for (int i = fractPos - 1; i >= 0; i--)
            {
                int digit = 0;
                char c = str[i];
                if (c >= '0' && c <= '9')
                {
                    digit = c - '0';
                }
                else if (c >= 'a' && c <= 'z')
                {
                    digit = c - 'a' + 10;
                }
                else
                {
                    throw new TokenizerException(Properties.Resources.InternalError);
                }
                if (digit >= radix)
                {
                    return false;
                }
                else if (digit > 0)
                {
                    if (double.IsInfinity(factor))
                    {
                        return false;
                    }
                    value += factor * (double)digit;
                }

                factor *= scale;
            }

            scale = 1.0 / scale;
            factor = scale;
            for (int i = fractPos + 1; i < str.Length; i++)
            {
                int digit = 0;
                char c = str[i];
                if (c >= '0' && c <= '9')
                {
                    digit = c - '0';
                }
                else if (c >= 'a' && c <= 'z')
                {
                    digit = c - 'a';
                }
                else
                {
                    throw new TokenizerException(Properties.Resources.InternalError);
                }
                if (digit >= radix)
                {
                    return false;
                }
                else if (digit > 0)
                {
                    value += factor * (double)digit;
                }

                factor *= scale;
            }

            outValue = value;
            return true;
        }

        // reads suffix and decides final type
        private void FinalizeNumber(double outValue, string str, TokenLocation savedLocation)
        {
            char c = Read();
            if (c == 'F')
            {
                // double
                str += "F";
                DoubleNumberToken token = new DoubleNumberToken();
                token.Location = savedLocation;
                token.Text = str;
                token.DoubleValue = outValue;
                currentToken = token;
            }
            else
            {
                Unread(c);
                FloatNumberToken token = new FloatNumberToken();
                token.Location = savedLocation;
                token.Text = str;
                token.FloatValue = (float)outValue;
                currentToken = token;
            }
        }

        private void DoNumber(char c)
        {
            StringBuilder str = new StringBuilder();
            TokenLocation savedLocation = GetLastLocation();

            do {
                str.Append(c);
                c = Read();
            } while(c >= '0' && c <= '9');

            if (c == '.')
            {
                // decimal real number
                do
                {
                    str.Append(c);
                    c = Read();
                } while (c >= '0' && c <= '9');
                Unread(c);

                // real number
                double outValue;
                if (!TryParseReal(str.ToString(), 10, out outValue))
                {
                    InvalidToken token = new InvalidToken();
                    token.Location = savedLocation;
                    token.Text = str.ToString();
                    currentToken = token;

                    ReportError(Properties.Resources.TokenizerInvalidNumericLiteral, savedLocation);
                }
                else
                {
                    FinalizeNumber(outValue, str.ToString(), savedLocation);
                }
            }
            else if (c == '#')
            {
                // number with radix
                int radix = int.Parse(str.ToString());
                if (radix < 2 || radix > 36)
                {
                    // invalid radix
                    InvalidToken token = new InvalidToken();
                    token.Location = savedLocation;
                    token.Text = radix.ToString() + "#";
                    currentToken = token;

                    ReportError(Properties.Resources.TokenizerNoDigits, savedLocation);
                    return;
                }

                str = new StringBuilder();
                c = Read();
                while ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z'))
                {
                    str.Append(c);
                    c = Read();
                }

                if (c == '.')
                {
                    // real number with radix
                    do
                    {
                        str.Append(c);
                        c = Read();
                    } while (c >= '0' && c <= '9');
                    Unread(c);

                    // real number
                    double outValue;
                    if (!TryParseReal(str.ToString(), radix, out outValue))
                    {
                        InvalidToken token = new InvalidToken();
                        token.Location = savedLocation;
                        token.Text = str.ToString();
                        currentToken = token;

                        ReportError(Properties.Resources.TokenizerInvalidNumericLiteral, savedLocation);
                    }
                    else
                    {
                        FinalizeNumber(outValue, str.ToString(), savedLocation);
                    }
                }
                else
                {
                    Unread(c);
                    string s = str.ToString();
                    if (s.Length == 0)
                    {
                        // empty number
                        InvalidToken token = new InvalidToken();
                        token.Location = savedLocation;
                        token.Text = radix.ToString() + "#";
                        currentToken = token;

                        ReportError(Properties.Resources.TokenizerNoDigits, savedLocation);
                    }
                    else
                    {
                        // integer with radix
                        long outValue;
                        if (!TryParseInteger(str.ToString(), radix, out outValue))
                        {
                            InvalidToken token = new InvalidToken();
                            token.Location = savedLocation;
                            token.Text = str.ToString();
                            currentToken = token;

                            ReportError(Properties.Resources.TokenizerInvalidNumericLiteral, savedLocation);
                        }
                        else
                        {
                            IntegerToken token = new IntegerToken();
                            token.Location = savedLocation;
                            token.Text = str.ToString();
                            token.LongValue = outValue;
                            currentToken = token;
                        }
                    }
                }

                
            }
            else
            {
                // integer
                Unread(c);

                long outValue;
                if (!TryParseInteger(str.ToString(), 10, out outValue))
                {
                    InvalidToken token = new InvalidToken();
                    token.Location = savedLocation;
                    token.Text = str.ToString();
                    currentToken = token;

                    ReportError(Properties.Resources.TokenizerInvalidNumericLiteral, savedLocation);
                }
                else
                {
                    IntegerToken token = new IntegerToken();
                    token.Location = savedLocation;
                    token.Text = str.ToString();
                    token.LongValue = outValue;
                    currentToken = token;
                }
            }
        }

        private void DoString(char c)
        {
            if (c != '"')
            {
                throw new TokenizerException(Properties.Resources.InternalError);
            }
            StringToken token = new StringToken();
            token.Location = GetLastLocation();

            StringBuilder str = new StringBuilder();
            StringBuilder str2 = new StringBuilder();
            str2.Append('"');
            while (true)
            {
                c = Read();
                if (c == '\0')
                {
                    ReportError(Properties.Resources.ParserClassUnexpectedEOF, GetLastLocation());
                    break;
                }
                str2.Append(c);
                if (c == '"')
                {
                    break;
                }
                if (c == '\\')
                {
                    // escape
                    c = Read();
                    str2.Append(c);
                    switch (c)
                    {
                        case '\\':
                            str.Append('\\'); continue;
                        case '"':
                            str.Append('"'); continue;
                        case '\'':
                            str.Append('\''); continue;
                        case 'n':
                            str.Append('\n'); continue;
                        case 't':
                            str.Append('\t'); continue;
                        case 'w':
                            str.Append('\u3000'); continue;
                        default:
                            str.Append('\\');
                            str.Append(c);
                            TokenLocation loc = GetLastLocation();
                            loc.column--;
                            ReportError(string.Format(Properties.Resources.TokenizerInvalidEscapeSequence, "\\" + c),
                               loc);
                            continue;
                    }
                }
                str.Append(c);
            }

            token.Text = str2.ToString();
            token.StringValue = str.ToString();
            currentToken = token;
        }
        private void DoChar(char c)
        {
            if (c != '\'')
            {
                throw new TokenizerException(Properties.Resources.InternalError);
            }
            CharToken token = new CharToken();
            token.Location = GetLastLocation();
             
            bool hasChar = false;
            bool invalid = false;
            StringBuilder str2 = new StringBuilder();
            str2.Append('\'');
            while (true)
            {
                c = Read();
                str2.Append(c);
                if (c == '\'')
                {
                    if (!hasChar)
                        invalid = true;
                    break;
                }
                if (c == '\\')
                {
                    // escape
                    c = Read();
                    str2.Append(c);
                    if (hasChar)
                    {
                        invalid = true;
                    }
                    switch (c)
                    {
                        case '\\':
                            c = '\\'; break;
                        case '"':
                            c = '"'; break;
                        case '\'':
                            c = '\''; break;
                        case 'n':
                            c = '\n'; break;
                        case 't':
                            c = '\t'; break;
                        case 'w':
                            c = '\u3000'; break;
                        default:
                            TokenLocation loc = GetLastLocation();
                            loc.column--;
                            ReportError(string.Format(Properties.Resources.TokenizerInvalidEscapeSequence, "\\" + c),
                               loc);
                            invalid = true;
                            continue;
                    }
                }
                if (hasChar)
                {
                    invalid = true;
                }
                token.CharValue = c;
                hasChar = true;
            }

            token.Text = str2.ToString();
            currentToken = token;

            if (invalid)
            {
                InvalidToken tk = new InvalidToken();
                tk.Location = token.Location;
                tk.Text = str2.ToString();
                currentToken = tk;
                ReportError(Properties.Resources.TokenizerInvalidCharToken,
                   tk.Location);
            }
        }

        private void DoIdentifier(char c)
        {
            IdentifierToken token = new IdentifierToken();
            token.Location = GetLastLocation(); ;

            StringBuilder str = new StringBuilder();
            do
            {
                str.Append(c);
                c = Read();
            } while (IsIdentifierChar(c));
            Unread(c);

            token.Text = str.ToString();
            currentToken = token;
        }

        public void UnMove(Token token)
        {
            prevTokens.Add(token);
            lastToken = null;
        }

        public void MovePrevious()
        {
            if (lastToken == null)
            {
                // cannot go further by itself
                throw new TokenizerException(Properties.Resources.InternalError);
            }

            prevTokens.Add(lastToken);
            lastToken = null;
        }

        public void MoveNext() {

            if (prevTokens.Count > 0)
            {
                prevTokens.RemoveAt(prevTokens.Count - 1);
                return;
            }

            TokenLocation savedLocation = location;

            lastToken = currentToken;
            currentToken = null;
            char c;
            do
            {
                c = Read();
            } while (c != '\0' && c != '\n' && char.IsWhiteSpace(c));



            if (c == '\0')
            {
                // EOF
                currentToken = new EndOfFileToken();
                currentToken.Location = location;
                return;
            }
            else if (c == '\n')
            {
                // EOL
                currentToken = new EndOfLineToken();
                currentToken.Location = location;
                return;
            }

            if (c == '{')
            {
                // Block comment
                currentToken = new CommentToken();
                currentToken.Location = location;

                // find terminator
                int level = 1;
                StringBuilder sb = new StringBuilder();
                sb.Append(c);
                do{
                    c = Read();
                    sb.Append(c);
                    if (c == '{')
                    {
                        level++;
                    }
                    else if (c == '}')
                    {
                        level--;
                        if (level == 0)
                            break;
                    }
                }while(c > 0);

                ((CommentToken)currentToken).Text = sb.ToString();

                if (c == '}')
                {
                    // found one
                    return;
                }

                InvalidToken errToken = new InvalidToken();
                errToken.Text = ((CommentToken)currentToken).Text;
                errToken.Location = currentToken.Location;

                ReportError(Properties.Resources.TokenizerNoCommentTerminator, errToken.Location);
                return;
            }

            if (c == ';')
            {
                // Line comment
                currentToken = new CommentToken();
                currentToken.Location = location;

                // find terminator
                StringBuilder sb = new StringBuilder();
                sb.Append(c);
                do
                {
                    c = Read();
                    if (c == '\n')
                    {
                        break;
                    }
                    sb.Append(c);
                } while (c > 0);

                ((CommentToken)currentToken).Text = sb.ToString();

                return;
            }

            // TODO: use correct "location" for below methods

            // check operator
            if (IsSymbolChar(c))
            {
                DoSymbol(c);
                return;
            }

            // check number
            if (c >= '0' && c <= '9')
            {
                DoNumber(c);
                return;
            }

            // check strings
            if (c == '"')
            {
                DoString(c);
                return;
            }else if(c == '\''){
                DoChar(c);
                return;
            }

            // check identifier
            if (IsIdentifierChar(c))
            {
                DoIdentifier(c);
                return;
            }

            // invalid character.
            InvalidToken token = new InvalidToken();
            token.Location = savedLocation;
            token.Text = new string(new char[] { c });
            currentToken = token;

            ReportError(Properties.Resources.TokenizerInvalidCharacter, savedLocation);
        }
    }

}
