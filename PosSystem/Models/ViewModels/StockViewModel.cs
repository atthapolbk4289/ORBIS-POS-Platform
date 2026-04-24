using System;
using System.Collections.Generic;

namespace PosSystem.Models.ViewModels
{
    public class StockIndexViewModel
    {
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public string? Search { get; set; }
        public bool LowStockOnly { get; set; }
        public string? StatusFilter { get; set; }
        public string? SortBy { get; set; }
        public List<StockItemData> StockItems { get; set; } = new();
    }

    public class StockItemData
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Sku { get; set; } = "";
        public int PhysicalQty { get; set; }
        public int ReservedQty { get; set; }
        public int AvailableQty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal StockValue { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class ProductDropdownDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public int PhysicalQty { get; set; }
    }

    // DTOs for Stock endpoints
    namespace PosSystem.Models.Dtos
    {
        public class ReceiveStockDto 
        {
            public Guid ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal CostPrice { get; set; }
            public string? Note { get; set; }
            public string? ReferenceNo { get; set; }
        }

        public class AdjustStockDto 
        {
            public Guid ProductId { get; set; }
            public string AdjustType { get; set; } = "SET"; // SET, ADD, SUB
            public int Quantity { get; set; }
            public string? Note { get; set; }
        }

        public class DamageStockDto
        {
            public Guid ProductId { get; set; }
            public int Quantity { get; set; }
            public string? Reason { get; set; }
        }

        public class StockCountDto
        {
            public Guid ProductId { get; set; }
            public int ActualQuantity { get; set; }
            public string? Note { get; set; }
        }
    }
}

