namespace Tech_Store.Models.Enums
{
    public enum InventoryTransactionType
    {
        Import = 1,
        Export = 2
    }

    public static class InventoryTransactionTypeExtensions
    {
        public static string ToRecordValue(this InventoryTransactionType inventoryTransactionType)
        {
            return inventoryTransactionType switch
            {
                InventoryTransactionType.Import => "Import",
                InventoryTransactionType.Export => "Export",
                _ => "Import"
            };
        }
    }
}
