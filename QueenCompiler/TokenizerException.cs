using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language
{
    class TokenizerException: Exception
    {
        public TokenizerException(string msg):
            base(msg)
        {
        }
    }
}
