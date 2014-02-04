using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CNullRefExcpt: CExcpt
    {
        public CNullRefExcpt() :
            base(0xc0000005, Queen.Properties.Resources.ExceptionNullReference)
        { 
            
        }
    }
}
