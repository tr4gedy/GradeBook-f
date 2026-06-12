using GradeBook.Data;
using GradeBook.Services;
using GradeBook.ViewModels;
using GradeBook.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF;
using QuestPDF.Infrastructure;
using System;
using System.Windows;

namespace GradeBook
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // #region agent log
                DebugLog.Write("H1,H2", "App.xaml.cs:OnStartup", "startup entered", new { argsCount = e.Args.Length });
                // #endregion

                Settings.License = LicenseType.Community;

                var services = new ServiceCollection();
                var connectionString = "Host=localhost;Database=GradeBook;Username=postgres;Password=sa";

                services.AddDbContextFactory<ApplicationDbContext>(opt =>
                    opt.UseNpgsql(connectionString));

                services.AddTransient<IGradeService, GradeService>();
                services.AddSingleton<IPdfExporter, PdfExporter>();
                services.AddTransient<MainViewModel>();

                ServiceProvider = services.BuildServiceProvider();

                // Создание БД и таблиц
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    using var db = dbFactory.CreateDbContext();

                    // Проверяем подключение
                    var canConnect = db.Database.CanConnect();
                    // #region agent log
                    DebugLog.Write("H1,H2", "App.xaml.cs:OnStartup", "database connection checked", new { canConnect, provider = db.Database.ProviderName });
                    // #endregion

                    if (!canConnect)
                    {
                        MessageBox.Show(
                            $"Не удалось подключиться к БД.\n\n" +
                            "Проверьте:\n" +
                            "1. Запущен ли PostgreSQL\n" +
                            "2. Создана ли БД 'GradeBook'\n" +
                            "3. Верны ли логин/пароль",
                            "Ошибка подключения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown();
                        return;
                    }

                    // ✅ Создаёт БД и все таблицы (если их нет)
                    db.Database.EnsureCreated();
                    // #region agent log
                    DebugLog.Write("H2", "App.xaml.cs:OnStartup", "database ensure created completed", new { canConnect });
                    // #endregion
                }

                var window = new MainWindow();
                window.Show();
            }
            catch (Exception ex)
            {
                // #region agent log
                DebugLog.Write("H1,H2,H3", "App.xaml.cs:OnStartup", "startup exception", new { type = ex.GetType().Name, ex.Message, innerType = ex.InnerException?.GetType().Name, innerMessage = ex.InnerException?.Message });
                // #endregion

                MessageBox.Show(
                    $"Критическая ошибка:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка запуска",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}