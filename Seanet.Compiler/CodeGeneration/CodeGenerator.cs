using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;

namespace Seanet.Compiler.CodeGeneration;

public class CodeGenerator
{
    public static void CompileApplication(string assemblyName, string inputFileName, string outputPath)
    {
        var implicitClassName = $"{assemblyName}.{inputFileName}";

        var assemblyNameObj = new AssemblyName(assemblyName);
        var assemblyBuilder = new PersistedAssemblyBuilder(assemblyNameObj, typeof(object).Assembly);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName + "Module");

        var attributeCtor = typeof(TargetFrameworkAttribute).GetConstructor([typeof(string)]);
        var attributeBuilder = new CustomAttributeBuilder(attributeCtor!, [".NETStandard,Version=v2.0"]);
        assemblyBuilder.SetCustomAttribute(attributeBuilder);
        var typeBuilder = moduleBuilder.DefineType(implicitClassName, TypeAttributes.Public | TypeAttributes.Class);

        var methodBuilder = typeBuilder.DefineMethod(
            "Main",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(int),
            [] //[typeof(string[])]
        );

        var il = methodBuilder.GetILGenerator();

        //il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);

        _ = typeBuilder.CreateType();

        var outputDir = Path.GetDirectoryName(outputPath)!;
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        /*
         * The EntryPoint property can't be modified, so as a workaround the generated IL is saved as a library
         * which is then invoked via a shim.
         */
        var dllName = assemblyName + ".dll";
        var dllOutputPath = Path.Combine(outputDir, dllName);
        assemblyBuilder.Save(dllOutputPath);

        SetupShim(outputPath, dllName);
    }

    private static void SetupShim(string outputPath, string dllName)
    {
        var outputDir = Path.GetDirectoryName(outputPath)!;

        // Configure the shim to run the generated DLL
        var shimSettingsName = "seanet.txt";
        var shimSettingsPath = Path.Combine(outputDir, shimSettingsName);
        File.WriteAllText(shimSettingsPath, dllName);

        // Copy the shim files and rename its executable to the provided output exe name.
        var compilerPath = Assembly.GetExecutingAssembly().Location;
        var compilerDir = Path.GetDirectoryName(compilerPath)!;
        var shimDir = Path.Combine(compilerDir, "Shims");
        var shimBaseName = "snc-shim";
        var shimDllName = $"{shimBaseName}.dll";
        var shimExeName = $"{shimBaseName}.exe";
        var shimRuntimeConfigName = $"{shimBaseName}.runtimeconfig.json";
        File.Copy(Path.Combine(shimDir, shimDllName), Path.Combine(outputDir, shimDllName), overwrite: true);
        File.Copy(Path.Combine(shimDir, shimRuntimeConfigName), Path.Combine(outputDir, shimRuntimeConfigName), overwrite: true);
        File.Copy(Path.Combine(shimDir, shimExeName), outputPath, overwrite: true);
    }

    public static void CompileLibrary(string assemblyName, string inputFileName, string outputPath)
    {
        var implicitClassName = $"{assemblyName}.{inputFileName}";

        var assemblyNameObj = new AssemblyName(assemblyName);
        var assemblyBuilder = new PersistedAssemblyBuilder(assemblyNameObj, typeof(object).Assembly);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName + "Module");

        var attributeCtor = typeof(TargetFrameworkAttribute).GetConstructor([typeof(string)]);
        var attributeBuilder = new CustomAttributeBuilder(attributeCtor!, [".NETStandard,Version=v2.0"]);
        assemblyBuilder.SetCustomAttribute(attributeBuilder);
        var typeBuilder = moduleBuilder.DefineType(implicitClassName, TypeAttributes.Public | TypeAttributes.Class);

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

        assemblyBuilder.Save(outputPath);
    }

    // private static void VerifyOutput(string outputPath)
    // {
    //     var asm = Assembly.LoadFile(outputPath);
    //     foreach (var t in asm.GetTypes())
    //     {
    //         Console.WriteLine(t.FullName);
    //     }

    //     var classInfo = asm.GetType("HelloWorld.Hello");
    //     var mainMethodInfo = classInfo?.GetMethod("Main");
    //     if (mainMethodInfo != null)
    //     {
    //         var result = mainMethodInfo.Invoke(null, []);//[new string[] {"some-args"}]);
    //         Console.WriteLine("The result is " + result);
    //     }
    // }
}