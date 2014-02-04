using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    class CIndexOutOfBoundExcpt: CExcpt
    {
        public CIndexOutOfBoundExcpt() :
            base(3002, Queen.Properties.Resources.ExceptionIndexOutOfBound)
        { 
            
        }
    }
}
