using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// This assembly is the default dynamic assembly generated Castle DynamicProxy, 
// used by Moq.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// Let the behavioral assembly be a friend
[assembly: InternalsVisibleTo("CleanMachine.Tests")]
[assembly: InternalsVisibleTo("CleanMachine.Behavioral")]
[assembly: InternalsVisibleTo("Activity")]
