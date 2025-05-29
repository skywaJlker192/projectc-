using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WarehouseSystem.Models;

namespace WarehouseSystem.DataAccess
{
    public class WarehouseDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Inventory> Inventory { get; set; } = null!;
        public DbSet<Warehouse> Warehouses { get; set; } = null!;
        public DbSet<StorageZone> StorageZones { get; set; } = null!;

        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();
            modelBuilder.Entity<Inventory>()
                .HasIndex(i => new { i.ProductId, i.WarehouseId, i.ZoneId })
                .IsUnique();

            modelBuilder.Entity<Warehouse>().HasData(
                new Warehouse { WarehouseId = 1, Location = "����� A" },
                new Warehouse { WarehouseId = 2, Location = "����� B" }
            );

            modelBuilder.Entity<StorageZone>().HasData(
                new StorageZone { ZoneId = 1, Name = "����������� ������ 1", Capacity = 100, WarehouseId = 1 },
                new StorageZone { ZoneId = 2, Name = "������� A1", Capacity = 200, WarehouseId = 1 }
            );
        }
    }

    public class DatabaseManager
    {
        private readonly WarehouseDbContext _context;
        private readonly ILogger<DatabaseManager> _logger;

        public DatabaseManager(WarehouseDbContext context, ILogger<DatabaseManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int?> CreateZoneAsync(string name, int capacity, int warehouseId)
        {
            try
            {
                _logger.LogInformation("�������� ����: Name={Name}, Capacity={Capacity}, WarehouseId={WarehouseId}", name, capacity, warehouseId);
                var zone = new StorageZone { Name = name, Capacity = capacity, WarehouseId = warehouseId };
                _context.StorageZones.Add(zone);
                await _context.SaveChangesAsync();
                _logger.LogInformation("���� ������� �������: ZoneId={ZoneId}", zone.ZoneId);
                return zone.ZoneId;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "������ ��� �������� ����: Name={Name}, WarehouseId={WarehouseId}", name, warehouseId);
                return null;
            }
        }

        public async Task<List<StorageZone>> GetZonesByWarehouseAsync(int warehouseId)
        {
            try
            {
                _logger.LogInformation("��������� ��� ��� ������: WarehouseId={WarehouseId}", warehouseId);
                var zones = await _context.StorageZones
                    .Where(z => z.WarehouseId == warehouseId)
                    .Include(z => z.Inventories)
                    .ThenInclude(i => i.Product)
                    .ToListAsync();
                _logger.LogInformation("������� �������� {Count} ��� ��� ������ {WarehouseId}", zones.Count, warehouseId);
                return zones;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� ��� ��� ������: WarehouseId={WarehouseId}", warehouseId);
                return new List<StorageZone>();
            }
        }

        public async Task<List<Inventory>> GetInventoryByWarehouseAsync(int warehouseId)
        {
            try
            {
                _logger.LogInformation("��������� �������� ��� ������ {WarehouseId}", warehouseId);
                var inventories = await _context.Inventory
                    .Where(i => i.WarehouseId == warehouseId)
                    .Include(i => i.Product)
                    .Include(i => i.Warehouse)
                    .Include(i => i.Zone)
                    .ToListAsync();
                _logger.LogInformation("������� �������� {Count} ������� ��� ������ {WarehouseId}", inventories.Count, warehouseId);
                return inventories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� �������� ��� ������ {WarehouseId}", warehouseId);
                return new List<Inventory>();
            }
        }

        public async Task<int?> CreateProductAsync(string name, string sku, decimal price)
        {
            try
            {
                _logger.LogInformation("�������� ������: Name={Name}, SKU={Sku}, Price={Price}", name, sku, price);
                var product = new Product { Name = name, Sku = sku, Price = price };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("����� ������� ������: ProductId={ProductId}", product.ProductId);
                return product.ProductId;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "������ ��� �������� ������: Name={Name}, SKU={Sku}", name, sku);
                return null;
            }
        }

        public async Task<Product?> GetProductAsync(int productId)
        {
            try
            {
                _logger.LogInformation("��������� ������: ProductId={ProductId}", productId);
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    _logger.LogInformation("����� ������: ProductId={ProductId}, Name={Name}", productId, product.Name);
                }
                else
                {
                    _logger.LogWarning("����� �� ������: ProductId={ProductId}", productId);
                }
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� ������: ProductId={ProductId}", productId);
                return null;
            }
        }

        public async Task<bool> UpdateProductAsync(int productId, string? name = null, string? sku = null, decimal? price = null)
        {
            try
            {
                _logger.LogInformation("���������� ������: ProductId={ProductId}, Name={Name}, SKU={Sku}, Price={Price}", productId, name, sku, price);
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("����� �� ������ ��� ����������: ProductId={ProductId}", productId);
                    return false;
                }

                if (!string.IsNullOrEmpty(name)) product.Name = name;
                if (!string.IsNullOrEmpty(sku)) product.Sku = sku;
                if (price.HasValue) product.Price = price.Value;

                await _context.SaveChangesAsync();
                _logger.LogInformation("����� ������� ��������: ProductId={ProductId}", productId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "������ ��� ���������� ������: ProductId={ProductId}", productId);
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                _logger.LogInformation("�������� ������: ProductId={ProductId}", productId);
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("����� �� ������ ��� ��������: ProductId={ProductId}", productId);
                    return false;
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("����� ������� ������: ProductId={ProductId}", productId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "������ ��� �������� ������: ProductId={ProductId}", productId);
                return false;
            }
        }

        public async Task<int?> AddInventoryAsync(int productId, int warehouseId, int? zoneId, int quantity)
        {
            try
            {
                _logger.LogInformation("���������� �������: ProductId={ProductId}, WarehouseId={WarehouseId}, ZoneId={ZoneId}, Quantity={Quantity}", productId, warehouseId, zoneId, quantity);
                var inventory = new Inventory { ProductId = productId, WarehouseId = warehouseId, ZoneId = zoneId, Quantity = quantity };
                _context.Inventory.Add(inventory);
                await _context.SaveChangesAsync();
                _logger.LogInformation("������� ������� ��������: InventoryId={InventoryId}", inventory.InventoryId);
                return inventory.InventoryId;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "������ ��� ���������� �������: ProductId={ProductId}, WarehouseId={WarehouseId}, ZoneId={ZoneId}", productId, warehouseId, zoneId);
                return null;
            }
        }

        public async Task<Inventory?> GetInventoryAsync(int productId, int warehouseId, int? zoneId = null)
        {
            try
            {
                _logger.LogInformation("��������� �������: ProductId={ProductId}, WarehouseId={WarehouseId}, ZoneId={ZoneId}", productId, warehouseId, zoneId);
                var query = _context.Inventory
                    .Include(i => i.Product)
                    .Include(i => i.Warehouse)
                    .Include(i => i.Zone)
                    .Where(i => i.ProductId == productId && i.WarehouseId == warehouseId);

                if (zoneId.HasValue)
                {
                    query = query.Where(i => i.ZoneId == zoneId);
                }

                var inventory = await query.FirstOrDefaultAsync();
                if (inventory != null)
                {
                    _logger.LogInformation("������� ������: InventoryId={InventoryId}, Quantity={Quantity}", inventory.InventoryId, inventory.Quantity);
                }
                else
                {
                    _logger.LogWarning("������� �� ������: ProductId={ProductId}, WarehouseId={WarehouseId}, ZoneId={ZoneId}", productId, warehouseId, zoneId);
                }
                return inventory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� �������: ProductId={ProductId}, WarehouseId={WarehouseId}, ZoneId={ZoneId}", productId, warehouseId, zoneId);
                return null;
            }
        }

        public async Task<bool> UpdateInventoryAsync(int inventoryId, int quantity)
        {
            try
            {
                _logger.LogInformation("���������� �������: InventoryId={InventoryId}, Quantity={Quantity}", inventoryId, quantity);
                var inventory = await _context.Inventory.FindAsync(inventoryId);
                if (inventory == null)
                {
                    _logger.LogWarning("������� �� ������ ��� ����������: InventoryId={InventoryId}", inventoryId);
                    return false;
                }

                inventory.Quantity = quantity;
                await _context.SaveChangesAsync();
                _logger.LogInformation("������� ������� ��������: InventoryId={InventoryId}", inventoryId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ���������� �������: InventoryId={InventoryId}", inventoryId);
                return false;
            }
        }
    }
}