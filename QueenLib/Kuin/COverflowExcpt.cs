using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class COverflowExcpt: CExcpt
    {
        public COverflowExcpt() :
            base(3003, Queen.Properties.Resources.ExceptionOverflow)
        { 
            
        }
    }
}
