using Kalite.Application.Features.ProductionLots.Queries.GetList;
using KaliteWeb.UI.Models.ProductionLots;
using KaliteWeb.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KaliteWeb.UI.Controllers;

public class ProductionLotController : Controller
{
    private readonly IProductionLotApiService _productionLotService;
    private readonly IDepoApiService _depoService;

    public ProductionLotController(
        IProductionLotApiService productionLotService,
        IDepoApiService depoService)
    {
        _productionLotService = productionLotService;
        _depoService = depoService;
    }

    // =========================================================
    // INDEX
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productionLotService.GetListAsync(cancellationToken);
            return View(result);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(Array.Empty<ProductionLotListDto>());
        }
    }

    // =========================================================
    // CREATE GET (Ayrı Sayfa Olarak Açıldığında)
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> Create(int productionId, string? productionNo = null, string? mainLotNo = null)
    {
        if (productionId <= 0)
        {
            return BadRequest("Geçerli bir üretim seçilmelidir.");
        }

        var depolar = await _depoService.GetListAsync();

        var model = new CreateProductionLotViewModel
        {
            ProductionId = productionId,
            ProductionNo = productionNo ?? "",
            MainLotNo = mainLotNo ?? "",
            ManualSubLotNo = false,
            Quantity = 0,
            DepoId = null,
            Depolar = depolar.Select(x => new DepoSelectItemViewModel
            {
                Id = x.Id,
                DepoAdi = x.DepoAdi
            }).ToList()
        };

        return View(model);
    }

    // =========================================================
    // CREATE POST (Normal Form Gönderimi)
    // =========================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductionLotViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var depolar = await _depoService.GetListAsync();
            model.Depolar = depolar.Select(x => new DepoSelectItemViewModel
            {
                Id = x.Id,
                DepoAdi = x.DepoAdi
            }).ToList();
            return View(model);
        }

        try
        {
            await _productionLotService.CreateAsync(model);
            TempData["SuccessMessage"] = "Alt lot başarıyla oluşturuldu.";
            return RedirectToAction("Detail", "Production", new { id = model.ProductionId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var depolar = await _depoService.GetListAsync();
            model.Depolar = depolar.Select(x => new DepoSelectItemViewModel
            {
                Id = x.Id,
                DepoAdi = x.DepoAdi
            }).ToList();
            return View(model);
        }
    }

    // =========================================================
    // CREATE AJAX (Detail.cshtml Modalından Gelen JSON Çağrısı)
    // =========================================================
    [HttpPost]
    [Route("ProductionLot/CreateAjax")]
    public async Task<IActionResult> CreateAjax([FromBody] CreateProductionLotAjaxRequest model)
    {
        if (model == null || model.ProductionId <= 0 || model.Quantity <= 0 || string.IsNullOrWhiteSpace(model.SubLotNo))
        {
            return BadRequest(new { success = false, message = "Üretim ID, Alt Lot No ve geçerli bir miktar zorunludur." });
        }

        try
        {
            var vm = new CreateProductionLotViewModel
            {
                ProductionId = model.ProductionId,
                SubLotNo = model.SubLotNo.Trim(),
                Quantity = model.Quantity,
                DepoId = model.DepoId,
                Description = model.Description,
                ManualSubLotNo = true
            };

            await _productionLotService.CreateAsync(vm);
            return Json(new { success = true, message = "Alt lot başarıyla kaydedildi." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================
    // ÜRETİME AİT ALT LOTLAR
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> List(int productionId, CancellationToken cancellationToken)
    {
        if (productionId <= 0)
        {
            return BadRequest("Geçerli bir üretim seçilmelidir.");
        }

        var result = await _productionLotService.GetByProductionIdAsync(productionId, cancellationToken);
        return Json(result);
    }
}

public class CreateProductionLotAjaxRequest
{
    public int ProductionId { get; set; }
    public string SubLotNo { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int? DepoId { get; set; }
    public string? Description { get; set; }
}