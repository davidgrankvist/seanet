using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Parsing;

/// <summary>
/// A piece of syntax that can be run, but not evaluated.
/// </summary>
public abstract class Statement
{
}

public class ProgramStatement : Statement
{
    public required List<Statement> Statements { get; init; }
}

public class ExpressionStatement : Statement
{
    public required Expression Expression { get; init; }
}

public class BlockStatement : Statement
{
    public required List<Statement> Statements { get; init; }
}

public class DeclarationStatement : Statement
{
    public required Token Type { get; init; }
    public required Token Identifier { get; init; }
    public required Expression Value { get; init; }
}

public class IfStatement : Statement
{
    public required Expression Condition { get; init; }
    public required BlockStatement Body { get; init; }
}

public class WhileStatement : Statement
{
    public required Expression Condition { get; init; }
    public required BlockStatement Body { get; init; }
}

public class FunctionDeclarationStatement : Statement
{
    public required Token ReturnType { get; init; }
    public required Token Identifier { get; init; }
    public required List<DeclarationStatement> Parameters { get; init; }
    public required BlockStatement Body { get; init; }
}