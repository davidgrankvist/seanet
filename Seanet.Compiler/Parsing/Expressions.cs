using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Parsing;

/// <summary>
/// A piece of syntax that can be evaluated.
/// </summary>
public abstract class Expression
{
}

public class LiteralExpression : Expression
{
    public required Token Literal { get; init; }
}

public class GroupExpression : Expression
{
    public required Expression Expression { get; init; }
}

public class PrefixUnaryExpression : Expression
{
    public required Token Operator { get; init; }
    public required Expression Expression { get; init; }
}

public class BinaryExpression : Expression
{
    public required Token Operator { get; init; }
    public required Expression First { get; init; }
    public required Expression Second { get; init; }
}

public class AssignmentExpression : Expression
{
    public required Token Identifier { get; init; }
    public required Expression Expression { get; init; }
}

public class LogicalExpression : Expression
{
    public required Token Operator { get; init; }
    public required Expression First { get; init; }
    public required Expression Second { get; init; }
}

public class CallExpression : Expression
{
    public required Expression Callee { get; init; }
    public required List<Expression> Arguments { get; init; }
}

public class PropertyAccessExpression : Expression
{
    public required Expression Object { get; init; }
    public required Token Property { get; init; }
}

public class PropertyAssignmentExpression : Expression
{
    public required Expression Object { get; init; }
    public required Token Property { get; init; }
    public required Expression Value { get; init; }
}

public class VariableExpression : Expression
{
    public required Token Identifier { get; init; }
}

public class PostfixIncrementExpression : Expression
{
    public required Token Operator { get; init; }
    public required Token Identifier { get; init; }
}

public class ArrayAccessExpression : Expression
{
    public required Expression Array { get; init; }
    public required Expression Index { get; init; }
}

public class NewSizedArrayExpression : Expression
{
    public required ArrayTypeInfo Type { get; init; }
    public required List<Expression> Sizes { get; init; }
}

public class NewStructExpression : Expression
{
    public required SingleTokenTypeInfo Type { get; init; }
}