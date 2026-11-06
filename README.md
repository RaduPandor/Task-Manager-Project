# Task Manager

A WPF desktop app inspired by the Windows 11 Task Manager. It shows running processes, performance graphs, services, startup apps and users, and lets you end processes and start or stop services.

<!-- Add a screenshot here, e.g. ![Performance tab](docs/performance.png) -->

## Features

- **Processes**: running processes with resource usage, and End Task
- **Performance**: live charts for CPU, memory, disk, network and GPU
- **App history**, **Startup apps** and **Users** tabs
- **Details**: per-process info, including the user that owns each process
- **Services**: list Windows services and start, stop or restart them

## Tech stack

C#, .NET 6, WPF, XAML, MVVM, Microsoft.Extensions.DependencyInjection, LiveCharts, System.Management (WMI), performance counters, LibreHardwareMonitor, xUnit, MSTest, Moq, Reqnroll (Gherkin)

## How it's built

- **MVVM**: each tab is a View with its own ViewModel. Views hold no logic. `RelayCommand` handles commands such as End Task.
- **Dependency injection**: services (`IProcessProvider`, `IServiceManager`, `IPerformanceMetricsService`, `INativeMethodsService`, `IErrorDialogService`) are registered in `App.xaml.cs` and injected through interfaces. That's what makes the ViewModels testable with Moq.
- **Async loading with cancellation**: tabs implement `ILoadableViewModel`. When you switch tabs, the old one is cancelled through a linked `CancellationTokenSource`, so tabs you aren't looking at don't keep polling.
- **Windows APIs**: process owners are resolved through P/Invoke (`OpenProcess`, `OpenProcessToken`, `GetTokenInformation`, `LookupAccountSid`). GPU info comes from WMI (`Win32_VideoController`) and performance counters.
- **Code quality**: StyleCop, SonarAnalyzer, Roslynator and .NET analyzers run on build.

## Tests

- `Tests/`: unit tests for the ViewModels and services, using xUnit, MSTest and Moq
- `AutomationTests/`: UI automation tests written as Gherkin feature files with Reqnroll. They launch the app and drive it through Windows UI Automation (for example, checking that reported CPU usage is between 0 and 100).

## Running it

Requires Windows and the .NET 6 SDK (or Visual Studio 2022).

```bash
git clone https://github.com/RaduPandor/Task-Manager-Project.git
cd Task-Manager-Project/TaskManager
dotnet run --project TaskManager
```

Run the tests:

```bash
dotnet test Tests
```

Some features, like stopping services or seeing other users' processes, need the app to run as administrator.
