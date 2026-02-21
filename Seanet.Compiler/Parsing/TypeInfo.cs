
using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Parsing;

/// <summary>
/// Wraps a type identifier with additional information to keep track of for example array types.
/// </summary>
public abstract class TypeInfo
{
}

public class SingleTokenTypeInfo : TypeInfo
{
    public required Token Type { get; init; }
}

public class ArrayTypeInfo : TypeInfo
{
    public required TypeInfo ItemType { get; init; }
}

public class FunctionTypeInfo : TypeInfo
{
    public required TypeInfo ReturnType { get; set; }
    public required List<TypeInfo> Parameters { get; init; }
}