using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WarehouseSystem.DataAccess;
using WarehouseSystem.Models;

namespace WarehouseSystem.Services
{
    public abstract class BaseService
    {
        protected readonly DatabaseManager _dbManager;
        protected readonly ILogger _logger;

        protected BaseService(DatabaseManager dbManager, ILogger logger)
        {
            _dbManager = dbManager ?? throw new ArgumentNullException(nameof(dbManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected virtual void ValidateInput<T>(T input, string paramName)
        {
            if (input == null || (input is string str && string.IsNullOrEmpty(str)))
            {
                _logger.LogWarning("������������ ����: �������� {ParamName} ������", paramName);
                throw new ArgumentException($"{paramName} �� ����� ���� ������.");
            }
        }
    }

    public class WarehouseService : BaseService
    {
        public WarehouseService(DatabaseManager dbManager, ILogger<WarehouseService> logger)
            : base(dbManager, logger) { }

        public async Task<int?> AddZoneAsync(string name, int capacity, int warehouseId)
        {
            _logger.LogInformation("������ ���������� ����: Name={Name}, Capacity={Capacity}, WarehouseId={WarehouseId}", name, capacity, warehouseId);
            ValidateInput(name, nameof(name));
            if (capacity <= 0)
            {
                _logger.LogWarning("����������� ������ ���� �������������: Capacity={Capacity}", capacity);
                throw new ArgumentException("����������� ������ ���� �������������.", nameof(capacity));
            }

            var zoneId = await _dbManager.CreateZoneAsync(name, capacity, warehouseId);
            if (zoneId.HasValue)
            {
                _logger.LogInformation("���� ������� ���������: ZoneId={ZoneId}", zoneId);
            }
            else
            {
                _logger.LogError("�� ������� �������� ����: Name={Name}, WarehouseId={WarehouseId}", name, warehouseId);
            }
            return zoneId;
        }

        public async Task<int?> AddProductAsync(string name, string sku, decimal price)
        {
            _logger.LogInformation("������ ���������� ������: Name={Name}, SKU={Sku}, Price={Price}", name, sku, price);
            ValidateInput(name, nameof(name));
            ValidateInput(sku, nameof(sku));
            if (price <= 0)
            {
                _logger.LogWarning("���� ������ ���� �������������: Price={Price}", price);
                throw new ArgumentException("���� ������ ���� �������������.", nameof(price));
            }

            var productId = await _dbManager.CreateProductAsync(name, sku, price);
            if (productId.HasValue)
            {
                _logger.LogInformation("����� ������� ��������: ProductId={ProductId}", productId);
            }
            else
            {
                _logger.LogError("�� ������� �������� �����: Name={Name}, SKU={Sku}", name, sku);
            }
            return productId;
        }

        public async Task<bool> MoveProductAsync(int productId, int fromWarehouseId, int? fromZoneId, int toWarehouseId, int? toZoneId, int quantity)
        {
            _logger.LogInformation("������ ����������� ������: ProductId={ProductId}, FromWarehouseId={FromWarehouseId}, FromZoneId={FromZoneId}, ToWarehouseId={ToWarehouseId}, ToZoneId={ToZoneId}, Quantity={Quantity}", productId, fromWarehouseId, fromZoneId, toWarehouseId, toZoneId, quantity);
            if (quantity <= 0)
            {
                _logger.LogWarning("���������� ������ ���� �������������: Quantity={Quantity}", quantity);
                throw new ArgumentException("���������� ������ ���� �������������.", nameof(quantity));
            }

            var fromInventory = await _dbManager.GetInventoryAsync(productId, fromWarehouseId, fromZoneId);
            if (fromInventory == null || fromInventory.Quantity < quantity)
            {
                _logger.LogWarning("������������ ������: ProductId={ProductId}, FromWarehouseId={FromWarehouseId}, FromZoneId={FromZoneId}, AvailableQuantity={AvailableQuantity}, RequestedQuantity={RequestedQuantity}", productId, fromWarehouseId, fromZoneId, fromInventory?.Quantity ?? 0, quantity);
                return false;
            }

            var updated = await _dbManager.UpdateInventoryAsync(fromInventory.InventoryId, fromInventory.Quantity - quantity);
            if (!updated)
            {
                _logger.LogError("�� ������� �������� ������� �� �������� ������: InventoryId={InventoryId}", fromInventory.InventoryId);
                return false;
            }

            var toInventory = await _dbManager.GetInventoryAsync(productId, toWarehouseId, toZoneId);
            if (toInventory != null)
            {
                updated = await _dbManager.UpdateInventoryAsync(toInventory.InventoryId, toInventory.Quantity + quantity);
                if (!updated)
                {
                    _logger.LogError("�� ������� �������� ������� �� ������� ������: InventoryId={InventoryId}", toInventory.InventoryId);
                    return false;
                }
            }
            else
            {
                var newInventoryId = await _dbManager.AddInventoryAsync(productId, toWarehouseId, toZoneId, quantity);
                if (!newInventoryId.HasValue)
                {
                    _logger.LogError("�� ������� �������� ������� �� ������� �����: ProductId={ProductId}, ToWarehouseId={ToWarehouseId}, ToZoneId={ToZoneId}", productId, toWarehouseId, toZoneId);
                    return false;
                }
            }

            _logger.LogInformation("����� ������� ���������: ProductId={ProductId}, FromWarehouseId={FromWarehouseId}, FromZoneId={FromZoneId}, ToWarehouseId={ToWarehouseId}, ToZoneId={ToZoneId}, Quantity={Quantity}", productId, fromWarehouseId, fromZoneId, toWarehouseId, toZoneId, quantity);
            return true;
        }

        public async Task<bool> PerformInventoryAsync(int warehouseId, Dictionary<int, int> actualQuantities)
        {
            _logger.LogInformation("������ ��������������: WarehouseId={WarehouseId}, ItemsCount={ItemsCount}", warehouseId, actualQuantities.Count);
            ValidateInput(actualQuantities, nameof(actualQuantities));

            foreach (var entry in actualQuantities)
            {
                var productId = entry.Key;
                var actualQuantity = entry.Value;
                _logger.LogDebug("��������� ������: ProductId={ProductId}, ActualQuantity={ActualQuantity}", productId, actualQuantity);

                var inventory = await _dbManager.GetInventoryAsync(productId, warehouseId);
                if (inventory != null)
                {
                    if (inventory.Quantity != actualQuantity)
                    {
                        _logger.LogInformation("���������� �������: InventoryId={InventoryId}, OldQuantity={OldQuantity}, NewQuantity={NewQuantity}", inventory.InventoryId, inventory.Quantity, actualQuantity);
                        await _dbManager.UpdateInventoryAsync(inventory.InventoryId, actualQuantity);
                    }
                    else
                    {
                        _logger.LogDebug("������� �� ���������: InventoryId={InventoryId}, Quantity={Quantity}", inventory.InventoryId, inventory.Quantity);
                    }
                }
                else
                {
                    _logger.LogInformation("���������� ������ �������: ProductId={ProductId}, WarehouseId={WarehouseId}, Quantity={Quantity}", productId, warehouseId, actualQuantity);
                    await _dbManager.AddInventoryAsync(productId, warehouseId, null, actualQuantity);
                }
            }

            _logger.LogInformation("�������������� ���������: WarehouseId={WarehouseId}", warehouseId);
            return true;
        }
    }

    public class ReportService : BaseService
    {
        public ReportService(DatabaseManager dbManager, ILogger<ReportService> logger)
            : base(dbManager, logger) { }

        public async Task<string> GenerateStockReportAsync(int warehouseId)
        {
            _logger.LogInformation("��������� ������ �� ��������: WarehouseId={WarehouseId}", warehouseId);
            var inventories = await _dbManager.GetInventoryByWarehouseAsync(warehouseId);

            if (!inventories.Any())
            {
                _logger.LogWarning("��� ������ ��� ������: WarehouseId={WarehouseId}", warehouseId);
                return "����� ����.";
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine($"����� �� �������� �� ������ {warehouseId} ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
            report.AppendLine("------------------------------------------------");
            foreach (var inventory in inventories)
            {
                if (inventory.Product == null)
                {
                    _logger.LogWarning("����� ����������� � ����: ProductId={ProductId}, WarehouseId={WarehouseId}", inventory.ProductId, warehouseId);
                    report.AppendLine($"����� (ID: {inventory.ProductId}): ������ �����������, ����������: {inventory.Quantity}");
                    continue;
                }
                var zoneInfo = inventory.Zone != null ? $", ����: {inventory.Zone.Name}" : "";
                report.AppendLine($"�����: {inventory.Product.Name}, SKU: {inventory.Product.Sku}, ����������: {inventory.Quantity}{zoneInfo}");
            }

            _logger.LogInformation("����� ������� ������������: WarehouseId={WarehouseId}, Lines={LinesCount}", warehouseId, inventories.Count);
            return report.ToString();
        }

        public async Task<string> GenerateZoneReportAsync(int warehouseId)
        {
            _logger.LogInformation("��������� ������ �� �����: WarehouseId={WarehouseId}", warehouseId);
            var zones = await _dbManager.GetZonesByWarehouseAsync(warehouseId);

            if (!zones.Any())
            {
                _logger.LogWarning("��� ��� ��� ������: WarehouseId={WarehouseId}", warehouseId);
                return "�� ������ ��� ���.";
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine($"����� �� ����� ������ {warehouseId} ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
            report.AppendLine("------------------------------------------------");
            foreach (var zone in zones)
            {
                report.AppendLine($"����: {zone.Name}, �����������: {zone.Capacity}");
                if (!zone.Inventories.Any())
                {
                    report.AppendLine("  ��� ������� � ����.");
                    continue;
                }
                foreach (var inventory in zone.Inventories)
                {
                    if (inventory.Product == null) continue;
                    report.AppendLine($"  �����: {inventory.Product.Name}, SKU: {inventory.Product.Sku}, ����������: {inventory.Quantity}");
                }
            }

            _logger.LogInformation("����� �� ����� ������� ������������: WarehouseId={WarehouseId}, ZonesCount={ZonesCount}", warehouseId, zones.Count);
            return report.ToString();
        }
    }
}