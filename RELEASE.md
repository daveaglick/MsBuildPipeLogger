# 1.1.6

- Fix for unhandled interrupted system call in `PipeLoggerServer` (#8, #9, thanks @xoofx).

# 1.1.5

- Added SourceLink support.

# 1.1.4

- Fix to avoid calling `PipeStream.WaitForPipeDrain()` on non-Windows platforms where it's not supported (#3, #7, thanks @xoofx).
- Updated target of `MsBuildPipeLogger.Logger` and `MsBuildPipeLogger.Server` to .NET Standard 2.0 (#5, thanks @xoofx). 
- Fix for lack of async availability on Windows with anonymous pipes (#4, thanks @xoofx).
- Fix to detect `BuildFinishedEventArgs` and stop reading from the pipe when found (#2, #6, thanks @ltcmelo and @xoofx).

# 1.1.3

- [Fix] Fixed a race condition when fetching a log buffer (#1, thanks @duncanawoods)

# 1.1.2

- [Fix] No longer catches certain error exceptions that actually indicate a problem

# 1.1.1

- [Fix] Handles premature stream termination when reading events in the server

# 1.1.0

- [Feature] Support for server read cancellation with a `CancellationToken`
- [Refactoring] Added a `IPipeLoggerServer` interface
- [Feature] Added `IPipeLoggerServer.ReadAll()` to read all events in one call
- [Refactoring] Changes `IPipeLoggerServer.Read()` to return the event that was read instead of a `bool`

# 1.0.3

- [Refactoring] Made it easier to override the logger and use it as the basis for custom pipe loggers (I.e., filtering events)

# 1.0.2

- [Fix] No longer crashes when caller disposes the server

# 1.0.1

- [Refactoring] Greatly improved performance by using buffers and threads for pipe I/O

# 1.0.0

- Initial release