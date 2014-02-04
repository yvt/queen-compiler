using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CInternalExcpt: CExcpt
    {
        public CInternalExcpt() :
            base(9999, Queen.Properties.Resources.InternalException)
        { 
            
        }
    }
}
