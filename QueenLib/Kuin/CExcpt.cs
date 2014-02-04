using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    public class CExcpt : Exception
    {
        long code;
        string msg;
        long pos;
        string convertedStackTrace;

        public long Code
        {
            get { return code; }
            set { code = value; }
        }

        public string Msg
        {
            get { return msg; }
            set { msg = value; }
        }

        public long Pos
        {
            get
            {
                return pos;
            }
        }

        public bool IsError()
        {
            return Code >= 1000;
        }

        public static CExcpt TryConvertFromFrameworkException(Exception ex)
        {
            if (ex == null)
                return null;

            CExcpt ret = ex as CExcpt;

            // TODO: zero division exception?
            if (ret == null)
            {
                if (ex is DivideByZeroException)
                    ret = new CZeroDivExcpt();
                else if (ex is ArithmeticException)
                    ret = new COverflowExcpt();
                else if (ex is IndexOutOfRangeException)
                    ret = new CIndexOutOfBoundExcpt();
                else if (ex is InvalidCastException)
                    ret = new CInvalidCastExcpt();
                else if (ex is InvalidOperationException)
                    ret = new CInvalidOperationExcpt();
                else if (ex is NullReferenceException)
                    ret = new CNullRefExcpt();
                else if (ex is StackOverflowException)
                    ret = new CStackOverflowExcpt();
                else if (ex is NotImplementedException)
                    ret = new CNotImplementedExcpt();
            }

            if (ret != null)
            {
                ret.SetConvertedStackTrace(ex.StackTrace);
                if(ret != ex)
                    ret.SetBaseException(ex);
            }

            return ret;
        }

        public CExcpt()
        {

        }

        public CExcpt(long code, string msg)
        {
            this.code = code;
            this.msg = msg;
            
            // TODO: what is Kuin@CExcpt#pos?
            this.pos = 0;
        }

        public CExcpt Init(long code, string msg)
        {
            this.code = code;
            this.msg = msg;
            return this;
        }

        public new string StackTrace
        {
            get
            {
                if (convertedStackTrace == null)
                    return base.StackTrace;
                return convertedStackTrace;
            }
        }

        public override string Message
        {
            get
            {
                return msg;
            }
        }

        public override string ToString()
        {
            return code.ToString() + " : " + (msg ?? "null") + "\r\n" + StackTrace;
        }

        private Exception baseException;

        public override Exception GetBaseException()
        {
            return baseException;
        }

        private void SetConvertedStackTrace(string val)
        {
            convertedStackTrace = val;
        }

        private void SetBaseException(Exception ex)
        {
            baseException = ex;
        }
    }
}
