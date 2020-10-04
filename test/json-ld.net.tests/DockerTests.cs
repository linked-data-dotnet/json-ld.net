using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace JsonLD.Test
{
    public class DockerTests
    {
        private readonly ITestOutputHelper output;
        
        public DockerTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [SkippableFact]
        public void DockerBuild()
        {
            try
            {
                var exit = ExecDocker("build -t json-ld.net .");
                Assert.Equal(0, exit);
            }
            catch (Win32Exception ex) when (ex.Message == "The system cannot find the file specified")
            {
                Skip.If(true, "Couldn't find docker in your path.\nThis is only required for building/testing inside a docker container.");
            }
        }

        [SkippableFact]
        public void DockerTest()
        {
            try
            {
                // --filter option required to avoid going full inception and
                // testing a container inside a container inside a conta...
                var exit = ExecDocker($"run --rm json-ld.net dotnet test --filter FullyQualifiedName!~{nameof(DockerTests)}");
                Assert.Equal(0, exit);
            }
            catch (Win32Exception ex) when (ex.Message == "The system cannot find the file specified")
            {
                Skip.If(true, "Couldn't find docker in your path.\nThis is only required for building/testing inside a docker container.");
            }
        }

        private int ExecDocker(string args)
        {
            var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            if (string.IsNullOrEmpty(workspace))
            {
                // Current directory for test context is usually REPO_HOME/test/json-ld.net.tests/bin/Release/netcoreapp2.1,
                // and we want just REPO_HOME.
                workspace = Path.Combine(Directory.GetCurrentDirectory(), "../../../../..");
            }

            var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workspace,
                FileName = "docker",
                Arguments = args
            });

            process.WaitForExit();

            output.WriteLine("=======STDOUT=======");
            output.WriteLine(process.StandardOutput.ReadToEnd());
            output.WriteLine("\n\n\n\n=======STDERR=======");
            output.WriteLine(process.StandardError.ReadToEnd());

            return process.ExitCode;
        }
    }
}
