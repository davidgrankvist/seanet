using Seanet.Compiler.Errors;
using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Parsing;

public class Parser
{
    private readonly ErrorReporter errorReporter;
    private int current = 0;
    private List<Token> tokens = [];

    public Parser(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
    }

    public Statement Parse(List<Token> tokens)
    {
        this.tokens = tokens;
        current = 0;
        var syntaxTree = ParseProgram(tokens);
        return syntaxTree;
    }

    private bool IsDone()
    {
        return current >= tokens.Count || Peek().TokenType == TokenType.Eof;
    }

    private Token Peek()
    {
        return tokens[current];
    }

    private void ReportError(string message)
    {
        throw new NotImplementedException();
    }

    private Statement ParseProgram(List<Token> tokens)
    {
        var statements = new List<Statement>();

        while (!IsDone())
        {
            var declaration = ParseFunctionDeclaration();
            statements.Add(declaration);
        }

        if (Peek().TokenType != TokenType.Eof)
        {
            ReportError("The parsing ended before the end of the file was reached.");
        }

        return new ProgramStatement
        {
            Statements = statements,
        };
    }

    private Statement ParseFunctionDeclaration()
    {
        throw new NotImplementedException();
    }
}