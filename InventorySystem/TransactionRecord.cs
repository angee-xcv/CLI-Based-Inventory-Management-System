using System;

namespace InventorySystem
{
    public enum TransactionType { StockIn, StockOut, ProductAdded, ProductUpdated, ProductDeleted }

    public class TransactionRecord
    {
        private static int _nextId = 1;

        public int Id { get; private set; }
        public TransactionType Type { get; private set; }
        public int ProductId { get; private set; }
        public string ProductName { get; private set; }
        public int? QuantityChanged { get; private set; }
        public int? StockBefore { get; private set; }
        public int? StockAfter { get; private set; }
        public string PerformedBy { get; private set; }
        public string Notes { get; private set; }
        public DateTime Timestamp { get; private set; }

        public TransactionRecord(TransactionType type, int productId, string productName,
            string performedBy, string notes = "",
            int? quantity = null, int? before = null, int? after = null)
        {
            Id = _nextId++;
            Type = type;
            ProductId = productId;
            ProductName = productName;
            PerformedBy = performedBy;
            Notes = notes ?? "";
            QuantityChanged = quantity;
            StockBefore = before;
            StockAfter = after;
            Timestamp = DateTime.Now;
        }

        public string TypeLabel()
        {
            if (Type == TransactionType.StockIn) return "STOCK IN";
            if (Type == TransactionType.StockOut) return "STOCK OUT";
            if (Type == TransactionType.ProductAdded) return "ADDED";
            if (Type == TransactionType.ProductUpdated) return "UPDATED";
            if (Type == TransactionType.ProductDeleted) return "DELETED";
            return "OTHER";
        }

        public static void ResetCounter(int value) => _nextId = value;
    }
}