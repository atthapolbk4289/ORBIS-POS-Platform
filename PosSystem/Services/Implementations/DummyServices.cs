using PosSystem.Models.ViewModels;
using PosSystem.Models.ViewModels.PosSystem.Repositories.Interfaces;
using PosSystem.Models.ViewModels.PosSystem.Services.Interfaces;
using PosSystem.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Services.Implementations
{
    public class DummyProductRepository : IProductRepository
    {
        public Task<List<CategoryData>> GetCategoriesAsync(Guid branchId)
        {
            return Task.FromResult(new List<CategoryData> 
            {
                new CategoryData { Id = Guid.NewGuid(), Name = "อาหาร" },
                new CategoryData { Id = Guid.NewGuid(), Name = "เครื่องดื่ม" }
            });
        }
    }

    public class DummyTableService : ITableService
    {
        public Task<List<TableData>> GetTablesAsync(Guid branchId)
        {
            return Task.FromResult(new List<TableData>
            {
                new TableData { Id = Guid.NewGuid(), TableNumber = "1", Status = "AVAILABLE" },
                new TableData { Id = Guid.NewGuid(), TableNumber = "2", Status = "OCCUPIED" }
            });
        }
    }

    public class DummyPromotionService : IPromotionService
    {
        public Task<int> GetActiveCountAsync(Guid branchId) => Task.FromResult(5);
    }

    public class DummyCustomerService : ICustomerService
    {
        public Task<int> GetMonthlyNewCountAsync(Guid branchId, DateTime from) => Task.FromResult(120);
    }

    public class DummyReportService : IReportService
    {
        public Task<List<DailySalesData>> GetDailySalesAsync(Guid branchId, DateTime from, DateTime to)
            => Task.FromResult(new List<DailySalesData>());
        
        public Task<List<HourlySalesData>> GetHourlySalesAsync(Guid branchId, DateTime date)
            => Task.FromResult(new List<HourlySalesData>());
            
        public Task<ProfitLossData> GetProfitLossAsync(Guid branchId, DateTime from, DateTime to)
            => Task.FromResult(new ProfitLossData());
            
        public Task<List<SalesByTypeData>> GetSalesByOrderTypeAsync(Guid branchId, DateTime from, DateTime to)
            => Task.FromResult(new List<SalesByTypeData>());
            
        public Task<MonthlySalesData> GetSalesSummaryAsync(Guid branchId, DateTime from, DateTime to)
            => Task.FromResult(new MonthlySalesData());
            
        public Task<List<ProductSalesData>> GetTopProductsAsync(Guid branchId, DateTime from, DateTime to, int limit)
            => Task.FromResult(new List<ProductSalesData>());
    }

    public class DummyStockService : IStockService
    {
        public Task<int> GetLowStockCountAsync(Guid branchId) => Task.FromResult(3);
    }
}
