A logger for MSBuild that sends event data over anonymous or named pipes.

**NuGet**
* [MsBuildPipeLogger.Logger](https://www.nuget.org/packages/MsBuildPipeLogger.Logger/)
* [MsBuildPipeLogger.Server](https://www.nuget.org/packages/MsBuildPipeLogger.Server/)

**MyGet**
* [MsBuildPipeLogger.Logger](https://www.myget.org/feed/msbuildpipelogger/package/nuget/MsBuildPipeLogger.Logger)
* [MsBuildPipeLogger.Server](https://www.myget.org/feed/msbuildpipelogger/package/nuget/MsBuildPipeLogger.Server)

**GitHub**
* [MsBuildPipeLogger](https://github.com/daveaglick/MsBuildPipeLogger)

**Donations**

<a href="https://www.buymeacoffee.com/daveaglick"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

---

## Say what?

As a general purpose build tool, MSBuild is really powerful. However, it can be hard to figure out what's going on under the hood. Thankfully, MSBuild provides a nice logging API that consists of sending a sequence of events to one or more attached loggers. This allows the logger to track (almost) everything that happens during the build.

This project is heavily based on the amazing work done by @KirillOsenkov on [MSBuildStructuredLog](https://github.com/KirillOsenkov/MSBuildStructuredLog), which is a custom logger (and log viewer application) that serializes every logging event to disk for post-build viewing, analysis, and playback. While that logger (which is now built into MSBuild itself by the way) serializes logging events to disk, this logger serializes them across either a named or anonymous pipe to a server that receives the serialized logging events, deserializes them, and issues callbacks using the MSBuild logging API for each event received. It uses the same serialization format as [MSBuildStructuredLog](https://github.com/KirillOsenkov/MSBuildStructuredLog) and lets you write code that responds to MSBuild logging events in much the same way you would if you were writing a custom logger, except your code can respond to events from another process or even another system.

## Why would I want to do that?

I wrote this to address a need I had in [Buildalyzer](https://github.com/daveaglick/Buildalyzer) to access MSBuild properties from out-of-process MSBuild instances. In that use case, you can use `MsBuildPipeLogger` with a forked MSBuild instance to send logging events back to your original process where they can be operated on. I'm sure there are other use cases such as remote log aggregation. Drop me a line if you find this logger helpful and let me know what you're up to.

## How do I use it?

There are two libraries:
* [MsBuildPipeLogger.Logger](https://www.nuget.org/packages/MsBuildPipeLogger.Logger/) - the logger that's given to MSBuild.
* [MsBuildPipeLogger.Server](https://www.nuget.org/packages/MsBuildPipeLogger.Server/) - the server that receives logging events from the pipe and raises them to your code.

Usage consists of creating a server to receive logging events and then telling MSBuild to use the `MsBuildPipeLogger`. It's slightly different depending on if you want to use an [anonymous pipe](https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-anonymous-pipes-for-local-interprocess-communication) or a [named pipe](https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication).

### Anonymous pipe

```csharp
// Create the server
AnonymousPipeLoggerServer server = new AnonymousPipeLoggerServer();

// Get the pipe handle
string pipeHandle = server.GetClientHandle();

// Register an event handler
server.AnyEventRaised += (s, e) => Console.WriteLine(e.Message);

// Run the MSBuild process, passing it the logger and pipe handle
// Note you can also call MSBuild straight from the CLI, you just need to know the pipe handle to pass it to the logger
Process process = new Process();
process.StartInfo.FileName = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin";
process.StartInfo.Arguments = $"/noconlog /logger:MsBuildPipeLogger,\"C:\Path\To\MsBuildPipeLogger.Logger.dll\";{pipeHandle}";
// ...other process settings like working directory
process.Start();

// Wait for the process to exit
while (!process.HasExited)
{
    // Read a single logger event (which will trigger the handler above)
    server.Read();
}
process.Close();
```

### Named pipe

```csharp
// Create the server with a pipe name
NamedPipeLoggerServer server = new NamedPipeLoggerServer("Mario");

// Register an event handler
server.AnyEventRaised += (s, e) => Console.WriteLine(e.Message);

// Run the MSBuild process, passing it the logger, pipe name, and optionally the server
// Note you can also call MSBuild straight from the CLI, you just need to know the pipe handle to pass it to the logger
Process process = new Process();
process.StartInfo.FileName = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin";
process.StartInfo.Arguments = $"/noconlog /logger:MsBuildPipeLogger.Logger,\"C:\Path\To\MsBuildPipeLogger.Logger.dll\";name=Mario;server=MyServerName";
// ...other process settings like working directory
process.Start();

// Wait for the process to exit
while (!process.HasExited)
{
    // Read a single logger event (which will trigger the handler above)
    server.Read();
}
process.Close();
```

### Logger parameters

[The syntax](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference) for specifying a logger to MSBuild is:

```
[LoggerClass,]LoggerAssembly[;LoggerParameters]
```

The `MsBuildPipeLogger.Logger` recognizes these parameters, separated by a `;` and distinguished as a name and value by `=`:
* A single string is interpreted as an anonymous pipe handle: `MsBuildPipeLogger.Logger,MsBuildPipeLogger.Logger.dll;1234`
* `handle` indicates the anonymous pipe handle to connect to: `MsBuildPipeLogger.Logger,MsBuildPipeLogger.Logger.dll;handle=1234`
* `name` indicates the named pipe name: `MsBuildPipeLogger.Logger,MsBuildPipeLogger.Logger.dll;name=Mario`
* `server` indicates the named pipe server (assumed to be a local pipe if omitted): `MsBuildPipeLogger.Logger,MsBuildPipeLogger.Logger.dll;name=Mario;server=MyServerName`

### A note on concurrency

The `AnonymousPipeLoggerServer.Read()` and `NamedPipeLoggerServer.Read()` methods both block while waiting for events. If you need to support concurrency or cancellation, pass a `CancellationToken` to the server constructors and then cancel it during read operations.

