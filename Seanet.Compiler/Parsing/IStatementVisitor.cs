namespace Seanet.Compiler.Parsing;

public interface IStatementVisitor<TResult>
{
    TResult VisitProgram(ProgramStatement programStatement);
    TResult VisitExpressionStatement(ExpressionStatement expressionStatement);
    TResult VisitBlock(BlockStatement blockStatement);
    TResult VisitVariableDeclaration(VariableDeclarationStatement variableDeclarationStatement);
    TResult VisitVariableDeclarationWithAssignment(VariableDeclarationWithAssignmentStatement variableDeclarationWithAssignmentStatement);
    TResult VisitIf(IfStatement ifStatement);
    TResult VisitWhile(WhileStatement whileStatement);
    TResult VisitFunctionDeclaration(FunctionDeclarationStatement functionDeclarationStatement);
    TResult VisitReturnEmpty(ReturnEmptyStatement returnEmptyStatement);
    TResult VisitReturnExpression(ReturnExpressionStatement returnExpressionStatement);
    TResult VisitStructDeclaration(StructDeclarationStatement structDeclarationStatement);
}
