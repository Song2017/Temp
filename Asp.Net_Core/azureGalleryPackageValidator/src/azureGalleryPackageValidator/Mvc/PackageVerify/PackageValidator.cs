using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace azureGalleryPackageValidator.Mvc.PackageVerify
{
    public class PackageValidator
    {
        private string _unzippedDirectory;
        private PackageValidationResult _packageValidationResult = new PackageValidationResult();
        private string _jsonFile = "Manifest.json";

        public PackageValidationResult Validate(string package)
        {
            FileInfo packageFile = new FileInfo(package);
            _unzippedDirectory = Path.Combine(packageFile.DirectoryName, Path.GetFileNameWithoutExtension(packageFile.FullName));
            //unzip file
            //ZipFile.ExtractToDirectory(packageFile.FullName, _unzippedDirectory);
            //Get Manifest.json from unzip folder
            PackageValidationManifest manifest = GetJson<PackageValidationManifest>(Path.Combine(_unzippedDirectory, _jsonFile));

            var type = manifest.GetType();
            var manifestProperties = typeof(PackageValidationManifest).GetProperties();

            string propertityValue;
            string propertityName;
            foreach (var pro in manifestProperties)
            {
                if (pro == null)
                    continue;

                propertityName = pro.Name;

                if (pro.PropertyType.Equals(typeof(string)))
                {
                    propertityValue = pro.GetValue(manifest).ToString();

                }
                else if (propertityName.Equals("links"))
                {
                }
                else if (propertityName.Equals("icons"))
                {
                }
            }

            return _packageValidationResult;
        }

        private T GetJson<T>(string v)
        {
            if (!File.Exists(v))
                return default(T);

            string text = File.ReadAllText(v).Trim();
            //text = text.Replace(@"$schema", "schema");
            try
            {
                JsonSerializerSettings jss = new JsonSerializerSettings();
                jss.NullValueHandling = NullValueHandling.Ignore;
                
                T package = JsonConvert.DeserializeObject<T>(text);
                return package;
            }
            catch { }

            return default(T);
        }
    }
}