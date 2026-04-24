using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosSystem.Models.ViewModels;
using PosSystem.Models.ViewModels.PosSystem.Services.Interfaces; // Due to namespace nesting
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PosSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IStockService _stockService;
        private readonly IPromotionService _promotionService;
        private readonly ICustomerService _customerService;

        public DashboardController(
            IReportService reportService, 
            IStockService stockService, 
            IPromotionService promotionService, 
            ICustomerService customerService)
        {
            _reportService = reportService;
            _stockService = stockService;
            _promotionService = promotionService;
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["ActiveMenu"] = "dashboard";
            ViewData["PageTitle"] = "ยอดขาย";
            ViewData["TopIcon"] = "trending-up";
            
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchId = string.IsNullOrEmpty(branchIdClaim) ? Guid.Empty : Guid.Parse(branchIdClaim);
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            
            var vm = new DashboardViewModel
            {
                MonthlySales = await _reportService.GetSalesSummaryAsync(branchId, monthStart, today) ?? new MonthlySalesData(),
                DailySales   = await _reportService.GetDailySalesAsync(branchId, today.AddDays(-29), today) ?? new(),
                HourlySales  = await _reportService.GetHourlySalesAsync(branchId, today) ?? new(),
                TopProducts  = await _reportService.GetTopProductsAsync(branchId, monthStart, today, 10) ?? new(),
                ProfitLoss   = await _reportService.GetProfitLossAsync(branchId, monthStart, today) ?? new ProfitLossData(),
                LowStockCount = await _stockService.GetLowStockCountAsync(branchId),
                SalesByType  = await _reportService.GetSalesByOrderTypeAsync(branchId, monthStart, today) ?? new(),
                PromotionCount = await _promotionService.GetActiveCountAsync(branchId),
                CustomerCount  = await _customerService.GetMonthlyNewCountAsync(branchId, monthStart)
            };
            return View(vm);
        }
    }
}
