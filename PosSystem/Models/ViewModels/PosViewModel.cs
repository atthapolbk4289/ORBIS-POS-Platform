using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PosSystem.Models.ViewModels
{
    public class PosViewModel
    {
        public List<CategoryData> Categories { get; set; } = new();
        public List<TableData> Tables { get; set; } = new();
        public List<PromotionData> ActivePromotions { get; set; } = new();
    }

    public class CategoryData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }
    public class TableData
    {
        public Guid Id { get; set; }
        public string TableNumber { get; set; } = "";
        public string Status { get; set; } = "AVAILABLE";
    }
    public class PromotionData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }

    namespace PosSystem.Repositories.Interfaces
    {
        public interface IProductRepository { Task<List<CategoryData>> GetCategoriesAsync(Guid branchId); }
    }
    namespace PosSystem.Services.Interfaces
    {
        public interface ITableService { Task<List<TableData>> GetTablesAsync(Guid branchId); }
    }
}
