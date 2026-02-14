using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;

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

        var assemblyName = Path.GetFileNameWithoutExtension(cliResult.InputFilePath);
        Compile(assemblyName, cliResult.OutputFilePath);

        VerifyOutput(cliResult.OutputFilePath);
        return 0;
    }

    private static void Compile(string assemblyName, string outputPath)
    {
        var assemblyNameObj = new AssemblyName(assemblyName);
        var asmBuilder = new PersistedAssemblyBuilder(assemblyNameObj, typeof(object).Assembly);
        var modBuilder = asmBuilder.DefineDynamicModule("MainModule");

        var attributeCtor = typeof(TargetFrameworkAttribute).GetConstructor([typeof(string)]);
        var attributeBuilder = new CustomAttributeBuilder(attributeCtor!, [".NETStandard,Version=v2.0"]);
        asmBuilder.SetCustomAttribute(attributeBuilder);

        var typeBuilder = modBuilder.DefineType("Samples.Stuff", TypeAttributes.Public | TypeAttributes.Class);

        var methodBuilder = typeBuilder.DefineMethod(
            "Add",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(int),
            [typeof(int), typeof(int)]
        );

        var il = methodBuilder.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        _ = typeBuilder.CreateType();

        var outputDir = Path.GetDirectoryName(outputPath)!;
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        asmBuilder.Save(outputPath);
    }

    private static void VerifyOutput(string outputPath)
    {
        var asm = Assembly.LoadFile(outputPath);
        foreach (var t in asm.GetTypes())
        {
            Console.WriteLine(t.FullName);
        }
    }
}
