using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace Queen.Language.CliCompiler
{
    public partial class CliCompiler
    {
        private TypeBuilder privateDataType = null;
        

        internal void EnsurePrivateDataType()
        {
            if (privateDataType != null)
                return;

            string name = "<PrivateImplementationDetails>";
            name += "{" + System.Guid.NewGuid().ToString() + "}";
            privateDataType = module.DefineType(name, TypeAttributes.Sealed | TypeAttributes.NotPublic);
        }

        private void CompletePrivateDataType()
        {
            if (privateDataType != null)
            {
                privateDataType.CreateType();
            }
        }

        private int initializedFieldId = 0;

        internal FieldInfo DefineInitializedField(byte[] bytes)
        {
            EnsurePrivateDataType();
            initializedFieldId += 1;
            return privateDataType.DefineInitializedData("field-" + initializedFieldId.ToString(), bytes,
                FieldAttributes.Public | FieldAttributes.Static);
        }
    }
}
