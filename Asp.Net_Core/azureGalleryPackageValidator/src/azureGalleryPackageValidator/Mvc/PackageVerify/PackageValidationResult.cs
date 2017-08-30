using System.Collections.Generic;

namespace azureGalleryPackageValidator.Mvc.PackageVerify
{

    public class PackageValidationResult
    {
        public bool Result { get; set; }
        public IList<ValidationItem> ValidationItems { get; set; } = new List<ValidationItem>();

        public static PackageValidationResult Fail(string errorMessage)
        {
            return new PackageValidationResult
            {
                Result = ValidationResult.FAIL,
                ValidationItems = { new ValidationItem(ValidationStatus.ERROR, errorMessage) }
            };
        }

        public void Add(ValidationStatus status, string fileName, string field, string message)
        {
            ValidationItems.Add(new ValidationItem(status, fileName, field, message));
        }

        public void Add(ValidationStatus status, string message)
        {
            ValidationItems.Add(new ValidationItem(status, message));
        }
    }

    public class ValidationItem
    {
        public ValidationStatus Status { get; set; }

        public string FileName { get; set; }

        public string Field { get; set; }

        public string Message { get; set; }

        public ValidationItem(ValidationStatus status, string fileName, string field, string message)
        {
            Status = status;
            FileName = fileName;
            Field = field;
            Message = message;
        }

        public ValidationItem(ValidationStatus status, string message)
        {
            Status = status;
            FileName = string.Empty;
            Field = string.Empty;
            Message = message;
        }
    }

    public class ValidationStatus
    {
        public int Status { get; set; }
        public string Name { get; set; }

        public static ValidationStatus ERROR = new ValidationStatus { Status = 0, Name = "error" };
        public static ValidationStatus INFORM = new ValidationStatus { Status = 1, Name = "inform" };
        public static ValidationStatus PASS = new ValidationStatus { Status = 2, Name = "pass" };
    }

    public static class ValidationResult
    {
        public const bool PASS = true;
        public const bool FAIL = false;
    }

    public class TemplateResValResult
    {
        public int ServerFarmsRes { get; set; } = 0;
        public int SitesRes { get; set; } = 0;
        public int SourcecontrolsRes { get; set; } = 0;
        public int MsDeployExtension { get; set; } = 0;
    }

}