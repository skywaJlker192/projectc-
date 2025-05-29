using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WarehouseSystem.DataAccess;
using WarehouseSystem.Services;

namespace WarehouseSystem
{
    public class ConsoleInterface
    {
        private readonly WarehouseService _warehouseService;
        private readonly ReportService _reportService;
        private readonly ILogger<ConsoleInterface> _logger;

        public ConsoleInterface(WarehouseService warehouseService, ReportService reportService, ILogger<ConsoleInterface> logger)
        {
            _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("Запуск консольного интерфейса");
            while (true)
            {
                Console.WriteLine("=== Информационная система склада ===");
                Console.WriteLine("1. Добавить товар");
                Console.WriteLine("2. Переместить товар");
                Console.WriteLine("3. Провести инвентаризацию");
                Console.WriteLine("4. Сгенерировать отчет по остаткам");
                Console.WriteLine("5. Добавить зону хранения");
                Console.WriteLine("6. Сгенерировать отчет по зонам");
                Console.WriteLine("7. Выход");
                Console.Write("Выберите действие (1-7): ");

                string? choice = Console.ReadLine()?.Trim();
                _logger.LogDebug("Пользователь выбрал действие: Choice={Choice}", choice);

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await AddProductAsync();
                            break;
                        case "2":
                            await MoveProductAsync();
                            break;
                        case "3":
                            await PerformInventoryAsync();
                            break;
                        case "4":
                            await GenerateStockReportAsync();
                            break;
                        case "5":
                            await AddZoneAsync();
                            break;
                        case "6":
                            await GenerateZoneReportAsync();
                            break;
                        case "7":
                            _logger.LogInformation("Завершение работы консольного интерфейса");
                            Console.WriteLine("Выход...");
                            return;
                        default:
                            _logger.LogWarning("Некорректный выбор действия: Choice={Choice}", choice);
                            Console.WriteLine("Некорректная команда.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при выполнении действия: Choice={Choice}", choice);
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }

                Console.WriteLine("\nНажмите Enter для продолжения...");
                Console.ReadLine();
                Console.Clear();
            }
        }

        private async Task AddProductAsync()
        {
            _logger.LogInformation("Начало добавления товара через консоль");
            Console.Write("Название товара: ");
            string? name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("Название товара не введено");
                throw new ArgumentException("Название не может быть пустым.");
            }

            Console.Write("SKU: ");
            string? sku = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(sku))
            {
                _logger.LogWarning("SKU не введен");
                throw new ArgumentException("SKU не может быть пустым.");
            }

            Console.Write("Цена: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price) || price <= 0)
            {
                _logger.LogWarning("Некорректная цена: Price={Price}", price);
                throw new ArgumentException("Цена должна быть положительным числом.");
            }

            _logger.LogDebug("Введены данные товара: Name={Name}, SKU={Sku}, Price={Price}", name, sku, price);
            var productId = await _warehouseService.AddProductAsync(name, sku, price);
            if (productId.HasValue)
            {
                _logger.LogInformation("Товар добавлен через консоль: ProductId={ProductId}", productId);
                Console.WriteLine($"Товар добавлен, ID: {productId}");
            }
            else
            {
                _logger.LogError("Не удалось добавить товар через консоль: Name={Name}, SKU={Sku}", name, sku);
                Console.WriteLine("Не удалось добавить товар.");
            }
        }

        private async Task AddZoneAsync()
        {
            _logger.LogInformation("Начало добавления зоны через консоль");
            Console.Write("Название зоны (например, 'Холодильная камера 1'): ");
            string? name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("Название зоны не введено");
                throw new ArgumentException("Название зоны не может быть пустым.");
            }

            Console.Write("Вместимость зоны: ");
            if (!int.TryParse(Console.ReadLine(), out int capacity) || capacity <= 0)
            {
                _logger.LogWarning("Некорректная вместимость: Capacity={Capacity}", capacity);
                throw new ArgumentException("Вместимость должна быть положительным числом.");
            }

            Console.Write("ID склада: ");
            if (!int.TryParse(Console.ReadLine(), out int warehouseId) || warehouseId <= 0)
            {
                _logger.LogWarning("Некорректный ID склада: WarehouseId={WarehouseId}", warehouseId);
                throw new ArgumentException("ID склада должен быть положительным числом.");
            }

            _logger.LogDebug("Введены данные зоны: Name={Name}, Capacity={Capacity}, WarehouseId={WarehouseId}", name, capacity, warehouseId);
            var zoneId = await _warehouseService.AddZoneAsync(name, capacity, warehouseId);
            if (zoneId.HasValue)
            {
                _logger.LogInformation("Зона добавлена через консоль: ZoneId={ZoneId}", zoneId);
                Console.WriteLine($"Зона добавлена, ID: {zoneId}");
            }
            else
            {
                _logger.LogError("Не удалось добавить зону через консоль: Name={Name}, WarehouseId={WarehouseId}", name, warehouseId);
                Console.WriteLine("Не удалось добавить зону.");
            }
        }

        private async Task MoveProductAsync()
        {
            _logger.LogInformation("Начало перемещения товара через консоль");
            Console.Write("ID товара: ");
            if (!int.TryParse(Console.ReadLine(), out int productId) || productId <= 0)
            {
                _logger.LogWarning("Некорректный ID товара: ProductId={ProductId}", productId);
                throw new ArgumentException("ID товара должен быть положительным числом.");
            }

            Console.Write("ID исходного склада: ");
            if (!int.TryParse(Console.ReadLine(), out int fromWarehouseId) || fromWarehouseId <= 0)
            {
                _logger.LogWarning("Некорректный ID исходного склада: FromWarehouseId={FromWarehouseId}", fromWarehouseId);
                throw new ArgumentException("ID склада должен быть положительным числом.");
            }

            Console.Write("ID исходной зоны (или 0, если не используется): ");
            int? fromZoneId = null;
            if (int.TryParse(Console.ReadLine(), out int fromZoneInput) && fromZoneInput > 0)
            {
                fromZoneId = fromZoneInput;
            }

            Console.Write("ID целевого склада: ");
            if (!int.TryParse(Console.ReadLine(), out int toWarehouseId) || toWarehouseId <= 0)
            {
                _logger.LogWarning("Некорректный ID целевого склада: ToWarehouseId={ToWarehouseId}", toWarehouseId);
                throw new ArgumentException("ID склада должен быть положительным числом.");
            }

            Console.Write("ID целевой зоны (или 0, если не используется): ");
            int? toZoneId = null;
            if (int.TryParse(Console.ReadLine(), out int toZoneInput) && toZoneInput > 0)
            {
                toZoneId = toZoneInput;
            }

            Console.Write("Количество: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity <= 0)
            {
                _logger.LogWarning("Некорректное количество: Quantity={Quantity}", quantity);
                throw new ArgumentException("Количество должно быть положительным числом.");
            }

            _logger.LogDebug("Введены данные для перемещения: ProductId={ProductId}, FromWarehouseId={FromWarehouseId}, FromZoneId={FromZoneId}, ToWarehouseId={ToWarehouseId}, ToZoneId={ToZoneId}, Quantity={Quantity}", productId, fromWarehouseId, fromZoneId, toWarehouseId, toZoneId, quantity);
            bool success = await _warehouseService.MoveProductAsync(productId, fromWarehouseId, fromZoneId, toWarehouseId, toZoneId, quantity);
            if (success)
            {
                _logger.LogInformation("Перемещение завершено: ProductId={ProductId}, FromWarehouseId={FromWarehouseId}, ToWarehouseId={ToWarehouseId}", productId, fromWarehouseId, toWarehouseId);
                Console.WriteLine("Товар перемещен.");
            }
            else
            {
                _logger.LogError("Не удалось переместить товар: ProductId={ProductId}, FromWarehouseId={FromWarehouseId}, ToWarehouseId={ToWarehouseId}", productId, fromWarehouseId, toWarehouseId);
                Console.WriteLine("Не удалось переместить товар.");
            }
        }

        private async Task PerformInventoryAsync()
        {
            _logger.LogInformation("Начало инвентаризации через консоль");
            Console.Write("ID склада: ");
            if (!int.TryParse(Console.ReadLine(), out int warehouseId) || warehouseId <= 0)
            {
                _logger.LogWarning("Некорректный ID склада: WarehouseId={WarehouseId}", warehouseId);
                throw new ArgumentException("ID склада должен быть положительным числом.");
            }

            var actualQuantities = new Dictionary<int, int>();
            while (true)
            {
                Console.Write("ID товара (0 для завершения): ");
                if (!int.TryParse(Console.ReadLine(), out int productId))
                {
                    _logger.LogWarning("Некорректный ID товара: ProductId={ProductId}", productId);
                    throw new ArgumentException("ID товара должен быть числом.");
                }
                if (productId == 0)
                {
                    _logger.LogDebug("Завершение ввода товаров для инвентаризации");
                    break;
                }

                Console.Write("Фактическое количество: ");
                if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity < 0)
                {
                    _logger.LogWarning("Некорректное количество: Quantity={Quantity}", quantity);
                    throw new ArgumentException("Количество должно быть неотрицательным числом.");
                }

                actualQuantities[productId] = quantity;
                _logger.LogDebug("Добавлен товар для инвентаризации: ProductId={ProductId}, Quantity={Quantity}", productId, quantity);
            }

            bool success = await _warehouseService.PerformInventoryAsync(warehouseId, actualQuantities);
            if (success)
            {
                _logger.LogInformation("Инвентаризация завершена через консоль: WarehouseId={WarehouseId}", warehouseId);
                Console.WriteLine("Инвентаризация завершена.");
            }
            else
            {
                _logger.LogError("Ошибка инвентаризации: WarehouseId={WarehouseId}", warehouseId);
                Console.WriteLine("Ошибка инвентаризации.");
            }
        }

        private async Task GenerateStockReportAsync()
        {
            _logger.LogInformation("Начало генерации отчета через консоль");
            Console.Write("ID склада: ");
            if (!int.TryParse(Console.ReadLine(), out int warehouseId) || warehouseId <= 0)
            {
                _logger.LogWarning("Некорректный ID склада: WarehouseId={WarehouseId}", warehouseId);
                throw new ArgumentException("ID склада должен быть положительным числом.");
            }

            _logger.LogDebug("Введен ID склада для отчета: WarehouseId={WarehouseId}", warehouseId);
            string report = await _reportService.GenerateStockReportAsync(warehouseId);
            _logger.LogInformation("Отчет сгенерирован через консоль: WarehouseId={WarehouseId}", warehouseId);
            Console.WriteLine("\n" + report);
        }

        private async Task GenerateZoneReportAsync()
        {
            _logger.LogInformation("Начало генерации отчета по зонам через консоль");
            Console.Write("ID склада: ");
            if (!int.TryParse(Console.ReadLine(), out int warehouseId) || warehouseId <= 0)
            {
                _logger.LogWarning("Некорректный ID склада: WarehouseId={WarehouseId}", warehouseId);
                throw new ArgumentException("ID склада должен быть положительным числом.");
            }

            _logger.LogDebug("Введен ID склада для отчета по зонам: WarehouseId={WarehouseId}", warehouseId);
            string report = await _reportService.GenerateZoneReportAsync(warehouseId);
            _logger.LogInformation("Отчет по зонам сгенерирован через консоль: WarehouseId={WarehouseId}", warehouseId);
            Console.WriteLine("\n" + report);
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddDbContext<WarehouseDbContext>(options =>
                options.UseSqlite("Data Source=warehouse.db"));
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<DatabaseManager>();
            services.AddScoped<WarehouseService>();
            services.AddScoped<ReportService>();
            services.AddScoped<ConsoleInterface>();

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Program>>();
            logger?.LogInformation("Инициализация приложения");

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<WarehouseDbContext>();
                if (dbContext == null)
                {
                    logger?.LogError("Не удалось инициализировать контекст базы данных");
                    Console.WriteLine("Ошибка: не удалось инициализировать контекст базы данных.");
                    return;
                }
                await dbContext.Database.EnsureCreatedAsync();
                logger?.LogInformation("База данных успешно инициализирована");
            }

            var consoleInterface = serviceProvider.GetService<ConsoleInterface>();
            if (consoleInterface == null)
            {
                logger?.LogError("Не удалось инициализировать консольный интерфейс");
                Console.WriteLine("Ошибка: не удалось инициализировать консольный интерфейс.");
                return;
            }

            await consoleInterface.RunAsync();
            logger?.LogInformation("Приложение завершило работу");
        }
    }
}