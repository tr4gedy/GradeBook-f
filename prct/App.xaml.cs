using System;
using System.Windows;
using GradeBook.Data;
using GradeBook.Services;
using GradeBook.ViewModels;
using GradeBook.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GradeBook
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Автоматическое создание БД и таблиц PostgreSQL при старте, если их нет
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    using var db = dbFactory.CreateDbContext();
                    db.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к PostgreSQL:\n{ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Корректно запрашиваем главное окно из DI контейнера и открываем его
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Строка подключения к PostgreSQL
            string connectionString = "Host=localhost;Port=5432;Database=GradeBook;Username=postgres;Password=sa";

            // Регистрация фабрики DbContext для потокобезопасной работы сервисов в WPF (через Npgsql)
            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Регистрация бизнес-логики (БД и PDF)
            services.AddSingleton<IGradeService, GradeService>();
            services.AddSingleton<IPdfExporter, PdfExporter>();

            // Регистрация MVVM компонентов слоя отображения
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }
    }
}