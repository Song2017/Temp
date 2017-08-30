using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using azureGalleryPackageValidator.Mvc.packageValidator;

namespace azureGalleryPackageValidator.Controllers
{
    public class HomeController : Controller
    {
        private string package = @"D:\Temp\Asp.Net_Core\azureGalleryPackageValidator\src\azureGalleryPackageValidator\Temp\azure-marketplace-deploy_3.zip";

        private ICertificationRequestService _certificationRequestService;

        public HomeController(ICertificationRequestService iCertificationRequestService)
        {
            _certificationRequestService = iCertificationRequestService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public async Task<IActionResult> Contact()
        {
            ViewData["Message"] = "Your contact page.";
            var packageValidation = await _certificationRequestService.AsyncPackageVerify(package);
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PackageValidation()
        {

            var packageValidation = await _certificationRequestService.AsyncPackageVerify(package);

            return View();
            //return Json(new { PackageValidation = packageValidation });
        }
    }
}
