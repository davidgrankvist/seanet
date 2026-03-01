namespace Seanet.Compiler.Errors;

public class ErrorReporter
{
    private List<string> errors = [];
    public IReadOnlyCollection<string> Errors => errors;
    public bool HasErrors() => errors.Count > 0;
    public void ReportErrorAtLocation(string prefix, string file, int line, int column, string message)
    {
        errors.Add($"{prefix} at {file}:{line},{column} - {message}");
    }
}

