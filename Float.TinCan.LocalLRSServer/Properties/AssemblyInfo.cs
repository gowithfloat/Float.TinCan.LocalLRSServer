using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

// allow testing of internal classes, only on debug builds
#if DEBUG
[assembly: InternalsVisibleTo("Float.TinCan.LocalLRSServer.Tests")]
#endif

[assembly: NeutralResourcesLanguage("en")]
[assembly: AssemblyVersion("0.0.1")]
