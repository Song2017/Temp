using azureGalleryPackageValidator.Mvc.PackageVerify;
using System.Threading.Tasks;
using System;

namespace azureGalleryPackageValidator.Mvc.packageValidator
{
    public class CertificationRequestService : ICertificationRequestService
    {
        public Task<PackageValidationResult> AsyncPackageVerify(string package)
        {
            var validationResult = (new PackageValidator()).Validate(package);

            return null;
        }

       
    }
}