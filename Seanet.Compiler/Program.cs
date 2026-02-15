using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;

using Seanet.Compiler.CodeGeneration;

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

        if (cliResult.IsDone)
        {
            return 0;
        }

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

        return 0;
    }
}
