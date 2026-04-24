using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        public MonthlySalesData MonthlySales { get; set; } = new();
        public List<DailySalesData> DailySales { get; set; } = new();
        public List<HourlySalesData> HourlySales { get; set; } = new();
        public List<ProductSalesData> TopProducts { get; set; } = new();
        public ProfitLossData ProfitLoss { get; set; } = new();
        public int LowStockCount { get; set; }
        public List<SalesByTypeData> SalesByType { get; set; } = new();
        public int PromotionCount { get; set; }
        public int CustomerCount { get; set; }
    }

    public class MonthlySalesData
    {
        public string MonthLabel { get; set; } = DateTime.Now.ToString("MMMM");
        public decimal TotalRevenue { get; set; }
    }
    public class DailySalesData
    {
        public string DateLabel { get; set; } = "";
        public decimal Revenue { get; set; }
    }
    public class HourlySalesData
    {
        public int Hour { get; set; }
        public decimal Revenue { get; set; }
    }
    public class ProductSalesData
    {
        public string Name { get; set; } = "";
        public decimal TotalRevenue { get; set; }
    }
    public class ProfitLossData
    {
        public decimal GrossProfit { get; set; }
        public decimal GrossMarginPct { get; set; }
    }
    public class SalesByTypeData
    {
        public string Label { get; set; } = "";
        public decimal Percentage { get; set; }
        public decimal Amount { get; set; }
        public string Color { get; set; } = "#000";
    }

    // Stubs for Services needed by Dashboard
    namespace PosSystem.Services.Interfaces
    {
        public interface IReportService 
        {
            Task<MonthlySalesData> GetSalesSummaryAsync(Guid branchId, DateTime from, DateTime to);
            Task<List<DailySalesData>> GetDailySalesAsync(Guid branchId, DateTime from, DateTime to);
            Task<List<HourlySalesData>> GetHourlySalesAsync(Guid branchId, DateTime date);
            Task<List<ProductSalesData>> GetTopProductsAsync(Guid branchId, DateTime from, DateTime to, int limit);
            Task<ProfitLossData> GetProfitLossAsync(Guid branchId, DateTime from, DateTime to);
            Task<List<SalesByTypeData>> GetSalesByOrderTypeAsync(Guid branchId, DateTime from, DateTime to);
        }
        public interface IStockService { Task<int> GetLowStockCountAsync(Guid branchId); }
        public interface IPromotionService { Task<int> GetActiveCountAsync(Guid branchId); }
        public interface ICustomerService { Task<int> GetMonthlyNewCountAsync(Guid branchId, DateTime from); }
    }
}
