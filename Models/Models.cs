namespace WarehouseSystem.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class Inventory
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int? ZoneId { get; set; } // ��������� ����� � �����
        public int Quantity { get; set; }
        public Product? Product { get; set; }
        public Warehouse? Warehouse { get; set; }
        public StorageZone? Zone { get; set; } // ������������� ��������
    }

    public class Warehouse
    {
        public int WarehouseId { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<StorageZone> Zones { get; set; } = new List<StorageZone>(); // ������ ���
    }

    public class StorageZone
    {
        public int ZoneId { get; set; }
        public string Name { get; set; } = string.Empty; // ��������, "����������� ������ 1"
        public int Capacity { get; set; } // ������������ �����������
        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }
        public List<Inventory> Inventories { get; set; } = new List<Inventory>(); // ������ � ����
    }
}