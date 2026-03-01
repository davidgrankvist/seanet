using Seanet.Compiler.Errors;
using Seanet.Compiler.Parsing;
using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Analysis;

public class Resolver : IStatementVisitor<object?>, IExpressionVisitor<object?>
{
    private readonly ErrorReporter errorReporter;
    private string file = string.Empty;
    private ScopeManager scopeManager = new();

    public Resolver(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
    }

    public void ResolveVariables(string file, Statement syntaxTree)
    {
        this.file = file;
        scopeManager = new();
        VisitStatement(syntaxTree);
    }

    private object? VisitStatement(Statement statement)
    {
        return statement.Accept(this);
    }

    private object? VisitExpression(Expression expression)
    {
        return expression.Accept(this);
    }

    private void ReportError(string message, Token token)
    {
        errorReporter.ReportErrorAtLocation("Resolution error", file, token.Line, token.Column, message);
    }

    public object? VisitProgram(ProgramStatement programStatement)
    {
        // Visit function and struct declarations first so they don't need to be declared in order.
        foreach (var stmt in programStatement.Statements)
        {
            if (stmt is FunctionDeclarationStatement funDecl)
            {
                scopeManager.Declare(funDecl.Identifier, funDecl);
            }
            else if (stmt is StructDeclarationStatement structDecl)
            {
                scopeManager.Declare(structDecl.Identifier, structDecl);
                // Skip fields and defer that to type checking
            }
        }

        foreach (var stmt in programStatement.Statements)
        {
            VisitStatement(stmt);
        }
        return null;
    }

    public object? VisitArrayAccess(ArrayAccessExpression arrayAccessExpression)
    {
        VisitExpression(arrayAccessExpression.Array);
        VisitExpression(arrayAccessExpression.Index);
        return null;
    }

    public object? VisitAssignment(AssignmentExpression assignmentExpression)
    {
        VisitExpression(assignmentExpression.Variable);
        VisitExpression(assignmentExpression.Value);
        return null;
    }

    public object? VisitBinary(BinaryExpression binaryExpression)
    {
        VisitExpression(binaryExpression.First);
        VisitExpression(binaryExpression.Second);
        return null;
    }

    public object? VisitBlock(BlockStatement blockStatement)
    {
        scopeManager.BeginScope();
        foreach (var stmt in blockStatement.Statements)
        {
            VisitStatement(stmt);
        }
        scopeManager.EndScope();
        return null;
    }

    public object? VisitCall(CallExpression callExpression)
    {
        VisitExpression(callExpression.Callee);
        foreach (var arg in callExpression.Arguments)
        {
            VisitExpression(arg);
        }
        return null;
    }

    public object? VisitExpressionStatement(ExpressionStatement expressionStatement)
    {
        VisitExpression(expressionStatement.Expression);
        return null;
    }

    public object? VisitFunctionDeclaration(FunctionDeclarationStatement functionDeclarationStatement)
    {
        // Don't declare the function identifier itself as functions are declared when visiting the ProgramStatement

        // Similar to visiting a block, but also declare all of the function parameters.
        scopeManager.BeginScope();
        foreach (var param in functionDeclarationStatement.Parameters)
        {
            VisitStatement(param);
        }
        foreach (var stmt in functionDeclarationStatement.Body.Statements)
        {
            VisitStatement(stmt);
        }
        scopeManager.EndScope();
        return null;
    }

    public object? VisitGroup(GroupExpression groupExpression)
    {
        VisitExpression(groupExpression.Expression);
        return null;
    }

    public object? VisitIf(IfStatement ifStatement)
    {
        VisitExpression(ifStatement.Condition);
        VisitBlock(ifStatement.IfBody);
        foreach (var elifStmt in ifStatement.ElseIfStatements)
        {
            VisitExpression(elifStmt.Condition);
            VisitBlock(elifStmt.IfBody);
        }
        if (ifStatement.ElseBody != null)
        {
            VisitBlock(ifStatement.ElseBody);
        }

        return null;
    }

    public object? VisitLiteral(LiteralExpression literalExpression)
    {
        // Possibly a type identifier for a function
        if (literalExpression.Literal.TokenType == TokenType.Identifier)
        {
            if (!scopeManager.TryResolve(literalExpression.Literal, literalExpression))
            {
                ReportError("Unresolved identifier", literalExpression.Literal);
            }
        }
        return null;
    }

    public object? VisitLogical(LogicalExpression logicalExpression)
    {
        VisitExpression(logicalExpression.First);
        VisitExpression(logicalExpression.Second);
        return null;
    }

    public object? VisitNewSizedArray(NewSizedArrayExpression newSizedArrayExpression)
    {
        foreach (var expr in newSizedArrayExpression.Sizes)
        {
            VisitExpression(expr);
        }
        return null;
    }

    public object? VisitNewStruct(NewStructExpression newStructExpression)
    {
        // Make sure that the struct type identifier is resolved
        if (!scopeManager.TryResolve(newStructExpression.Type.Type, newStructExpression))
        {
            ReportError("Unresolved struct type", newStructExpression.Type.Type);
        }
        return null;
    }

    public object? VisitPostfixIncrement(PostfixIncrementExpression postfixIncrementExpression)
    {
        VisitExpression(postfixIncrementExpression.Variable);
        return null;
    }

    public object? VisitPrefixUnary(PrefixUnaryExpression prefixUnaryExpression)
    {
        VisitExpression(prefixUnaryExpression.Expression);
        return null;
    }

    public object? VisitPropertyAccess(PropertyAccessExpression propertyAccessExpression)
    {
        VisitExpression(propertyAccessExpression.Object);
        // Defer checking the property to the type checking
        return null;
    }

    public object? VisitPropertyAssignment(PropertyAssignmentExpression propertyAssignmentExpression)
    {
        VisitExpression(propertyAssignmentExpression.Object);
        VisitExpression(propertyAssignmentExpression.Value);
        return null;
    }

    public object? VisitVariable(VariableExpression variableExpression)
    {
        if (!scopeManager.TryResolve(variableExpression.Identifier, variableExpression))
        {
            ReportError("Unresolved variable", variableExpression.Identifier);
        }
        return null;
    }

    public object? VisitReturnEmpty(ReturnEmptyStatement returnEmptyStatement)
    {
        // Nothing to resolve in an empty return
        return null;
    }

    public object? VisitReturnExpression(ReturnExpressionStatement returnExpressionStatement)
    {
        VisitExpression(returnExpressionStatement.Value);
        return null;
    }

    public object? VisitStructDeclaration(StructDeclarationStatement structDeclarationStatement)
    {
        // Do nothing as structs are declared when visiting the ProgramStatement
        return null;
    }

    public object? VisitVariableDeclaration(VariableDeclarationStatement variableDeclarationStatement)
    {
        scopeManager.Declare(variableDeclarationStatement.Identifier, variableDeclarationStatement);
        return null;
    }

    public object? VisitVariableDeclarationWithAssignment(VariableDeclarationWithAssignmentStatement variableDeclarationWithAssignmentStatement)
    {
        // Visit the value first to avoid statements like var x = x;
        VisitExpression(variableDeclarationWithAssignmentStatement.Value);
        VisitVariableDeclaration(variableDeclarationWithAssignmentStatement);
        return null;
    }

    public object? VisitWhile(WhileStatement whileStatement)
    {
        VisitExpression(whileStatement.Condition);
        VisitStatement(whileStatement.Body);
        return null;
    }
}
