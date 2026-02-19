using System.Globalization;
using Seanet.Compiler.Errors;

namespace Seanet.Compiler.Scanning;

public class Scanner
{
    private int line;
    private int column;
    private int tokenStart;
    private int current;
    private List<Token> tokens = [];
    private string source = string.Empty;
    private string file = string.Empty;
    private readonly ErrorReporter errorReporter;

    public Scanner(ErrorReporter errorReporter)
    {
        this.errorReporter = errorReporter;
    }

    public List<Token> Scan(string file, string source)
    {
        this.file = file;
        this.source = source;
        line = 1;
        column = 1;
        tokens = [];

        while (!IsDone())
        {
            tokenStart = current;
            ScanToken();
        }

        ProduceToken(TokenType.Eof);
        return tokens;
    }

    private bool IsDone()
    {
        return current >= source.Length;
    }

    private void ScanToken()
    {
        var c = Advance();
        switch (c)
        {
            // Whitespace
            case ' ':
            case '\t':
            case '\r':
                break;
            case '\n':
                NextLine();
                break;
            // Single character tokens
            case '{':
                ProduceToken(TokenType.CurlyStart);
                break;
            case '}':
                ProduceToken(TokenType.CurlyEnd);
                break;
            case '(':
                ProduceToken(TokenType.ParenStart);
                break;
            case ')':
                ProduceToken(TokenType.ParenEnd);
                break;
            case '[':
                ProduceToken(TokenType.SquareStart);
                break;
            case ']':
                ProduceToken(TokenType.SquareEnd);
                break;
            case ',':
                ProduceToken(TokenType.Comma);
                break;
            case '.':
                ProduceToken(TokenType.Dot);
                break;
            case ';':
                ProduceToken(TokenType.SemiColon);
                break;
            case '*':
                ProduceToken(TokenType.Star);
                break;
            case '~':
                ProduceToken(TokenType.LogcalNot);
                break;
            case '^':
                ProduceToken(TokenType.BitwiseXor);
                break;
            // Multiple character tokens
            case '+':
                if (Match('+'))
                {
                    ProduceToken(TokenType.PlusPlus);
                }
                else if (Match('='))
                {
                    ProduceToken(TokenType.PlusEquals);
                }
                else
                {
                    ProduceToken(TokenType.Plus);
                }
                break;
            case '-':
                if (Match('-'))
                {
                    ProduceToken(TokenType.MinusMinus);
                }
                else if (Match('='))
                {
                    ProduceToken(TokenType.MinusEquals);
                }
                else
                {
                    ProduceToken(TokenType.Minus);
                }
                break;
            case '/':
                if (Match('/'))
                {
                    ScanSingleLineComment();
                }
                else if (Match('*'))
                {
                    ScanMultiLineComment();
                }
                else
                {
                    ProduceToken(TokenType.Slash);
                }
                break;
            case '!':
                if (Match('='))
                {
                    ProduceToken(TokenType.NotEqual);
                }
                else
                {
                    ProduceToken(TokenType.LogicalNot);
                }
                break;
            case '=':
                if (Match('='))
                {
                    ProduceToken(TokenType.EqualEqual);
                }
                else
                {
                    ProduceToken(TokenType.Equal);
                }
                break;
            case '<':
                if (Match('='))
                {
                    ProduceToken(TokenType.LessEqual);
                }
                else
                {
                    ProduceToken(TokenType.Less);
                }
                break;
            case '>':
                if (Match('='))
                {
                    ProduceToken(TokenType.GreaterEqual);
                }
                else
                {
                    ProduceToken(TokenType.Greater);
                }
                break;
            case '&':
                if (Match('&'))
                {
                    ProduceToken(TokenType.LogicalAnd);
                }
                else
                {
                    ProduceToken(TokenType.BitwiseAnd);
                }
                break;
            case '|':
                if (Match('|'))
                {
                    ProduceToken(TokenType.LogicalOr);
                }
                else
                {
                    ProduceToken(TokenType.BitwiseOr);
                }
                break;
            case '"':
                ScanString();
                break;
            default:
                if (IsDigit(c))
                {
                    ScanNumber();
                }
                else if (IsAlpha(c))
                {
                    ScanKeywordOrIdentifier();
                }
                else
                {
                    ReportError($"Unexpected character \"{c}\".");
                }
                break;
        }
    }

    /// <summary>
    /// Parse literals of various numeric types and formats. The parsed number is added
    /// to the produced token.
    /// </summary>
    private void ScanNumber()
    {
        while (IsDigit(Peek()))
        {
            Advance();
        }

        var isHex = false;
        if (Match('x') || Match('X'))
        {
            isHex = true;
            while (IsDigit(Peek()))
            {
                Advance();
            }
        }

        if (!isHex && (Peek() == 'e' || Peek() == 'E' || (Peek() == '.' && IsDigit(PeekNext()))))
        {
            // double or float literal

            if (Peek() == 'e' || Peek() == 'E')
            {
                // advance past the e
                Advance();
                
                if (PeekNext() == '-' || PeekNext() == '+')
                {
                    // advance past the - or +
                    Advance();
                }
            }

            Advance();

            while (IsDigit(Peek()))
            {
                Advance();
            }

            if (Match('f') || Match('F'))
            {
                // float literal
                ProduceFloatToken();
            }
            else
            {
                // double literal
                ProduceDoubleToken();
            }
        }
        else if (Peek() == 'u' || Peek() == 'U')
        {
            // ulong or uint
            Advance();

            var numberStyle = isHex ? NumberStyles.HexNumber : NumberStyles.Integer;
            if (Match('l') || Match('L'))
            {
                ProduceUnsignedLongToken(numberStyle);
            }
            else
            {
                ProduceUnsignedIntToken(numberStyle);
            }
        }
        else
        {
            var numberStyle = isHex ? NumberStyles.HexNumber : NumberStyles.Integer;

            // long or int
            if (Match('l') || Match('L'))
            {
                ProduceLongToken(numberStyle);
            }
            else
            {
                ProduceIntToken(numberStyle);
            }
        }
    }

    private void ProduceFloatToken()
    {
        var text = GetTokenSpan();
        // drop f
        var noSuffix = text.Slice(0, text.Length - 1);
        var toParse = noSuffix;
        try
        {
            var value = float.Parse(toParse, NumberStyles.Float, CultureInfo.InvariantCulture);
            ProduceToken(TokenType.FloatLiteral, value);
        }
        catch (Exception e)
        {
            ReportError($"Failed to parse float {text} - {e}");
        }
    }

    private void ProduceDoubleToken()
    {
        var text = GetTokenSpan();
        try
        {
            var value = double.Parse(text, CultureInfo.InvariantCulture);
            ProduceToken(TokenType.DoubleLiteral, value);
        }
        catch (Exception e)
        {
            ReportError($"Failed to parse double {text} - {e}");
        }
    }

    private void ProduceIntToken(NumberStyles numberStyles)
    {
        var text = GetTokenSpan();
        ReadOnlySpan<char> toParse;
        if (numberStyles == NumberStyles.HexNumber)
        {
            // drop 0x
            toParse = text.Slice(2);
        }
        else
        {
            toParse = text;
        }

        try
        {
            var value = int.Parse(toParse, numberStyles, CultureInfo.InvariantCulture);
            ProduceToken(TokenType.IntLiteral, value);
        }
        catch (Exception e)
        {
            ReportError($"Failed to parse int {text} - {e}");
        }
    }

    private void ProduceUnsignedIntToken(NumberStyles numberStyles)
    {
        var text = GetTokenSpan();
        // drop u
        var noSuffix = text.Slice(0, text.Length - 1);
        ReadOnlySpan<char> toParse;
        if (numberStyles == NumberStyles.HexNumber)
        {
            // drop 0x
            toParse = noSuffix.Slice(2);
        }
        else
        {
            toParse = noSuffix;
        }
        try
        {
            var value = uint.Parse(toParse, numberStyles, CultureInfo.InvariantCulture);
            ProduceToken(TokenType.UIntLiteral, value);
        }
        catch (Exception e)
        {
            ReportError($"Failed to parse uint {text} - {e}");
        }
    }

    private void ProduceLongToken(NumberStyles numberStyles)
    {
        var text = GetTokenSpan();
        // drop u
        var noSuffix = text.Slice(0, text.Length - 1);

        ReadOnlySpan<char> toParse;
        if (numberStyles == NumberStyles.HexNumber)
        {
            // drop 0x
            toParse = noSuffix.Slice(2);
        }
        else
        {
            toParse = noSuffix;
        }
        try
        {
            var value = long.Parse(toParse, numberStyles, CultureInfo.InvariantCulture);
            ProduceToken(TokenType.LongLiteral, value);
        }
        catch (Exception e)
        {
            ReportError($"Failed to parse long {text} - {e}");
        }
    }

    private void ProduceUnsignedLongToken(NumberStyles numberStyles)
    {
        var text = GetTokenSpan();
        // drop ul
        var noSuffix = text.Slice(0, text.Length - 2);
        ReadOnlySpan<char> toParse;
        if (numberStyles == NumberStyles.HexNumber)
        {
            // drop 0x
            toParse = noSuffix.Slice(2);
        }
        else
        {
            toParse = noSuffix;
        }

        try
        {
            var value = ulong.Parse(toParse, numberStyles, CultureInfo.InvariantCulture);
            ProduceToken(TokenType.ULongLiteral, value);
        }
        catch (Exception e)
        {
            ReportError($"Failed to parse ulong {text} - {e}");
        }
    }

    private void ScanKeywordOrIdentifier()
    {
        while (IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        var identifier = GetTokenSpan();
        if (TryParseKeyword(identifier, out var tokenType))
        {
            ProduceToken(tokenType);
        }
        else
        {
            ProduceToken(TokenType.Identifier);
        }
    }

    private static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private static bool IsAlpha(char c)
    {
        return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
    }

    private static bool IsAlphaNumeric(char c)
    {
        return IsDigit(c) || IsAlpha(c);
    }

    private void ProduceToken(TokenType tokenType, object? value = null)
    {
        tokens.Add(new()
        {
            TokenType = tokenType,
            Line = line,
            Column = column,
            Start = tokenStart,
            Length = current - tokenStart,
            Source = source,
            Value = value,
        });
    }

    private void NextLine()
    {
        column = 1;
        line++;
    }

    private char Advance()
    {
        column++;
        return source[current++];
    }

    private bool Match(char c)
    {
        if (IsDone())
        {
            return false;
        }

        if (Peek() != c)
        {
            return false;
        }

        Advance();
        return true;
    }

    private char Peek()
    {
        return current >= source.Length ? '\0' : source[current];
    }

    private char PeekNext()
    {
        return current + 1 >= source.Length ? '\0' : source[current + 1];
    }

    private ReadOnlySpan<char> GetTokenSpan()
    {
        return source.AsSpan(tokenStart, current - tokenStart);
    }

    private void ScanSingleLineComment()
    {
        while (Peek() != '\n' && !IsDone())
        {
            Advance();
        }

        ProduceToken(TokenType.Comment);
    }

    private void ScanMultiLineComment()
    {
        while (!(Peek() == '*' && PeekNext() == '/') && !IsDone())
        {
            Advance();
            if (Peek() == '\n')
            {
                NextLine();
            }
        }

        if (IsDone())
        {
            ReportError("Untermined multi-line comment. Expected */, but reached end of file.");
        }
        else
        {
            ProduceToken(TokenType.Comment);
        }
    }

    private void ScanString()
    {
        while (Peek() != '"' && !IsDone())
        {
            Advance();
        }

        if (!Match('"'))
        {
            ReportError("Untermined string. Expected \", but reached end of file.");
            return;
        }

        ProduceToken(TokenType.StringLiteral);
    }

    private void ReportError(string message)
    {
        errorReporter.ReportParseError(file, line, column, message);
    }

    private static bool TryParseKeyword(ReadOnlySpan<char> chars, out TokenType tokenType)
    {
        var didMatch = true;
        switch (chars)
        {
            case "true":
                tokenType = TokenType.True;
                break;
            case "false":
                tokenType = TokenType.False;
                break;
            case "if":
                tokenType = TokenType.If;
                break;
            case "else":
                tokenType = TokenType.Else;
                break;
            case "return":
                tokenType = TokenType.Return;
                break;
            case "for":
                tokenType = TokenType.For;
                break;
            case "while":
                tokenType = TokenType.While;
                break;
            case "break":
                tokenType = TokenType.Break;
                break;
            case "continue":
                tokenType = TokenType.Break;
                break;
            case "byte":
                tokenType = TokenType.Byte;
                break;
            case "short":
                tokenType = TokenType.Short;
                break;
            case "ushort":
                tokenType = TokenType.UShort;
                break;
            case "int":
                tokenType = TokenType.Int;
                break;
            case "uint":
                tokenType = TokenType.UInt;
                break;
            case "long":
                tokenType = TokenType.Long;
                break;
            case "ulong":
                tokenType = TokenType.ULong;
                break;
            case "float":
                tokenType = TokenType.Float;
                break;
            case "double":
                tokenType = TokenType.Double;
                break;
            case "void":
                tokenType = TokenType.Void;
                break;
            case "string":
                tokenType = TokenType.String;
                break;
            case "var":
                tokenType = TokenType.Var;
                break;
            default:
                tokenType = TokenType.Eof; // dummy value
                didMatch = false;
                break;
        }
        return didMatch;
    }
}
