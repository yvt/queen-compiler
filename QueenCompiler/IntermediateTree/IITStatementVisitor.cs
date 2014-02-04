using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public interface IITStatementVisitor<T>
    {
        T Visit(ITBlockStatement statement);
        T Visit(ITExitBlockStatement statement);
        T Visit(ITExpressionStatement statement);
        T Visit(ITIfStatement statement);
        T Visit(ITReturnStatement statement);
        T Visit(ITTableSwitchStatement statement);
        T Visit(ITTryStatement statement);
        T Visit(ITAssertStatement statement);
        T Visit(ITThrowNumericStatement statement);
        T Visit(ITThrowObjectStatement statement);
    }
}
