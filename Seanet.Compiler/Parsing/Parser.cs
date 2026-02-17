using Seanet.Compiler.Errors;
using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Parsing;

public class Parser
{
    private readonly ErrorReporter errorReporter;
    private string file = string.Empty;
    private int current = 0;
    private List<Token> tokens = [];

    public Parser(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
    }

    public Statement Parse(string file, List<Token> tokens)
    {
        this.tokens = tokens;
        current = 0;
        this.file = file;

        Statement? syntaxTree = null;
        try
        {
            syntaxTree = ParseProgram(tokens);
        }
        catch (ParsingAbortedException)
        {
            // The error messages are reported via the reporter.
        }

        var emptyProgram = new ProgramStatement()
        {
            Statements = [],
        };
        return syntaxTree ?? emptyProgram;
    }

    private bool IsDone()
    {
        return current >= tokens.Count || Peek().TokenType == TokenType.Eof;
    }

    private Token Peek()
    {
        return tokens[current];
    }


    private Token Previous()
    {
        return tokens[current - 1];
    }


    private Token Advance()
    {
        if (IsDone())
        {
            return Previous();
        }

        return tokens[current++];
    }

    private bool Check(TokenType tokenType)
    {
        return !IsDone() && Peek().TokenType == tokenType;
    }

    private bool Match(TokenType tokenType)
    {
        if (Check(tokenType))
        {
            Advance();
            return true;
        }
        return false;
    }

    private Token Consume(TokenType tokenType, string errorMessage)
    {
        if (Check(tokenType))
        {
            return Advance();
        }

        throw ReportErrorAndAbort(errorMessage);
    }

    private class ParsingAbortedException : Exception
    {
    }

    private void ReportError(string message)
    {
        var token = Peek();
        errorReporter.ReportParseError(file, token.Line, token.Column, message);
    }

    private ParsingAbortedException ReportErrorAndAbort(string message)
    {
        ReportError(message);
        return new ParsingAbortedException();
    }

    private Statement ParseProgram(List<Token> tokens)
    {
        var statements = ParseTopLevelStatements();

        return new ProgramStatement
        {
            Statements = statements,
        };
    }

    private List<Statement> ParseTopLevelStatements()
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

        return statements;
    }

    private FunctionDeclarationStatement ParseFunctionDeclaration()
    {
        var returnType = Consume(TokenType.Identifier, "Expected a function return type.");
        var identifier = Consume(TokenType.Identifier, "Expected a function name.");

        var parameters = new List<DeclarationStatement>();
        Consume(TokenType.ParenStart, "Expected ( before function parameter list.");
        while (!Check(TokenType.ParenEnd))
        {
            parameters.Add(ParseFunctionParameter());
        }
        Consume(TokenType.ParenEnd, "Expected ) at the end of function parameter list.");

        var body = ParseBlock();

        return new FunctionDeclarationStatement()
        {
            ReturnType = returnType,
            Identifier = identifier,
            Parameters = parameters,
            Body = body,
        };
    }

    private DeclarationStatement ParseFunctionParameter()
    {
        var type = Consume(TokenType.Identifier, "Expected a function parameter type.");
        var identifier = Consume(TokenType.Identifier, "Expected a function parameter name.");

        return new DeclarationStatement()
        {
            Type = type,
            Identifier = identifier,
        };
    }

    private BlockStatement ParseBlock()
    {
        var statements = new List<Statement>();
        Consume(TokenType.CurlyStart, "Expected a { at the beginning of a block.");
        while (!Check(TokenType.CurlyEnd))
        {
            statements.Add(ParseDeclaration());
        }
        Consume(TokenType.CurlyEnd, "Expected a } at the end of a block.");

        return new BlockStatement()
        {
            Statements = statements,
        };
    }

    private Statement ParseDeclaration()
    {
        if (Match(TokenType.Var))
        {
            return ParseVarDeclaration();
        }

        if (Match(TokenType.Identifier) && Check(TokenType.Identifier))
        {
            return ParseTypedVarDeclaration();
        }

        return ParseStatement();
    }

    private Statement ParseStatement()
    {
        if (Match(TokenType.CurlyStart))
        {
            return ParseBlock();
        }

        if (Match(TokenType.If))
        {
            return ParseIfStatement();
        }

        if (Match(TokenType.While))
        {
            return ParseWhileStatement();
        }

        if (Match(TokenType.For))
        {
            return ParseForStatement();
        }

        if (Match(TokenType.Return))
        {
            return ParseReturnStatement();
        }

        return ParseExpressionStatement();
    }

    private Statement ParseVarDeclaration()
    {
        var varToken = Previous();
        var identifier = Consume(TokenType.Identifier, "Expected a variable name in declaration.");
        Consume(TokenType.Equal, "A var declaration must be assigned to a value.");
        var value = ParseExpression();
        Consume(TokenType.SemiColon, "Expected ; at the end of variable declaration.");

        return new DeclarationWithAssignmentStatement()
        {
            Type = varToken,
            Identifier = identifier,
            Value = value,
        };
    }

    private Statement ParseTypedVarDeclaration()
    {
        var type = Previous();
        var identifier = Consume(TokenType.Identifier, "Expected a variable name in declaration.");

        if (Match(TokenType.Equal))
        {
            var value = ParseExpression();
            Consume(TokenType.SemiColon, "Expected ; at the end of variable declaration");
            return new DeclarationWithAssignmentStatement()
            {
                Type = type,
                Identifier = identifier,
                Value = value,
            };
        }
        Consume(TokenType.SemiColon, "Expected ; at the end of variable declaration");

        return new DeclarationStatement()
        {
            Type = type,
            Identifier = identifier,
        };
    }

    private ReturnStatement ParseReturnStatement()
    {
        var value = ParseExpression();
        Consume(TokenType.SemiColon, "Expected ; at the end of return statement");

        return new ReturnStatement()
        {
            Value = value,
        };
    }

    private IfStatement ParseIfStatement()
    {
        Consume(TokenType.ParenStart, "Expected ( before if condition");
        var ifCondition = ParseExpression();
        Consume(TokenType.ParenEnd, "Expected ) at the end of if condition");
        var ifBody = ParseBlock();

        var elseifStatements = new List<IfStatement>();
        while (Match(TokenType.ElseIf))
        {
            Consume(TokenType.ParenStart, "Expected ( before if condition");
            var elseIfCondition = ParseExpression();
            Consume(TokenType.ParenEnd, "Expected ) at the end of if condition");
            var elseIfBody = ParseBlock();

            var elseIf = new IfStatement()
            {
                Condition = elseIfCondition,
                IfBody = elseIfBody,
            };

            elseifStatements.Add(elseIf);
        }

        BlockStatement? elseBody = null;
        if (Match(TokenType.Else))
        {
            elseBody = ParseBlock();
        }

        return new IfStatement()
        {
            Condition = ifCondition,
            IfBody = ifBody,
            ElseBody = elseBody,
        };
    }

    private WhileStatement ParseWhileStatement()
    {
        Consume(TokenType.ParenStart, "Expected ( before while condition.");
        var condition = ParseExpression();
        Consume(TokenType.ParenEnd, "Expected ) after while condition.");
        var body = ParseBlock();

        return new WhileStatement()
        {
            Condition = condition,
            Body = body,
        };
    }

    private BlockStatement ParseForStatement()
    {
        Consume(TokenType.ParenStart, "Expected ( before for parameters.");

        Statement? initializer = null;
        if (Match(TokenType.SemiColon))
        {
            // no initializer
        }
        else if (Match(TokenType.Var))
        {
            initializer = ParseVarDeclaration();
        }
        else if (Match(TokenType.Identifier) && Check(TokenType.Identifier))
        {
            initializer = ParseTypedVarDeclaration();
        }
        else
        {
            // other initializer statement
            initializer = ParseExpressionStatement();
        }

        var tokenBeforeCondition = Peek();

        Expression? condition = null;
        if (!Check(TokenType.SemiColon))
        {
            condition = ParseExpression();
        }
        Consume(TokenType.SemiColon, "Expected ; after for condition");

        Expression? increment = null;
        if (!Check(TokenType.ParenEnd))
        {
            increment = ParseExpression();
        }
        Consume(TokenType.ParenEnd, "Expected ) after for parameters.");

        var body = ParseBlock();
        return DesugarForStatementToWhile(
            initializer,
            condition,
            increment,
            body,
            tokenBeforeCondition);
    }

    /*
     * This for loop
     *
     * for (var i = 0; i < 10, i++) 
     * {
     * }
     *
     * is equivalent to this while loop in a block
     *
     * {
     *      var i = 0;
     *      while (i < 10)
     *      {
     *          i++;
     *      }       
     * }
     */
    private BlockStatement DesugarForStatementToWhile(
        Statement? initializer,
        Expression? condition,
        Expression? increment,
        BlockStatement body,
        Token tokenBeforeCondition)
    {
        var outerStatements = new List<Statement>();
        if (initializer != null)
        {
            outerStatements.Add(initializer);
        }

        Expression whileCondition;
        if (condition == null)
        {
            // Create a True literal for the infinite loop condition
            whileCondition = new LiteralExpression()
            {
                Literal = new Token
                {
                    TokenType = TokenType.True,
                    // Use a reference token for all of the other required fields.
                    Start = tokenBeforeCondition.Start,
                    Length = tokenBeforeCondition.Length,
                    Source = tokenBeforeCondition.Source,
                    Line = tokenBeforeCondition.Line,
                    Column = tokenBeforeCondition.Column,
                }
            };
        }
        else
        {
            whileCondition = condition;
        }

        var innerStatements = new List<Statement>();
        innerStatements.AddRange(body.Statements);
        if (increment != null)
        {
            innerStatements.Add(new ExpressionStatement()
            {
                Expression = increment,
            });
        }
        var innerBlock = new BlockStatement()
        {
            Statements = innerStatements,
        };

        var whileStatement =  new WhileStatement
        {
            Condition = whileCondition,
            Body = innerBlock,
        };

        innerStatements.Add(whileStatement);

        return new BlockStatement()
        {
            Statements = outerStatements,
        };
    }

    private ExpressionStatement ParseExpressionStatement()
    {
        var expression = ParseExpression();
        Consume(TokenType.SemiColon, "Expected ; at the end of statement.");
        return new ExpressionStatement()
        {
            Expression = expression,
        };
    }

    private Expression ParseExpression()
    {
        throw new NotImplementedException();
    }
}