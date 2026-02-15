namespace Seanet.Compiler.Scanning;

public readonly struct Token
{
    public TokenType TokenType { get; init; }

    /// <summary>
    /// Start index of the token text within the source file.
    /// </summary>
    public int Start { get; init; }

    /// <summary>
    /// Length of the token text.
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Reference to the source file contents.
    /// </summary>
    public string Source { get; init; }

    /// <summary>
    /// Token text as a span to avoid copying.
    /// </summary>
    public ReadOnlySpan<char> Text() => Source.AsSpan(Start, Length);

    /// <summary>
    /// Line number in the source file.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number in the source file.
    /// </summary>
    public int Column { get; init; }
    
    /// <summary>
    /// Literal value, if applicable. Some literals are parsed during scanning,
    /// in which case the value is stored in the token for later usage.
    /// </summary>
    public object? Value { get; init; }
}