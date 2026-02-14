using System.CommandLine;
using System.CommandLine.Help;

namespace Seanet.Compiler;

public class ArgumentParser
{
    public static CliResult Parse(string[] args)
    {
        var rootCommand = new RootCommand("The SeaNet compiler.");
        
        // Remove default options to be more explicit and control aliases.
        rootCommand.Options.Clear();

        var helpOption = new HelpOption("--help", "-h");
        var versionOption = new VersionOption("--version", "-v");
        var inputOption = new Option<string>("--input-file", "-i")
        {
            Description = "The file to compile.",
            Required = true,
            Recursive = false,
            HelpName = "path",
        };
        var outputOption = new Option<string>("--output-file", "-o")
        {
            Description = "Path to the output file.",
            Required = true,
            Recursive = false,
            HelpName = "path"
        };
        var outputKindOption = new Option<bool>("--library", "-l")
        {
            Description = "Compile as a library. By default, an executable is created.",
            Required = false,
            Recursive = false,
            DefaultValueFactory = _ => false,
        };

        rootCommand.Options.Add(helpOption);
        rootCommand.Options.Add(versionOption);
        rootCommand.Options.Add(inputOption);
        rootCommand.Options.Add(outputOption);
        rootCommand.Options.Add(outputKindOption);

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            var errorMessages = parseResult.Errors.Select(x => x.ToString());
            return CliResult.Failure(errorMessages);
        }

        if (HasOption(args, helpOption) || HasOption(args, versionOption))
        {
            // Print help or version message.
            parseResult.Invoke();
            return CliResult.SuccessDone();
        }

        var inputFile = parseResult.GetValue<string>(inputOption.Name);
        var outputFile = parseResult.GetValue<string>(outputOption.Name);
        var isLibrary = parseResult.GetValue<bool>(outputKindOption.Name);

        var result = new CliResult()
        {
            InputFilePath = NormalizePath(inputFile!),
            OutputFilePath = NormalizePath(outputFile!),
            IsLibrary = isLibrary,
        };
        return result;
    }

    private static bool HasOption(string[] args, Option option)
    {
        return args.Any(x => x == option.Name || option.Aliases.Contains(x));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
    }
}

public readonly struct CliResult
{
    public CliResult()
    {

    }

    public bool IsDone { get; init; } = false;
    public bool DidFail { get; init; } = false;
    public IReadOnlyCollection<string> Errors { get; init; } = [];
    public string InputFilePath { get; init; } = string.Empty;
    public string OutputFilePath { get; init; } = string.Empty;
    public bool IsLibrary { get; init; } = false;

    public static CliResult SuccessDone()
    {
        return new()
        {
            IsDone = true,
        };
    }

    public static CliResult Failure(IEnumerable<string> errors)
    {
        return new()
        {
            DidFail = true,
            Errors = errors.ToList(),
        };
    }
}