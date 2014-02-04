using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CNotImplementedExcpt: CExcpt
    {
        public CNotImplementedExcpt() :
            base(3009, Queen.Properties.Resources.ExceptionNotImplemented)
        { 
            
        }
    }
}
