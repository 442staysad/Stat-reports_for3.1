// В файле ApplicationDbContextFactory.cs в вашем проекте Infrastructure.Data

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System; // Добавьте using System; для InvalidOperationException

namespace Infrastructure.Data
{
    // Этот класс нужен только инструментам EF Core Design-Time (миграции, scaffold и т.п.)
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Создаем построитель конфигурации
            IConfigurationRoot configuration = new ConfigurationBuilder()
                // Указываем базовый путь для поиска appsettings.json
                // Обычно это папка стартового проекта, поэтому нужно либо запускать команду EF из нее,
                // либо явно указывать путь к стартовому проекту в команде EF.
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                // Можете добавить appsettings.Development.json или другие файлы, если они используются
                // .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .Build();

            // Получаем строку подключения из конфигурации
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Проверяем, найдена ли строка подключения
            if (string.IsNullOrEmpty(connectionString))
            {
                // Выбрасываем исключение с понятным сообщением, если строка подключения отсутствует
                throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена в файлах конфигурации (например, appsettings.json).");
            }


            // Создаем построитель опций для DbContext
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // *** ВАЖНО: УКАЖИТЕ ПРАВИЛЬНЫЙ ПРОВАЙДЕР БАЗЫ ДАННЫХ ***
            // Судя по вашей строке подключения, вы используете SQL Server.
            // Убедитесь, что у вас установлен NuGet пакет Microsoft.EntityFrameworkCore.SqlServer.
            builder.UseSqlServer(connectionString);

            // Создаем и возвращаем экземпляр вашего DbContext с настроенными опциями
            return new ApplicationDbContext(builder.Options);
        }
    }
}