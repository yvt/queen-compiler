using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CAssertFailExcpt: CExcpt
    {
        public CAssertFailExcpt():
            base(3000, Queen.Properties.Resources.ExceptionAssertionFailure)
        { 
            
        }
    }
}
