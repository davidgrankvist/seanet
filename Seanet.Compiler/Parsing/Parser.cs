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
        // simply ignore comments for now
        this.tokens = tokens.Where(x => x.TokenType != TokenType.Comment).ToList();
        current = 0;
        this.file = file;

        Statement? syntaxTree = null;
        try
        {
            syntaxTree = ParseProgram();
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


    private Token PeekNext()
    {
        return tokens[current + 1];
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

    private bool CheckNext(TokenType tokenType)
    {
        return !IsDone() && current + 1 < tokens.Count && PeekNext().TokenType == tokenType;
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

    private bool MatchAny(params TokenType[] tokenTypes)
    {
        for (int i = 0; i < tokenTypes.Length; i++)
        {
            var tokenType = tokenTypes[i];
            if (Check(tokenType))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool MatchLiteral()
    {
        return MatchAny(
            TokenType.Null,
            TokenType.True,
            TokenType.False,
            TokenType.StringLiteral,
            TokenType.IntLiteral,
            TokenType.UIntLiteral,
            TokenType.LongLiteral,
            TokenType.ULongLiteral,
            TokenType.FloatLiteral,
            TokenType.DoubleLiteral
        );
    }

    private bool MatchBuiltinVariableType()
    {
        return MatchAny(
            TokenType.Bool,
            TokenType.Byte,
            TokenType.Short,
            TokenType.UShort,
            TokenType.Int,
            TokenType.UInt,
            TokenType.Long,
            TokenType.ULong,
            TokenType.Float,
            TokenType.Double,
            TokenType.String);
    }

    private bool MatchElseIf()
    {
        if (Check(TokenType.Else) && CheckNext(TokenType.If))
        {
            Advance();
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

    private Token ConsumeAny(TokenType[] tokenTypes, string errorMessage)
    {
        for (var i = 0; i < tokenTypes.Length; i++)
        {
            var tokenType = tokenTypes[i];
            if (Check(tokenType))
            {
                return Advance();
            }
        }

        throw ReportErrorAndAbort(errorMessage);
    }

    private Token ConsumeBuiltinTypeOrIdentifier(string errorMessage)
    {
        return ConsumeAny([
            TokenType.Bool,
            TokenType.Byte,
            TokenType.Short,
            TokenType.UShort,
            TokenType.Int,
            TokenType.UInt,
            TokenType.Long,
            TokenType.ULong,
            TokenType.Float,
            TokenType.Double,
            TokenType.String,
            TokenType.Identifier,
        ], errorMessage);
    }

    private TypeInfo ConsumeTypeOrIdentifier(string errorMessage)
    {
        var isReference = false;
        if (Match(TokenType.Ref))
        {
            isReference = true;
        }

        if (Match(TokenType.Fun))
        {
            if (isReference)
            {
                throw ReportErrorAndAbort("Invalid ref. The ref keyword cannot be applied on a function pointer type.");
            }
            return ParseFunctionTypeInfo();
        }

        var typeToken = ConsumeBuiltinTypeOrIdentifier(errorMessage);

        if (Match(TokenType.SquareStart))
        {
            return ConsumeArrayType(typeToken, isReference);
        }

        return new SingleTokenTypeInfo
        {
            Type = typeToken,
            IsReference = isReference,
        };
    }

    private FunctionTypeInfo ParseFunctionTypeInfo()
    {
        if (Match(TokenType.Less))
        {
            // Parse types like fun<int> or fun<int, string, double>. The last parameter is the return type.
            TypeInfo? returnType = null;
            var parameters = new List<TypeInfo>();
            do
            {
                var parameterType = ConsumeTypeOrIdentifier("Expected a function type parameter.");
                if (Check(TokenType.Comma))
                {
                    parameters.Add(parameterType);
                }
                else
                {
                    returnType = parameterType;
                }
            } while (Match(TokenType.Comma));

            Consume(TokenType.Greater, "Expected > at the end of function pointer type parameters.");

            return new FunctionTypeInfo
            {
                ReturnType = returnType!,
                Parameters = parameters,
            };
        }
        else
        {
            // Parse the fun type, which is a void with no params.
            var funToken = Previous();
            var voidToken = new Token
            {
                TokenType = TokenType.Void,
                // Use a reference token for all of the other required fields.
                Start = funToken.Start,
                Length = funToken.Length,
                Source = funToken.Source,
                Line = funToken.Line,
                Column = funToken.Column,
            };
            var voidReturnType = new SingleTokenTypeInfo
            {
                Type = voidToken,
                IsReference = false,
            };
            return new FunctionTypeInfo
            {
                ReturnType = voidReturnType,
                Parameters = [],
            };
        }
    }

    /// <summary>
    /// Indicates that parsing was aborted.
    /// 
    /// This exception is used internally to abort parsing down in the call stack.
    /// It is caught and the user facing errors can be read from the error reporter.
    /// </summary>
    private class ParsingAbortedException : Exception
    {
    }

    private void ReportError(string message, Token token)
    {
        errorReporter.ReportParseError(file, token.Line, token.Column, message);
    }

    private ParsingAbortedException ReportErrorAndAbort(string message, Token? token = null)
    {
        var tokenArg = token.HasValue ? token.Value : Peek();
        ReportError(message, tokenArg);
        return new ParsingAbortedException();
    }

    private ProgramStatement ParseProgram()
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
            if (Match(TokenType.Struct))
            {
                var declaration = ParseStructDeclaration();
                statements.Add(declaration);
            }
            else
            {
                var declaration = ParseFunctionDeclaration();
                statements.Add(declaration);
            }

        }

        if (Peek().TokenType != TokenType.Eof)
        {
            throw ReportErrorAndAbort("The parsing ended before the end of the file was reached.");
        }

        return statements;
    }

    private StructDeclarationStatement ParseStructDeclaration()
    {
        var identifier = Consume(TokenType.Identifier, "Expected a struct name in struct declaration.");
        Consume(TokenType.CurlyStart, "Expected { before struct fields.");

        var fields = new List<VariableDeclarationStatement>();
        while (!Check(TokenType.CurlyEnd))
        {
            var fieldTypeInfo = ConsumeTypeOrIdentifier("Failed to parse struct. Expected a field type.");
            var declaration = ParseTypedVarDeclaration(fieldTypeInfo);
            if (declaration is not VariableDeclarationStatement declarationStatement)
            {
                throw ReportErrorAndAbort("Failed to parse struct field. Must be a variable declaration without assignment.");
            }
            fields.Add(declarationStatement);
        }
        Consume(TokenType.CurlyEnd, "Expected } after struct fields.");

        return new StructDeclarationStatement
        {
            Identifier = identifier,
            Fields = fields,
        };
    }

    private FunctionDeclarationStatement ParseFunctionDeclaration()
    {
        TypeInfo returnType;
        if (Match(TokenType.Void))
        {
            var voidToken = Previous();
            returnType = new SingleTokenTypeInfo
            {
                Type = voidToken,
                IsReference = false,
            };
        }
        else
        {
            returnType = ConsumeTypeOrIdentifier("Expected a function return type.");
        }
        var identifier = Consume(TokenType.Identifier, "Expected a function name.");

        var parameters = new List<VariableDeclarationStatement>();
        Consume(TokenType.ParenStart, "Expected ( before function parameter list.");
        if (!Check(TokenType.ParenEnd))
        {
            do
            {
                parameters.Add(ParseFunctionParameter());
            } while (Match(TokenType.Comma));
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

    private VariableDeclarationStatement ParseFunctionParameter()
    {
        var type = ConsumeTypeOrIdentifier("Expected a function parameter type.");
        var identifier = Consume(TokenType.Identifier, "Expected a function parameter name.");

        return new VariableDeclarationStatement()
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

    private bool MatchTypeIdentifier()
    {
        // Look ahead to make sure that the identifier is used as a type and is not a variable. 
        if (Check(TokenType.Identifier) && CheckNext(TokenType.Identifier))
        {
            Advance();
            return true;
        }

        return false;
    }

    private bool MatchArrayItemType()
    {
        return MatchBuiltinVariableType() || Match(TokenType.Identifier);
    }

    private bool MatchVariableType(out TypeInfo typeInfo)
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        typeInfo = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        // types like int or int[]
        if (MatchBuiltinVariableType())
        {
            var typeToken = Previous();
            if (Match(TokenType.SquareStart))
            {
                typeInfo = ConsumeArrayType(typeToken, isReference: false);
            }
            else
            {
                typeInfo = new SingleTokenTypeInfo()
                {
                    Type = typeToken,
                    IsReference = false,
                };
            }
        }

        // library or user defined types, but not arrays
        if (MatchTypeIdentifier())
        {
            var typeToken = Previous();
            typeInfo = new SingleTokenTypeInfo()
            {
                Type = typeToken,
                IsReference = false,
            };
        }

        // array of library or user defined types
        if (Check(TokenType.Identifier) && CheckNext(TokenType.SquareStart))
        {
            var typeToken = Peek();
            Advance(); // move past type
            Advance(); // move past opening [
            typeInfo = ConsumeArrayType(typeToken, isReference: false);
        }

        return typeInfo != null;
    }

    private ArrayTypeInfo ConsumeArrayType(Token itemTypeToken, bool isReference)
    {
        Consume(TokenType.SquareEnd, "Expected ] at the end of array type.");
        var itemType = new SingleTokenTypeInfo
        {
            Type = itemTypeToken,
            IsReference = false,
        };
        var arrayType = new ArrayTypeInfo
        {
            ItemType = itemType,
            IsReference = isReference,
        };

        // Handle multi-dimensional arrays by nesting array type infos.
        while (Match(TokenType.SquareStart))
        {
            Consume(TokenType.SquareEnd, "Expected ] at the end of array type.");

            arrayType = new ArrayTypeInfo
            {
                ItemType = arrayType,
                IsReference = isReference,
            };
        }

        return arrayType;
    }

    private Statement ParseDeclaration()
    {
        if (Match(TokenType.Var))
        {
            return ParseVarDeclaration();
        }

        if (MatchVariableType(out var typeInfo))
        {
            return ParseTypedVarDeclaration(typeInfo);
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

    private VariableDeclarationWithAssignmentStatement ParseVarDeclaration()
    {
        var varToken = Previous();
        var identifier = Consume(TokenType.Identifier, "Expected a variable name in inferred type declaration.");
        Consume(TokenType.Equal, "A var declaration must be assigned to a value.");
        var value = ParseExpression();
        Consume(TokenType.SemiColon, "Expected ; at the end of inferred type variable declaration.");

        var typeInfo = new SingleTokenTypeInfo
        {
            Type = varToken,
            IsReference = false,
        };

        return new VariableDeclarationWithAssignmentStatement()
        {
            Type = typeInfo,
            Identifier = identifier,
            Value = value,
        };
    }

    private Statement ParseTypedVarDeclaration(TypeInfo typeInfo)
    {
        var identifier = Consume(TokenType.Identifier, "Expected a variable name in declaration.");

        if (Match(TokenType.Equal))
        {
            var value = ParseExpression();
            Consume(TokenType.SemiColon, "Expected ; at the end of typed variable declaration");
            return new VariableDeclarationWithAssignmentStatement()
            {
                Type = typeInfo,
                Identifier = identifier,
                Value = value,
            };
        }
        Consume(TokenType.SemiColon, "Expected ; at the end of variable declaration");

        return new VariableDeclarationStatement()
        {
            Type = typeInfo,
            Identifier = identifier,
        };
    }

    private Statement ParseReturnStatement()
    {
        if (Match(TokenType.SemiColon))
        {
            return new ReturnEmptyStatement();
        }

        var value = ParseExpression();
        Consume(TokenType.SemiColon, "Expected ; at the end of return statement");

        return new ReturnExpressionStatement()
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
        while (MatchElseIf())
        {
            Consume(TokenType.ParenStart, "Expected ( before else if condition");
            var elseIfCondition = ParseExpression();
            Consume(TokenType.ParenEnd, "Expected ) at the end of else if condition");
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
        else if (MatchVariableType(out var typeInfo))
        {
            initializer = ParseTypedVarDeclaration(typeInfo);
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
    private static BlockStatement DesugarForStatementToWhile(
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

        var whileStatement = new WhileStatement
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
        return ParseAssignment();
    }

    private Expression ParseAssignment()
    {
        var expression = ParseLogicalOr();

        if (MatchAny(TokenType.Equal,
            TokenType.PlusEquals,
            TokenType.MinusEquals,
            TokenType.StarEquals,
            TokenType.SlashEquals))
        {
            // Use the equals token for error reporting that points to where the assignment occurred.
            var equalsToken = Previous();
            var valueExpression = ParseAssignment();

            return expression switch
            {
                VariableExpression varExp => new AssignmentExpression()
                {
                    Identifier = varExp.Identifier,
                    Expression = valueExpression,
                },
                PropertyAccessExpression propAccessExp => new PropertyAssignmentExpression()
                {
                    Object = propAccessExp.Object,
                    Property = propAccessExp.Property,
                    Value = valueExpression,
                },
                _ => throw ReportErrorAndAbort("Invalid assignment. The left hand side of the assignment must be a variable or an object property. ", equalsToken),
            };
        }

        return expression;
    }

    private Expression ParseLogicalOr()
    {
        var expression = ParseLogicalAnd();

        while (Match(TokenType.LogicalOr))
        {
            var operatorToken = Previous();
            var rightExpression = ParseLogicalAnd();
            expression = new LogicalExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseLogicalAnd()
    {
        var expression = ParseBitwiseOr();

        while (Match(TokenType.LogicalAnd))
        {
            var operatorToken = Previous();
            var rightExpression = ParseBitwiseOr();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseBitwiseOr()
    {
        var expression = ParseBitwiseXor();

        while (Match(TokenType.BitwiseOr))
        {
            var operatorToken = Previous();
            var rightExpression = ParseBitwiseXor();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseBitwiseXor()
    {
        var expression = ParseBitwiseAnd();

        while (Match(TokenType.BitwiseXor))
        {
            var operatorToken = Previous();
            var rightExpression = ParseBitwiseAnd();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseBitwiseAnd()
    {
        var expression = ParseEquality();

        while (Match(TokenType.BitwiseAnd))
        {
            var operatorToken = Previous();
            var rightExpression = ParseEquality();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseEquality()
    {
        var expression = ParseComparison();

        while (MatchAny(TokenType.EqualEqual, TokenType.NotEqual))
        {
            var operatorToken = Previous();
            var rightExpression = ParseComparison();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseComparison()
    {
        var expression = ParseBitShift();

        while (MatchAny(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            var operatorToken = Previous();
            var rightExpression = ParseBitShift();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseBitShift()
    {
        var expression = ParseTerm();

        while (MatchAny(TokenType.BitShiftLeft, TokenType.BitShiftRight))
        {
            var operatorToken = Previous();
            var rightExpression = ParseTerm();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseTerm()
    {
        var expression = ParseFactor();

        while (MatchAny(TokenType.Plus, TokenType.Minus))
        {
            var operatorToken = Previous();
            var rightExpression = ParseFactor();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseFactor()
    {
        var expression = ParseUnary();

        while (MatchAny(TokenType.Star, TokenType.Slash, TokenType.Mod))
        {
            var operatorToken = Previous();
            var rightExpression = ParseUnary();
            expression = new BinaryExpression()
            {
                Operator = operatorToken,
                First = expression,
                Second = rightExpression,
            };
        }

        return expression;
    }

    private Expression ParseUnary()
    {
        if (MatchAny(TokenType.LogicalNot, TokenType.Minus, TokenType.PlusPlus, TokenType.MinusMinus))
        {
            var operatorToken = Previous();
            var expression = ParseUnary();
            return new PrefixUnaryExpression()
            {
                Operator = operatorToken,
                Expression = expression,
            };
        }

        return ParseCall();
    }

    /// <summary>
    /// Handles chains of calls, property access and array access.
    /// </summary>
    private Expression ParseCall()
    {
        var expression = ParsePrimary();

        while (true)
        {
            if (Match(TokenType.ParenStart))
            {
                expression = ParseCallExpression(expression);
            }
            else if (Match(TokenType.Dot))
            {
                var identifierToken = Consume(TokenType.Identifier, "Expected an identifier after property accessor.");
                expression = new PropertyAccessExpression
                {
                    Object = expression,
                    Property = identifierToken,
                };
            }
            else if (Match(TokenType.SquareStart))
            {
                var accessExpression = ParseExpression();
                Consume(TokenType.SquareEnd, "Expected ] at the end of array access.");
                expression = new ArrayAccessExpression()
                {
                    Array = expression,
                    Index = accessExpression,
                };
            }
            else
            {
                break;
            }
        }


        return expression;
    }

    private Expression ParseCallExpression(Expression callee)
    {
        if (Match(TokenType.ParenEnd))
        {
            return new CallExpression()
            {
                Callee = callee,
                Arguments = [],
            };
        }

        var arguments = new List<Expression>();
        do
        {
            Expression argumentExpression;
            if (Match(TokenType.Ref))
            {
                var identifier = Consume(TokenType.Identifier, "Invalid ref. Expected a variable.");
                argumentExpression = new VariableExpression
                {
                    Identifier = identifier,
                    IsReference = true,
                };
            }
            else
            {
                argumentExpression = ParseExpression();
            }

            arguments.Add(argumentExpression);
        } while (Match(TokenType.Comma));

        Consume(TokenType.ParenEnd, "Expected ) at the end of function call.");

        return new CallExpression()
        {
            Callee = callee,
            Arguments = arguments,
        };
    }

    private Expression ParseNewExpression()
    {
        if (MatchArrayItemType())
        {
            if (Check(TokenType.SquareStart))
            {
                return ParseSizedArrayConstructorCall();
            }
            else
            {
                return ParseStructConstructorCall();
            }
        }

        throw ReportErrorAndAbort("Failed to parse new expression. Unexpected constructor.");
    }

    private NewStructExpression ParseStructConstructorCall()
    {
        var typeToken = Peek();
        var typeInfo = new SingleTokenTypeInfo
        {
            Type = typeToken,
            IsReference = false,
        };
        Consume(TokenType.ParenStart, "Expected ( at the beginning of struct constructor call.");
        // No arguments for now. Not sure if structs in this language will have parameterized constructors.
        Consume(TokenType.ParenEnd, "Expected ) at the end of struct constructor call.");

        return new NewStructExpression
        {
            Type = typeInfo,
        };
    }

    // parse sized array expressions like int[5] or int[SomeFunction()]
    private Expression ParseSizedArrayConstructorCall()
    {
        var typeToken = Previous();
        var itemType = new SingleTokenTypeInfo
        {
            Type = typeToken,
            IsReference = false,
        };

        TypeInfo typeInfo = itemType;
        var arraySizes = new List<Expression>();
        while (Match(TokenType.SquareStart))
        {
            var arraySize = ParseExpression();
            arraySizes.Add(arraySize);

            typeInfo = new ArrayTypeInfo
            {
                ItemType = typeInfo,
                IsReference = false,
            };

            Consume(TokenType.SquareEnd, "Expected ] at the end of sized array type.");
        }

        if (typeInfo is ArrayTypeInfo arrayType)
        {
            return new NewSizedArrayExpression
            {
                Type = arrayType,
                Sizes = arraySizes,
            };
        }

        throw ReportErrorAndAbort("Failed to parse new array expression. Unexpected token.");
    }

    private Expression ParsePrimary()
    {
        if (MatchLiteral())
        {
            return new LiteralExpression()
            {
                Literal = Previous(),
            };
        }

        if (Match(TokenType.ParenStart))
        {
            var expression = ParseExpression();
            Consume(TokenType.ParenEnd, "Expected ) at the end of parenthesis group.");
            return new GroupExpression()
            {
                Expression = expression,
            };
        }

        if (Match(TokenType.Identifier))
        {
            var identifier = Previous();
            if (MatchAny(TokenType.PlusPlus, TokenType.MinusMinus))
            {
                var operatorToken = Previous();
                return new PostfixIncrementExpression()
                {
                    Operator = operatorToken,
                    Identifier = identifier,
                };
            }
            else
            {
                return new VariableExpression()
                {
                    Identifier = identifier,
                    IsReference = false,
                };
            }
        }

        if (Match(TokenType.New))
        {
            return ParseNewExpression();
        }

        throw ReportErrorAndAbort("Unexpected expression. Expected a literal, parenthesis grouping or identifier.");
    }
}