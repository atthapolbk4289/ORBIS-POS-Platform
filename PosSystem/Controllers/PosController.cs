using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosSystem.Models.ViewModels;
using PosSystem.Models.ViewModels.PosSystem.Repositories.Interfaces;
using PosSystem.Models.ViewModels.PosSystem.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PosSystem.Controllers
{
    [Authorize(Roles = "IT_ADMIN,MANAGER,CASHIER")]
    public class PosController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly ITableService _tableService;
        private readonly IPromotionService _promotionService;

        public PosController(
            IProductRepository productRepo,
            ITableService tableService,
            IPromotionService promotionService)
        {
            _productRepo = productRepo;
            _tableService = tableService;
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["ActiveMenu"] = "pos";
            ViewData["PageTitle"] = "หน้าร้าน POS";
            ViewData["TopIcon"] = "store";
            
            var branchIdClaim = User.FindFirst("BranchId")?.Value;
            var branchId = string.IsNullOrEmpty(branchIdClaim) ? Guid.Empty : Guid.Parse(branchIdClaim);
            
            var vm = new PosViewModel
            {
                Categories = await _productRepo.GetCategoriesAsync(branchId) ?? new(),
                Tables     = await _tableService.GetTablesAsync(branchId) ?? new()
                // ActivePromotions not included to avoid compilation errors due to method signature mismatches
                // ActivePromotions = await _promotionService.GetActiveAsync(branchId, 0)
            };
            return View(vm);
        }
    }
}
