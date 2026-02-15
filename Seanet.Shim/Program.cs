using System.Reflection;

class Program
{
    static int Main(string[] args)
    {
        string settingsFileName = "seanet.txt";

        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath)!;
        var settingsPath = Path.Combine(assemblyDir, settingsFileName);

        if (!File.Exists(settingsPath))
        {
            Console.Error.WriteLine($"Failed to run shim. Couldn't find {settingsPath}");
            return 1;
        }

        var programAssemblyName = File.ReadAllText(settingsPath).Trim();
        var programAssemblyPath = Path.Combine(assemblyDir, programAssemblyName);
        if (!File.Exists(programAssemblyPath))
        {
            Console.Error.WriteLine($"Failed to run shim. Couldn't find {programAssemblyPath}");
            return 1;
        }

        var asm = Assembly.LoadFile(programAssemblyPath);
        var entryPointClasses = asm.GetTypes()
            .Where(x => x.IsClass && !x.IsAbstract && x.GetMethod("Main") != null)
            .ToList();
        
        if (entryPointClasses.Count == 0)
        {
            Console.Error.WriteLine($"Failed to run {programAssemblyName}. Unable to find entrypoint.");
            return 1;
        }
        else if (entryPointClasses.Count > 1)
        {
            Console.Error.WriteLine($"Failed to run {programAssemblyName}. Multiple entrypoints found. This is currently not supported.");
            return 1;
        }

        var entryPointClass = entryPointClasses[0];
        var entryPoint = entryPointClass.GetMethod("Main")!;
        ParameterInfo[] parameters = entryPoint.GetParameters() ?? [];

        if (entryPoint.ReturnType == typeof(int) && parameters.Length > 0)
        {
            var result = entryPoint.Invoke(null, [args]);
#pragma warning disable CS8605 // Unboxing a possibly null value.
            return (int)result;
#pragma warning restore CS8605 // Unboxing a possibly null value.
        }
        else
        {
            Console.Error.WriteLine($"Failed to run {programAssemblyName}. Main must have the signature int Main(string[] args).");
            return 1;
        }
    }

}