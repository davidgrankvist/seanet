using Seanet.Compiler.CodeGeneration;
using Seanet.Compiler.Errors;
using Seanet.Compiler.Parsing;
using Seanet.Compiler.Scanning;

namespace Seanet.Compiler;

class Program
{
    static int Main(string[] args)
    {
        var cliResult = ArgumentParser.Parse(args);

        if (cliResult.DidFail)
        {
            foreach (var msg in cliResult.Errors)
            {
                Console.Error.WriteLine(msg);
            }

            return 1;
        }

        // Early exit for commands like --help
        if (cliResult.IsDone)
        {
            return 0;
        }

        if (!File.Exists(cliResult.InputFilePath))
        {
            Console.Error.WriteLine($"Failed to compile. Couldn't find the file {cliResult.InputFilePath}");
            return 1;
        }

        var errorReporter = new ErrorReporter();
        CompileProgram(cliResult, errorReporter);

        if (errorReporter.HasErrors())
        {
            foreach (var error in errorReporter.Errors)
            {
                Console.Error.WriteLine(error);
            }
            return 1;
        }

        return 0;
    }

    private static void CompileProgram(CliResult cliResult, ErrorReporter errorReporter)
    {
        // Scanning
        var scanner = new Scanner(errorReporter);
        var source = File.ReadAllText(cliResult.InputFilePath);
        var tokens = scanner.Scan(cliResult.InputFilePath, source);
        if (errorReporter.HasErrors())
        {
            return;
        }

        // Parsing
        var parser = new Parser(errorReporter);
        var syntaxTree = parser.Parse(tokens);
        if (errorReporter.HasErrors())
        {
            return;
        }

        // Code generation (dummy implementation for now)
        var assemblyName = Path.GetFileNameWithoutExtension(cliResult.OutputFilePath);
        var fileName = Path.GetFileNameWithoutExtension(cliResult.InputFilePath);
        if (cliResult.IsLibrary)
        {
            CodeGenerator.CompileLibrary(assemblyName, fileName, cliResult.OutputFilePath);
        }
        else
        {
            CodeGenerator.CompileApplication(assemblyName, fileName, cliResult.OutputFilePath);
        }
    }
}
