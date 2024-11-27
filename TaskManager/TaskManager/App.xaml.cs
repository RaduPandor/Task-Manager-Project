using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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
            serviceCollection.AddSingleton<PerformanceMetricsHelper>();
            serviceCollection.AddSingleton<MainWindowViewModel>();
            serviceCollection.AddSingleton<WindowCommands>();
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public static IServiceProvider ServiceProvider { get; private set; }
    }
}
