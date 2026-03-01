using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Parsing;

/// <summary>
/// A piece of syntax that can be run, but not evaluated.
/// </summary>
public abstract class Statement
{
    public abstract TResult Accept<TResult>(IStatementVisitor<TResult> visitor);
}

public class ProgramStatement : Statement
{
    public required List<Statement> Statements { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitProgram(this);
    }
}

public class ExpressionStatement : Statement
{
    public required Expression Expression { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitExpressionStatement(this);
    }
}

public class BlockStatement : Statement
{
    public required List<Statement> Statements { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitBlock(this);
    }
}

public class VariableDeclarationStatement : Statement
{
    public required TypeInfo Type { get; init; }
    public required Token Identifier { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitVariableDeclaration(this);
    }
}

public class VariableDeclarationWithAssignmentStatement : VariableDeclarationStatement
{
    public required Expression Value { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitVariableDeclarationWithAssignment(this);
    }
}

public class IfStatement : Statement
{
    public required Expression Condition { get; init; }
    public required BlockStatement IfBody { get; init; }
    public List<IfStatement> ElseIfStatements { get; init; } = [];
    public BlockStatement? ElseBody { get; init; } = null;

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitIf(this);
    }
}

public class WhileStatement : Statement
{
    public required Expression Condition { get; init; }
    public required BlockStatement Body { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitWhile(this);
    }
}

public class FunctionDeclarationStatement : Statement
{
    public required TypeInfo ReturnType { get; init; }
    public required Token Identifier { get; init; }
    public required List<VariableDeclarationStatement> Parameters { get; init; }
    public required BlockStatement Body { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitFunctionDeclaration(this);
    }
}

public class ReturnEmptyStatement : Statement
{
    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitReturnEmpty(this);
    }
}

public class ReturnExpressionStatement : Statement
{
    public required Expression Value;

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitReturnExpression(this);
    }
}

public class StructDeclarationStatement : Statement
{
    public required Token Identifier { get; init; }
    public required List<VariableDeclarationStatement> Fields { get; init; }

    public override TResult Accept<TResult>(IStatementVisitor<TResult> visitor)
    {
        return visitor.VisitStructDeclaration(this);
    }
}