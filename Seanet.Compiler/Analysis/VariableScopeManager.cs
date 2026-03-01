using Seanet.Compiler.Parsing;
using Seanet.Compiler.Scanning;

namespace Seanet.Compiler.Analysis;

/// <summary>
/// Supports variable and type identifier resolution by setting up nested scopes.
/// </summary>
public class ScopeManager
{
    private Scope currentScope = new();

    public void BeginScope()
    {
        currentScope = new()
        {
            Parent = currentScope,
        };
    }

    public void EndScope()
    {
        if (currentScope.Parent == null)
        {
            throw new InvalidOperationException("Unable to end scope. Already at the top level scope.");
        }
        currentScope = currentScope.Parent;
    }

    /// <summary>
    /// Declares a type in the current scope.
    /// </summary>
    public void Declare(Token identifier, Statement statement)
    {
        var key = identifier.Text().ToString();
        currentScope.Declare(key, statement);
    }

    /// <summary>
    /// Searches for a declaration and attaches it to the given expression.
    /// The current scope is searched first, then its parent scope and so on.
    /// </summary>
    public bool TryResolve(Token identifier, Expression expression)
    {
        var key = identifier.Text();
        if (!currentScope.TryFindDeclaration(key, out var declaration))
        {
            return false;
        }
        expression.Declaration = declaration;
        return true;
    }

    private class Scope
    {
        private readonly Dictionary<string, Statement> declarations = [];
        private readonly Dictionary<string, Statement>.AlternateLookup<ReadOnlySpan<char>> declarationsBySpan;
        public Scope? Parent { get; init; }

        public Scope()
        {
            declarationsBySpan = declarations.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public void Declare(string name, Statement declaration)
        {
            declarations[name] = declaration;
        }

        public bool TryFindDeclaration(ReadOnlySpan<char> key, out Statement declaration)
        {
            if (declarationsBySpan.TryGetValue(key, out var statement))
            {
                declaration = statement;
                return true;
            }
            else if (Parent != null && Parent.TryFindDeclaration(key, out var parentStatement))
            {
                declaration = parentStatement;
                return true;
            }
            else
            {
                declaration = null!;
                return false;
            }
        }
    }
}