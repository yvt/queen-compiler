using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CStackOverflowExcpt: CExcpt
    {
        public CStackOverflowExcpt() :
            base(0xc00000fd, Queen.Properties.Resources.ExceptionStackOverflow)
        { 
            
        }
    }
}
