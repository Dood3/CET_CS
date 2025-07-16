// sudo apt update
// sudo apt install -y wget apt-transport-https
// wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
// sudo dpkg -i packages-microsoft-prod.deb
// sudo apt update
// sudo apt install -y dotnet-sdk-8.0
// dotnet --list-sdks


using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CommandEmbedder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Command Embedder Tool (C#) ===");
            Console.WriteLine("Generate standalone executables that run shell commands\n");

            // Get target OS
            string targetOS = GetTargetOS();

            // Get command to embed
            Console.Write("Enter the shell command to embed: ");
            string command = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine("Error: Command cannot be empty!");
                return;
            }

            // Get output filename
            Console.Write("Enter output filename (without extension): ");
            string filename = Console.ReadLine() ?? "embedded_command";

            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = "embedded_command";
            }

            // Create output directory
            string outputDir = "output";
            Directory.CreateDirectory(outputDir);

            // Generate source code
            string sourceCode = GenerateSourceCode(command, targetOS);
            string sourceFile = Path.Combine(outputDir, $"{filename}.cs");

            Console.WriteLine($"\nGenerating source code: {sourceFile}");
            File.WriteAllText(sourceFile, sourceCode);

            // Create project file
            string projectFile = Path.Combine(outputDir, $"{filename}.csproj");
            string projectContent = GenerateProjectFile();
            File.WriteAllText(projectFile, projectContent);

            // Compile the executable
            Console.WriteLine("Compiling executable...");
            bool success = CompileExecutable(outputDir, filename, targetOS);

            if (success)
            {
                Console.WriteLine($"\n✅ Success!");
                Console.WriteLine($"Source file: {sourceFile}");
                Console.WriteLine($"Executable: {Path.Combine(outputDir, "bin", "Release", GetTargetFramework(), GetRuntimeIdentifier(targetOS), "publish", GetExecutableName(filename, targetOS))}");
                Console.WriteLine($"\n⚠️  Security Warning: The generated executable will run the embedded command when executed.");
                Console.WriteLine("Only use on systems you own or have explicit permission to test.");
            }
            else
            {
                Console.WriteLine("\n❌ Compilation failed!");
            }
        }

        static string GetTargetOS()
        {
            while (true)
            {
                Console.WriteLine("Select target operating system:");
                Console.WriteLine("1. Linux");
                Console.WriteLine("2. Windows");
                Console.Write("Enter choice (1-2): ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        return "linux";
                    case "2":
                        return "windows";
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1 or 2.\n");
                        break;
                }
            }
        }

        static string GenerateSourceCode(string command, string targetOS)
        {
            string shellCommand = targetOS == "windows" ? "cmd" : "sh";
            string shellArgs = targetOS == "windows" ? "/C" : "-c";

            return $@"using System;
using System.Diagnostics;

namespace EmbeddedCommand
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            try
            {{
                // Embedded command: {command}
                var processInfo = new ProcessStartInfo
                {{
                    FileName = ""{shellCommand}"",
                    Arguments = ""{shellArgs} {command.Replace("\"", "\\\"")}"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }};

                using (var process = Process.Start(processInfo))
                {{
                    if (process != null)
                    {{
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        
                        process.WaitForExit();
                        
                        if (!string.IsNullOrEmpty(output))
                        {{
                            Console.WriteLine(output);
                        }}
                        
                        if (!string.IsNullOrEmpty(error))
                        {{
                            Console.Error.WriteLine(error);
                        }}
                        
                        Environment.Exit(process.ExitCode);
                    }}
                    else
                    {{
                        Console.Error.WriteLine(""Failed to start process"");
                        Environment.Exit(1);
                    }}
                }}
            }}
            catch (Exception ex)
            {{
                Console.Error.WriteLine($""Error executing command: {{ex.Message}}"");
                Environment.Exit(1);
            }}
        }}
    }}
}}";
        }
        static string GenerateProjectFile()
        {
            return @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
  </PropertyGroup>
</Project>";
        }

        static bool CompileExecutable(string outputDir, string filename, string targetOS)
        {
            try
            {
                string runtimeId = GetRuntimeIdentifier(targetOS);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish -c Release -r {runtimeId} --self-contained true",
                    WorkingDirectory = outputDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            Console.WriteLine($"Compilation output: {output}");
                            Console.WriteLine($"Compilation error: {error}");
                            return false;
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during compilation: {ex.Message}");
            }

            return false;
        }

        static string GetRuntimeIdentifier(string targetOS)
        {
            return targetOS == "windows" ? "win-x64" : "linux-x64";
        }

        static string GetTargetFramework()
        {
            return "net8.0";
        }

        static string GetExecutableName(string filename, string targetOS)
        {
            return targetOS == "windows" ? $"{filename}.exe" : filename;
        }
    }
}