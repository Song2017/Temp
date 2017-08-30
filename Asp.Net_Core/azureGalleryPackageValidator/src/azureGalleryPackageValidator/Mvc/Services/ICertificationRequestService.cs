using azureGalleryPackageValidator.Mvc.PackageVerify;
using System.Threading.Tasks;

namespace azureGalleryPackageValidator.Mvc.packageValidator
{
    public interface ICertificationRequestService
    {
        Task<PackageValidationResult> AsyncPackageVerify(string packagePath);
         
    }
}