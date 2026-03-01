using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Parsing;

/// <summary>
/// A piece of syntax that can be evaluated.
/// </summary>
public abstract class Expression
{
    public abstract TResult Accept<TResult>(IExpressionVisitor<TResult> visitor);

    /// <summary>
    /// Declaration associated with this expression, if applicable.
    /// This is attached by the resolver.
    /// </summary>
    public Statement? Declaration { get; set; }
}

public class LiteralExpression : Expression
{
    public required Token Literal { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitLiteral(this);
    }
}

public class GroupExpression : Expression
{
    public required Expression Expression { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitGroup(this);
    }
}

public class PrefixUnaryExpression : Expression
{
    public required Token Operator { get; init; }
    public required Expression Expression { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitPrefixUnary(this);
    }
}

public class BinaryExpression : Expression
{
    public required Token Operator { get; init; }
    public required Expression First { get; init; }
    public required Expression Second { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitBinary(this);
    }
}

public class AssignmentExpression : Expression
{
    public required Expression Variable { get; init; }
    public required Expression Value { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitAssignment(this);
    }
}

public class LogicalExpression : Expression
{
    public required Token Operator { get; init; }
    public required Expression First { get; init; }
    public required Expression Second { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitLogical(this);
    }
}

public class CallExpression : Expression
{
    public required Expression Callee { get; init; }
    public required List<Expression> Arguments { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitCall(this);
    }
}

public class PropertyAccessExpression : Expression
{
    public required Expression Object { get; init; }
    public required Token Property { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitPropertyAccess(this);
    }
}

public class PropertyAssignmentExpression : Expression
{
    public required Expression Object { get; init; }
    public required Token Property { get; init; }
    public required Expression Value { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitPropertyAssignment(this);
    }
}

public class VariableExpression : Expression
{
    public required Token Identifier { get; init; }

    /// <summary>
    /// Indicates that the variable was passed by reference.
    /// </summary>
    public required bool IsReference { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitVariable(this);
    }
}

public class PostfixIncrementExpression : Expression
{
    public required Token Operator { get; init; }
    public required Expression Variable { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitPostfixIncrement(this);
    }
}

public class ArrayAccessExpression : Expression
{
    public required Expression Array { get; init; }
    public required Expression Index { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitArrayAccess(this);
    }
}

public class NewSizedArrayExpression : Expression
{
    public required ArrayTypeInfo Type { get; init; }
    public required List<Expression> Sizes { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitNewSizedArray(this);
    }
}

public class NewStructExpression : Expression
{
    public required SingleTokenTypeInfo Type { get; init; }

    public override TResult Accept<TResult>(IExpressionVisitor<TResult> visitor)
    {
        return visitor.VisitNewStruct(this);
    }
}