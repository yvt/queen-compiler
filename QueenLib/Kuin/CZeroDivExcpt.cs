using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CZeroDivExcpt: CExcpt
    {
        public CZeroDivExcpt() :
            base(0xc0000094L, Queen.Properties.Resources.ExceptionZeroDivision)
        { 
            
        }
    }
}
