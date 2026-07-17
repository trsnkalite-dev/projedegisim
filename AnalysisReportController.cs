



using AnteMortemWeb.UI.Dtos.AnalysisReportDto;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AnteMortemWeb.UI.Controllers
{
    public class AnalysisReportController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AnalysisReportController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateClient()
        {
            // Localhost SSL hatalarını bypass etmek için handler kullanıyoruz
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            return client;
        }

        public async Task<IActionResult> Dashboard()
        {
            var client = CreateClient();
            var response = await client.GetAsync("https://localhost:7194/api/AnalysisReport/dashboard");

            if (!response.IsSuccessStatusCode)
                return View(new List<ResultAnalysisReportDto>());

            var jsonData = await response.Content.ReadAsStringAsync();
            var values = JsonConvert.DeserializeObject<List<ResultAnalysisReportDto>>(jsonData)
                         ?? new List<ResultAnalysisReportDto>();
            return View(values);
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = CreateClient();

                var response = await client.GetAsync("https://localhost:7194/api/AnalysisReport");

                Console.WriteLine("STATUS: " + response.StatusCode);

                var jsonData = await response.Content.ReadAsStringAsync();

                Console.WriteLine("JSON:");
                Console.WriteLine(jsonData);

                var values = JsonConvert.DeserializeObject<List<ResultAnalysisReportDto>>(jsonData);

                Console.WriteLine("COUNT: " + values?.Count);

                return View(values ?? new List<ResultAnalysisReportDto>());
            }
            catch (Exception ex)
            {
                Console.WriteLine("HATA:");
                Console.WriteLine(ex.ToString());

                return View(new List<ResultAnalysisReportDto>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var client = CreateClient();

                var response = await client.GetAsync($"https://localhost:7194/api/AnalysisReport/GetAnalysisReport?id={id}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Rapor getirilemedi";
                    return RedirectToAction("Index");
                }

                var jsonData = await response.Content.ReadAsStringAsync();

                Console.WriteLine("DETAIL JSON:");
                Console.WriteLine(jsonData);

                var value = JsonConvert.DeserializeObject<ResultAnalysisReportDto>(jsonData);

                if (value == null)
                {
                    TempData["Error"] = "Rapor bulunamadı";
                    return RedirectToAction("Index");
                }

                // 🔥 NULL PATLAMASINI ENGELLE
                value.Analyses ??= new List<ResultAnalysisReportDto.AnalysisItemDto>();

                return View(value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DETAILS ERROR:");
                Console.WriteLine(ex.ToString());

                TempData["Error"] = "Sunucuya ulaşılamadı";
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public IActionResult Create()
        {

            var model = new CreateAnalysisReportDto
            {
                Analyses = new List<CreateAnalysisItemDto>
                {
                    new(){ AnalysisName="Salmonella"},
                    new(){ AnalysisName="Toplam Koliform"},
                    new(){ AnalysisName="E.Coli"},
                    new(){ AnalysisName="TAMB(36°C)"},
                    new(){ AnalysisName="Enterekok"},
                    new(){ AnalysisName="Fekal Koliform"},
                    new(){ AnalysisName="TAMB(22°C)"},
                    new(){ AnalysisName="E.coli O157"},
                    new(){ AnalysisName="L.monocytogenes"},
                    new(){ AnalysisName="Stafilokok Enterebacter"}
                }
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAnalysisReportDto model)
        {

            Console.WriteLine("MVC CREATE ÇALIŞTI");

            Console.WriteLine(Request.ContentType);

            try
            {
                var client = CreateClient();

                model.Analyses ??= new List<CreateAnalysisItemDto>();

                var selectedAnalyses = model.Analyses
                    .Where(x => x.Positive)
                    .ToList();

                var newModel = new CreateAnalysisReportDto
                {
                    ReportDate = model.ReportDate,
                    ReportNo = model.ReportNo,
                    ProductName = model.ProductName,
                    AnalysisType = model.AnalysisType,
                    SamplingLocation = model.SamplingLocation,
                    ProductionDate = model.ProductionDate,
                    ExpiryDate = model.ExpiryDate,
                    LotNo = model.LotNo,
                    Sonuc = model.Sonuc,
                    Analyses = selectedAnalyses
                };

                var jsonData = JsonConvert.SerializeObject(
                    newModel,
                    Formatting.Indented);

                Console.WriteLine("GIDEN JSON:");
                Console.WriteLine(jsonData);

                var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    "https://localhost:7194/api/AnalysisReport",
                    content);

                var responseText =
                    await response.Content.ReadAsStringAsync();

                Console.WriteLine("API RESPONSE:");
                Console.WriteLine(responseText);

                if (response.IsSuccessStatusCode)
                {
                    TempData["success"] = "Kayıt başarılı";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", responseText);

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                ModelState.AddModelError("", ex.Message);

                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id)
        {
            var client = CreateClient();
            await client.DeleteAsync("https://localhost:7194/api/AnalysisReport?id=" + id);
            return RedirectToAction("Index");
        }

        [HttpGet("GetAnalysisReport")]
        public async Task<IActionResult> Update(int id)
        {
            var client = CreateClient();
            var responseMessage = await client.GetAsync("https://localhost:7194/api/AnalysisReport/GetAnalysisReport?id=" + id);

            if (!responseMessage.IsSuccessStatusCode)
                return RedirectToAction("Index");

            var jsonData = await responseMessage.Content.ReadAsStringAsync();
            var value = JsonConvert.DeserializeObject<GetAnalysisReportByIdDto>(jsonData);

            if (value == null)
                return RedirectToAction("Index");

            // Mevcut analizleri işaretle
            foreach (var item in value.Analyses)
            {
                item.Positive = true;
            }

            // Eksik varsayılan analizleri ekle
            var defaultAnalyses = new List<string> {
                "Salmonella", "Toplam Koliform", "E.Coli", "TAMB(36°C)",
                "Enterekok", "Fekal Koliform", "TAMB(22°C)", "E.coli O157", "L.monocytogenes"
            };

            foreach (var name in defaultAnalyses)
            {
                if (!value.Analyses.Any(x => x.AnalysisName == name))
                {
                    value.Analyses.Add(new GetAnalysisReportByIdDto.AnalysisItemDto
                    {
                        AnalysisName = name,
                        Positive = false,
                        Sonuc = "Uygun"
                    });
                }
            }

            return View(value);
        }

        [HttpPost]
        public async Task<IActionResult> Update(UpdateAnalysisReportDto model)
        {
            var client = CreateClient();

            // Sadece "Evet" seçilenleri gönder
            var selectedAnalyses = model.Analyses
                .Where(x => x.Positive == true)
                .Select(x => new UpdateAnalysisReportDto.AnalysisItemDto
                {
                    AnalysisName = x.AnalysisName,
                    LimitValue = x.LimitValue,
                    Result = x.Result,
                    Sonuc = x.Sonuc,
                    Positive = true
                })
                .ToList();

            model.Analyses = selectedAnalyses;

            var jsonData = JsonConvert.SerializeObject(model);
            StringContent stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await client.PutAsync("https://localhost:7194/api/AnalysisReport", stringContent);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            return View("Update", model); // Hata durumunda Update view'ına geri dön (ama model tipi farklı olabilir, dikkat)
        }
    } }
