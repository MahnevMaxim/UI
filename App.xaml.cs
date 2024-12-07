using System;
using System.IO;
using System.Windows;
using Core.App;
using Core.App.Helpers;
using Sentry;
using Serilog;
using Serilog.Events;
using SetUp.UI.Base;
using SetUp.UI.Model;
using SetUp.UI.ViewModel;
using SetUp.UI.Views;
using AppContext = Core.App.AppContext;

namespace SetUp
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitializeLogger();
            AppContext.Logger.Information("Setup run");
            MvvmFactory.CreateWindow(new Main(), new MainViewModel(), new MainWindow(), ViewMode.ShowDialog);
        }

        private void InitializeLogger()
        {
            _ = AppContext.Kernel.Bind<ILogger>().ToMethod(context => new LoggerConfiguration()
                .WriteTo.File(Path.Combine(PathHelper.ServiceLogsFolderPath, $"{DateTime.Now:dd-MM-yyyy}_{nameof(SetUp)}.log"))

                .Enrich.WithProperty(nameof(Consts.ProjectName), Consts.ProjectName)
                .Enrich.WithProperty(nameof(Consts.SourceName), Consts.SourceName)
                .Enrich.WithProperty(nameof(Consts.ReleaseVersion), Consts.ReleaseVersion)
                .Enrich.WithProperty(nameof(Consts.ReleaseType), Consts.ReleaseType)

                .WriteTo.Http(Consts.ElkUrl, null)

                .WriteTo.Sentry(o =>
                {
                    o.Dsn = Consts.SentryDsn;

                    o.SendDefaultPii = true;
                    o.StackTraceMode = StackTraceMode.Enhanced;

                    o.Environment = Consts.ReleaseType;
                    o.Release = Consts.ReleaseVersion;

                    o.MinimumBreadcrumbLevel = LogEventLevel.Warning;
                    o.MinimumEventLevel = LogEventLevel.Warning;

                    o.DefaultTags.Add(nameof(Consts.ProjectName), Consts.ProjectName);
                    o.DefaultTags.Add(nameof(Consts.SourceName), Consts.SourceName);
                    o.DefaultTags.Add(nameof(Consts.ReleaseVersion), Consts.ReleaseVersion);
                    o.DefaultTags.Add(nameof(Consts.ReleaseType), Consts.ReleaseType);
                })
                .CreateLogger()).InSingletonScope();
        }
    }
}
