namespace Seanet.Compiler.Parsing;

public interface IExpressionVisitor<TResult>
{
    TResult VisitPropertyAssignment(PropertyAssignmentExpression propertyAssignmentExpression);
    TResult VisitVariable(VariableExpression variableExpression);
    TResult VisitLiteral(LiteralExpression literalExpression);
    TResult VisitGroup(GroupExpression groupExpression);
    TResult VisitPrefixUnary(PrefixUnaryExpression prefixUnaryExpression);
    TResult VisitBinary(BinaryExpression binaryExpression);
    TResult VisitAssignment(AssignmentExpression assignmentExpression);
    TResult VisitLogical(LogicalExpression logicalExpression);
    TResult VisitCall(CallExpression callExpression);
    TResult VisitPropertyAccess(PropertyAccessExpression propertyAccessExpression);
    TResult VisitPostfixIncrement(PostfixIncrementExpression postfixIncrementExpression);
    TResult VisitArrayAccess(ArrayAccessExpression arrayAccessExpression);
    TResult VisitNewSizedArray(NewSizedArrayExpression newSizedArrayExpression);
    TResult VisitNewStruct(NewStructExpression newStructExpression);
}
