using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CInvalidOperationExcpt: CExcpt
    {
        public CInvalidOperationExcpt() :
            base(3007, Queen.Properties.Resources.ExceptionInvalidOperation)
        { 
            
        }
    }
}
