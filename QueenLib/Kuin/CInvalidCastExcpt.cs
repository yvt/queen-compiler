using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CInvalidCastExcpt: CExcpt
    {
        public CInvalidCastExcpt() :
            base(3004, Queen.Properties.Resources.ExceptionInvalidCast)
        { 
            
        }
    }
}
