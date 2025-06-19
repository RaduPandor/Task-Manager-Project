using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace TaskManager
{
    public partial class App : Application
    {
        public App()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<INativeMethodsService, NativeMethodsService>();
            serviceCollection.AddSingleton<IWindowCommands, WindowCommands>();
            serviceCollection.AddSingleton<IPerformanceMetricsService, PerformanceMetricsService>();
            serviceCollection.AddSingleton<IMainWindowViewModel, MainWindowViewModel>();
            serviceCollection.AddSingleton<IProcessProvider, ProcessProvider>();
            serviceCollection.AddSingleton<IServiceManager, ServiceManager>();
            serviceCollection.AddSingleton<IErrorDialogService, ErrorDialogService>();
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public static IServiceProvider ServiceProvider { get; private set; }
    }
}
