using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace AzureGallery.PackageVerify
{
    public class PackageValidator
    {
        private string _unzippedDirectory;
        private PackageValidationResult _packageValidationResult = new PackageValidationResult();

        public PackageValidationResult Validate(FileInfo file)
        {
            var unzippedDirectory = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName));
            try
            {
                ZipFile.ExtractToDirectory(file.FullName, unzippedDirectory);
                _unzippedDirectory = unzippedDirectory;
            }
            catch
            {
                return PackageValidationResult.Fail("The Package can not be unzipped.");
            }

            ManifestValidate();
            UIDefinitionValidate();
            StringsFolderValidate();

            // try to delete the unzipped directory
            try { Directory.Delete(_unzippedDirectory, true); }
            catch { }// it's okay if deleting throws exceptions.

            _packageValidationResult.Result = _packageValidationResult.ValidationItems.Any(s => s.Status == ValidationStatus.ERROR) ? ValidationResult.FAIL : ValidationResult.PASS;

            return _packageValidationResult;
        }


        private void ManifestValidate()
        {
            var fileName = "Manifest.json";
            var manifest = GetJson(fileName);
            if (manifest == null)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, string.Empty, "Manifest.json is not exists or not a valid JSON file.");
                return;
            }

            SchemaValidate(fileName, "$schema", manifest["$schema"], "https://gallery.azure.com/schemas/2014-09-01/manifest.json#");
            StringFieldValidate(fileName, "name", manifest["name"], "[A-Za-z0-9]+", null, true);
            StringFieldValidate(fileName, "publisher", manifest["publisher"], "[A-Za-z0-9]+", null, true);
            StringFieldValidate(fileName, "version", manifest["version"], null, null, true);
            StringFieldValidate(fileName, "displayName", manifest["displayName"], null, 256, true);
            StringFieldValidate(fileName, "publisherDisplayName", manifest["publisherDisplayName"], null, 256, true);
            StringFieldValidate(fileName, "publisherLegalName", manifest["publisherLegalName"], null, 256, true);
            StringFieldValidate(fileName, "summary", manifest["summary"], null, 100, true);
            StringFieldValidate(fileName, "longSummary", manifest["longSummary"], null, 256, true);
            StringFieldValidate(fileName, "description", manifest["description"], null, 2000, true);

            if (SectionValidate(fileName, "properties", manifest["properties"], false))
            {
                if (manifest["properties"].Count() > 10)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, "properties", $"The amount of \"properties\":{manifest["properties"].Count()} exceed the limit of 10 properties.");
                }
                else
                {
                    for (int i = 0; i < manifest["properties"].Count(); i++)
                    {
                        StringFieldValidate(fileName, $"properties[{i}].displayName", manifest["properties"][i]["displayName"], null, 64, true);
                        StringFieldValidate(fileName, $"properties[{i}].value", manifest["properties"][i]["value"], null, 64, true);
                    }
                }
            }

            if (SectionValidate(fileName, "uiDefinition", manifest["uiDefinition"], true))
            {
                UIDefinitionPathValidation(fileName, "uiDefinition.path", manifest["uiDefinition"]["path"]);
            }

            if (SectionValidate(fileName, "artifacts", manifest["artifacts"], true))
            {
                if (manifest["artifacts"].Count() < 1)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, "artifacts", $"There is no artifact in artifacts field.");
                }

                for (int i = 0; i < manifest["artifacts"].Count(); i++)
                {
                    StringFieldValidate(fileName, $"artifacts[{i}].name", manifest["artifacts"][i]["name"], @"[A-Za-z0-9\-_]+", 256, true);
                    EnforceValidate(fileName, $"artifacts[{i}].type", manifest["artifacts"][i]["type"], new string[] { "Fragment", "Template" }, true);
                    ArtifactsPathValidate(fileName, $"artifacts[{i}].path", manifest["artifacts"][i]["path"], 128);
                    BooleanValidate(fileName, $"artifacts[{i}].isDefault", manifest["artifacts"][i]["isDefault"], true);
                }
            }

            if (SectionValidate(fileName, "icons", manifest["icons"], true))
            {
                ImageValidate("Icons", "icons.small", manifest["icons"]["small"]);
                ImageValidate("Icons", "icons.medium", manifest["icons"]["medium"]);
                ImageValidate("Icons", "icons.large", manifest["icons"]["large"]);
                ImageValidate("Icons", "icons.wide", manifest["icons"]["wide"]);
            }

            if (SectionValidate(fileName, "links", manifest["links"], false))
            {
                if (manifest["links"].Count() > 10)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, "links", $"The amount of \"links\":{manifest["links"].Count()} exceed the limit of 10 links.");
                }
                else
                {
                    for (int i = 0; i < manifest["links"].Count(); i++)
                    {
                        StringFieldValidate(fileName, $"links[{i}].displayName", manifest["links"][i]["displayName"], null, 64, true);
                        StringFieldValidate(fileName, $"links[{i}].uri", manifest["links"][i]["uri"], null, null, true);
                    }
                }
            }

            if (SectionValidate(fileName, "products", manifest["products"], false))
            {
                for (int i = 0; i < manifest["products"].Count(); i++)
                {
                    StringFieldValidate(fileName, $"products[{i}].displayName", manifest["products"][i]["displayName"], null, 256, true);
                    StringFieldValidate(fileName, $"products[{i}].publisherDisplayName", manifest["products"][i]["publisherDisplayName"], null, 256, true);
                    StringFieldValidate(fileName, $"products[{i}].legalTerms", manifest["products"][i]["legalTerms"], null, null, true);
                    StringFieldValidate(fileName, $"products[{i}].privacyPolicy", manifest["products"][i]["privacyPolicy"], null, null, false);
                    StringFieldValidate(fileName, $"products[{i}].pricingDetailsUri", manifest["products"][i]["pricingDetailsUri"], null, null, false);
                    if (SectionValidate(fileName, $"products[{i}].offerDetails", manifest["products"][i]["offerDetails"], false))
                    {
                        StringFieldValidate(fileName, $"products[{i}].offerDetails.publisherId", manifest["products"][i]["offerDetails"]["publisherId"], null, 256, true);
                        StringFieldValidate(fileName, $"products[{i}].offerDetails.offerId", manifest["products"][i]["offerDetails"]["offerId"], null, 256, true);
                        if (SectionValidate(fileName, $"products[{i}].offerDetails.plans", manifest["products"][i]["offerDetails"]["plans"], true))
                        {
                            for (int j = 0; j < manifest["products"][i]["offerDetails"]["plans"].Count(); j++)
                            {
                                StringFieldValidate(fileName, $"products[{i}].offerDetails.plans[{j}].planId", manifest["products"][i]["offerDetails"]["plans"][j]["planId"], null, 256, true);
                                StringFieldValidate(fileName, $"products[{i}].offerDetails.plans[{j}].displayName", manifest["products"][i]["offerDetails"]["plans"][j]["displayName"], null, 256, true);
                                StringFieldValidate(fileName, $"products[{i}].offerDetails.plans[{j}].summary", manifest["products"][i]["offerDetails"]["plans"][j]["summary"], null, 100, true);
                                StringFieldValidate(fileName, $"products[{i}].offerDetails.plans[{j}].description", manifest["products"][i]["offerDetails"]["plans"][j]["description"], null, 2000, true);
                            }
                        }
                    }
                }
            }

            if (SectionValidate(fileName, "screenshots", manifest["screenshots"], true))
            {
                for (int i = 0; i < manifest["screenshots"].Count(); i++)
                {
                    ImageValidate(fileName, $"screenshots[{i}]", manifest["screenshots"][i]);
                }
            }

            if (SectionValidate(fileName, "categories", manifest["categories"], true))
            {
                for (int i = 0; i < manifest["categories"].Count(); i++)
                {
                    StringFieldValidate(fileName, $"categories[{i}]", manifest["categories"][i], null, null, true);
                }
            }

            if (SectionValidate(fileName, "filters", manifest["filters"], false))
            {
                for (int i = 0; i < manifest["filters"].Count(); i++)
                {
                    EnforceValidate(fileName, $"filters[{i}].type", manifest["filters"][i]["type"], new string[] { "Country", "Subscription", "Resources", "HideKey", "OfferType", "OfferCategory" }, true);
                    StringFieldValidate(fileName, $"filters[{i}].value", manifest["filters"][i]["value"], null, null, true);
                }
            }
        }

        private void UIDefinitionValidate()
        {
            var fileName = "UIDefinition.json";
            var uIDefinition = GetJson(fileName);
            if (uIDefinition == null)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, string.Empty, $"{fileName} is not a correct path or a valid JSON file.");
                return;
            }

            SchemaValidate(fileName, "$schema", uIDefinition["$schema"], "https://gallery.azure.com/schemas/2015-02-12/UIDefinition.json");
            if (SectionValidate(fileName, "createDefinition", uIDefinition["createDefinition"], true))
            {
                if (SectionValidate(fileName, "createDefinition.createBlade", uIDefinition["createDefinition"]["createBlade"], true))
                {
                    StringFieldValidate(fileName, "createDefinition.createBlade.name", uIDefinition["createDefinition"]["createBlade"]["name"], null, null, true);
                    StringFieldValidate(fileName, "createDefinition.createBlade.extension", uIDefinition["createDefinition"]["createBlade"]["extension"], null, null, true);
                }

                if (SectionValidate(fileName, "createDefinition.startboardPart", uIDefinition["createDefinition"]["startboardPart"], true))
                {
                    StringFieldValidate(fileName, "createDefinition.startboardPart.name", uIDefinition["createDefinition"]["startboardPart"]["name"], null, null, true);
                    StringFieldValidate(fileName, "createDefinition.startboardPart.extension", uIDefinition["createDefinition"]["startboardPart"]["extension"], null, null, true);
                }

                EnforceValidate(fileName, "createDefinition.startboardPartKeyId", uIDefinition["createDefinition"]["startboardPartKeyId"], new string[] { "id" }, true);
            }

            if (SectionValidate(fileName, "initialData", uIDefinition["initialData"], true))
            {
                BooleanValidate(fileName, "initialData.mysql", uIDefinition["initialData"]["mysql"], true);
                BooleanValidate(fileName, "initialData.localmysql", uIDefinition["initialData"]["localmysql"], false);
                BooleanValidate(fileName, "initialData.sql", uIDefinition["initialData"]["sql"], true);
                if (SectionValidate(fileName, "initialData.hiddenAppConfigSettings", uIDefinition["initialData"]["hiddenAppConfigSettings"], false))
                {
                    for (int i = 0; i < uIDefinition["initialData"]["hiddenAppConfigSettings"].Count(); i++)
                    {
                        StringFieldValidate(fileName, $"initialData.hiddenAppConfigSettings[{i}].name", uIDefinition["initialData"]["hiddenAppConfigSettings"][i]["name"], null, null, true);
                        StringFieldValidate(fileName, $"initialData.hiddenAppConfigSettings[{i}].value", uIDefinition["initialData"]["hiddenAppConfigSettings"][i]["value"], null, null, true);
                    }
                }

                var gitHubDeployment = uIDefinition["initialData"]["gitHubDeployment"];
                var gitHubDeploymentField = "initialData.gitHubDeployment";
                if (SectionValidate(fileName, gitHubDeploymentField, gitHubDeployment, false))
                {
                    StringFieldValidate(fileName, gitHubDeploymentField + ".repoUrl", gitHubDeployment["repoUrl"], null, null, true);
                    StringFieldValidate(fileName, gitHubDeploymentField + ".branch", gitHubDeployment["branch"], null, null, true);
                    if (SectionValidate(fileName, gitHubDeploymentField + ".parameters", gitHubDeployment["parameters"], false))
                    {
                        foreach (var child in gitHubDeployment["parameters"])
                        {
                            var parameter = child.First();
                            UIDefinitionParametersValidate(fileName, parameter.Path, parameter, false);
                        }
                    }
                }

                if (SectionValidate(fileName, "initialData.appSettingsToSet", uIDefinition["initialData"]["appSettingsToSet"], false))
                {
                    StringFieldValidate(fileName, "initialData.appSettingsToSet.phpVersion", uIDefinition["initialData"]["appSettingsToSet"]["phpVersion"], null, null, true);
                }

                var msDeploySettings = uIDefinition["initialData"]["msDeploySettings"];
                var msDeploySettingsField = "initialData.msDeploySettings";
                if (SectionValidate(fileName, msDeploySettingsField, msDeploySettings, false))
                {
                    StringFieldValidate(fileName, msDeploySettingsField + ".msDeployPackage", msDeploySettings["msDeployPackage"], null, null, true);

                    var parameters = msDeploySettings["parameters"];
                    var parametersField = msDeploySettingsField + ".parameters";
                    if (SectionValidate(fileName, parametersField, parameters, false))
                    {
                        foreach (var child in parameters)
                        {
                            var parameter = child.First();
                            UIDefinitionParametersValidate(fileName, parameter.Path, parameter, false);
                        }
                    }

                    if (SectionValidate(fileName, msDeploySettingsField + ".appSettingsToSet", msDeploySettings["appSettingsToSet"], false))
                    {
                        StringFieldValidate(fileName, msDeploySettingsField + ".appSettingsToSet.phpVersion", msDeploySettings["appSettingsToSet"]["phpVersion"], null, null, true);
                    }

                    var hiddenParameters = msDeploySettings["hiddenParameters"];
                    var hiddenParametersField = msDeploySettingsField + ".hiddenParameters";
                    if (SectionValidate(fileName, hiddenParametersField, hiddenParameters, false))
                    {
                        foreach (var child in hiddenParameters)
                        {
                            var parameter = child.First();
                            if (parameter["type"] != null && parameter["type"].Type == JTokenType.String)
                            {
                                switch (parameter["type"].Value<string>())
                                {
                                    case "apppath": TypeValueValidate(fileName, parameter.Path, parameter, new string[] { "apppath" }, false); break;
                                    case "dbname": TypeValueValidate(fileName, parameter.Path, parameter, new string[] { "dbname" }, false); break;
                                    case "db": DatabaseValidate(fileName, parameter.Path, parameter, false); break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Validate parameter jtokens in uIDefinition.json at initialData.gitHubDeployment/msDeploySettings.parameters
        private void UIDefinitionParametersValidate(string fileName, string parameterFiled, JToken parameter, bool isRequired)
        {
            if (SectionValidate(fileName, parameterFiled, parameter, isRequired))
            {
                StringFieldValidate(fileName, parameterFiled + ".defaultValue", parameter["defaultValue"], null, null, false);
                BooleanValidate(fileName, parameterFiled + ".nonAdminUsername", parameter["nonAdminUsername"], false);
                StringFieldValidate(fileName, parameterFiled + ".parameterValue", parameter["parameterValue"], null, null, true);
                StringFieldValidate(fileName, parameterFiled + ".displayName", parameter["displayName"], null, null, true);
                BooleanValidate(fileName, parameterFiled + ".hidden", parameter["hidden"], true);
                StringFieldValidate(fileName, parameterFiled + ".description", parameter["description"], null, null, true);
                StringFieldValidate(fileName, parameterFiled + ".toolTip", parameter["toolTip"], null, null, true);
                EnforceValidate(fileName, parameterFiled + ".type", parameter["type"], new string[] { "dropdown", "text", "password" }, true);
                if (SectionValidate(fileName, parameterFiled + ".constraints", parameter["constraints"], true))
                {
                    var constraints = parameter["constraints"];
                    var constraintsField = parameterFiled + ".constraints";
                    BooleanValidate(fileName, constraintsField + ".required", constraints["required"], true);
                    BooleanValidate(fileName, constraintsField + ".hidden", constraints["hidden"], true);
                    if (SectionValidate(fileName, constraintsField + ".allowedValues", constraints["allowedValues"], true))
                    {
                        var allowedValues = constraints["allowedValues"];
                        var allowedValuesField = constraintsField + ".allowedValues";
                        for (int i = 0; i < allowedValues.Count(); i++)
                        {
                            StringFieldValidate(fileName, allowedValuesField + "[" + i.ToString() + "].text", allowedValues[i]["text"], null, null, true);
                            StringFieldValidate(fileName, allowedValuesField + "[" + i.ToString() + "].value", allowedValues[i]["value"], null, null, true);
                        }
                    }
                    BooleanValidate(fileName, constraintsField + ".hasDigit", constraints["hasDigit"], true);
                    BooleanValidate(fileName, constraintsField + ".hasLetter", constraints["hasLetter"], true);
                    BooleanValidate(fileName, constraintsField + ".hasUpperCaseLetter", constraints["hasUpperCaseLetter"], true);
                    BooleanValidate(fileName, constraintsField + ".hasLowerCaseLetter", constraints["hasLowerCaseLetter"], true);
                    BooleanValidate(fileName, constraintsField + ".hasPunctuation", constraints["hasPunctuation"], true);
                    BooleanValidate(fileName, constraintsField + ".numeric", constraints["numeric"], true);
                    if (SectionValidate(fileName, constraintsField + ".custom", constraints["custom"], true))
                    {
                        for (int i = 0; i < constraints["custom"].Count(); i++)
                        {
                            StringFieldValidate(fileName, constraintsField + ".custom" + "[" + i.ToString() + "].key", constraints["custom"][i]["key"], null, null, true);
                            StringFieldValidate(fileName, constraintsField + ".custom" + "[" + i.ToString() + "].value", constraints["custom"][i]["value"], null, null, true);
                        }
                    }
                }
            }
        }

        // Validate the database releated sections in UIdefinition.json
        private void DatabaseValidate(string fileName, string field, JToken database, bool isRequired)
        {
            if (SectionValidate(fileName, field, database, isRequired))
            {
                EnforceValidate(fileName, field + ".type", database["type"], new string[] { "db" }, true);
                StringFieldValidate(fileName, field + ".oldDBValue", database["oldDBValue"], null, null, true);
                StringFieldValidate(fileName, field + ".newMySQLDBValue", database["newMySQLDBValue"], null, null, true);
                StringFieldValidate(fileName, field + ".oldSqlDBValue", database["oldSqlDBValue"], null, null, false);
                StringFieldValidate(fileName, field + ".newSQLValue", database["newSQLValue"], null, null, true);
            }
        }

        // Validate the field only with type and value key in UIdefinition.json
        private void TypeValueValidate(string fileName, string fieldName, JToken token, string[] types, bool isRequired)
        {
            if (SectionValidate(fileName, fieldName, token, isRequired))
            {
                EnforceValidate(fileName, fieldName + ".type", token["type"], types, true);
                StringFieldValidate(fileName, fieldName + ".value", token["value"], null, null, true);
            }
        }

        private JToken GetJson(string path)
        {
            var filePath = Path.Combine(_unzippedDirectory, path);
            if (!File.Exists(filePath))
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, $"There is no file correspond to the {path} in package.");
                return null;
            }

            var text = File.ReadAllText(filePath).Trim();
            if ((text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("[") && text.EndsWith("]")))
            {
                try
                {
                    return JToken.Parse(text);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        // Validate json file in Strings Folder
        private void StringsFolderValidate()
        {
            string[] files = null;

            try
            {
                files = Directory.GetFiles(Path.Combine(_unzippedDirectory, "Strings"), "*.resjson");
            }
            catch { }

            if (files == null || !files.Any())
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, "There is no json file in folder Strings.");
                return;
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var json = GetJson(Path.Combine("Strings", fileName));
                if (json == null)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, $"The file {fileName} is not a valid json file.");
                }
                else
                {
                    StringResourcesValidate(json, fileName);
                }
            }
        }

        // Validate .resjson file in Strings folder
        private void StringResourcesValidate(JToken jtoken, string fileName)
        {
            StringFieldValidate(fileName, "displayName", jtoken["displayName"], null, null, true);
            StringFieldValidate(fileName, "publisherDisplayName", jtoken["publisherDisplayName"], null, null, true);
            StringFieldValidate(fileName, "summary", jtoken["summary"], null, null, true);
            StringFieldValidate(fileName, "description", jtoken["description"], null, null, true);
            StringFieldValidate(fileName, "authorLink", jtoken["authorLink"], null, null, true);
            StringFieldValidate(fileName, "learnMoreLink", jtoken["learnMoreLink"], null, null, true);
        }

        private void StringFieldValidate(string fileName, string field, JToken token, string regex, int? maxLength, bool isRequired)
        {
            if (token == null)
            {
                if (isRequired)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Field: \"{field}\" missed in {fileName}.");
                }
                return;
            }

            if (token.Type != JTokenType.String)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"The value is not string type. ");
                return;
            }

            if (maxLength != null && token.Value<string>().Length > maxLength)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, "Value: \"{token.Value<string>()}\" exceeding the limit of {maxLength} characters.");
                return;
            }

            if (!string.IsNullOrEmpty(regex) && !(new Regex(regex)).IsMatch(token.Value<string>()))
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Value: \"{token.Value<string>()}\" doesn't match the Regex: {regex}.");
                return;
            }

            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Value: \"{token.Value<string>()}\" is valid.");
        }

        private void SchemaValidate(string fileName, string field, JToken token, string schema)
        {
            if (token == null)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Field: \"{field}\" missed in {fileName}.");
                return;
            }

            if (token.Type != JTokenType.String)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, "The value is not  string type. ");
                return;
            }

            if (!schema.Equals(token.Value<string>()))
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Value: \"{token.Value<string>()}\" doesn't match the schema {schema}.");
                return;
            }

            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Value: \"{token.Value<string>()}\" is valid.");
        }

        // Check a json section exists
        private bool SectionValidate(string fileName, string field, JToken token, bool isRequired)
        {
            if (token == null)
            {
                if (isRequired)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Field: \"{field}\" missed in {fileName}.");
                }
                return false;
            }

            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Field: \"{field}\" exists in {fileName}.");

            return true;
        }

        // In fact, the artifact path is the path of DeploymentTemplates JSON
        private void ArtifactsPathValidate(string fileName, string field, JToken token, int? maxLength)
        {
            if (token == null)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"\"{field}\" missed in {fileName}.");
                return;
            }

            if (token.Type != JTokenType.String)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, "The value is not string type. ");
                return;
            }

            if (maxLength != null && token.Value<string>().Length > maxLength)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Value: \"{token.Value<string>()}\" exceeding the limit of {maxLength} characters.");
                return;
            }

            TemplateValidate(token.Value<string>(), field);
        }

        private void TemplateValidate(string fileName, string field)
        {
            var template = GetJson(fileName);
            if (template == null)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"{fileName} is not a valid path or not a valid JSON file.");
                return;
            }

            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"{fileName} is a a valid JSON file.");

            SchemaValidate(fileName, "$schema", template["$schema"], "http://schema.management.azure.com/schemas/2014-04-01-preview/deploymentTemplate.json#");
            StringFieldValidate(fileName, "contentVersion", template["contentVersion"], null, null, true);

            if (SectionValidate(fileName, "parameters", template["parameters"], true))
            {
                TemplateParametersValidate(fileName, "parameters", template["parameters"]);
            }

            SectionValidate(fileName, "variables", template["variables"], false);

            var resources = template["resources"];
            var resourcesField = "resources";
            if (SectionValidate(fileName, resourcesField, resources, true))
            {
                var result = new TemplateResValResult();
                for (int i = 0; i < resources.Count(); i++)
                {
                    TemplateResourceValidate(fileName, resourcesField + "[" + i.ToString() + "]", resources[i], result);
                }

                if (result.ServerFarmsRes == 0)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, resourcesField, "The resource whoes type is 'Microsoft.Web/serverFarms' is missed.");
                }
                else
                {
                    _packageValidationResult.Add(ValidationStatus.INFORM, fileName, resourcesField, $"The amount of resources whoes type are 'Microsoft.Web/serverFarms' is {result.ServerFarmsRes}.");
                }

                if (result.SitesRes == 0)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, resourcesField, "The resource whoes type is 'Microsoft.Web/Sites' is missed.");
                }
                else
                {
                    _packageValidationResult.Add(ValidationStatus.INFORM, fileName, resourcesField, $"The amount of resources whoes type are 'Microsoft.Web/Sites' is {result.SitesRes}.");
                }

                if (result.SourcecontrolsRes == 0 && result.MsDeployExtension == 0)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, resourcesField, "Both the 'sourcecontrols' resource and 'MSDeploy' resource are missed.");
                }
                else
                {
                    _packageValidationResult.Add(ValidationStatus.INFORM, fileName, resourcesField, $"The amount of resources whoes type are 'sourcecontrols' is {result.SourcecontrolsRes}, and the amount of 'MSDeploy' resources is {result.MsDeployExtension}");
                }
            }
        }

        // Check the parameters field at Template.json
        private void TemplateParametersValidate(string fileName, string parametersField, JToken parameters)
        {
            foreach (var child in parameters)
            {
                var parameter = child.FirstOrDefault();
                if (SectionValidate(fileName, parameter.Path, parameter, false))
                {
                    EnforceValidate(fileName, parameter.Path + ".type", parameter["type"], new string[] { "string", "object", "bool", "securestring" }, true);
                    StringFieldValidate(fileName, parameter.Path + ".defaultValue", parameter["defaultValue"], null, null, false);
                    if (SectionValidate(fileName, parameter.Path + ".allowedValues", parameter["allowedValues"], false))
                    {
                        foreach (var value in parameter["allowedValues"])
                        {
                            StringFieldValidate(fileName, value.Path, value, null, null, false);
                        }
                    }
                }
            }
        }

        // Check each field in the resources field at Template.json
        private void TemplateResourceValidate(string fileName, string resourceField, JToken resource, TemplateResValResult result)
        {
            result.ServerFarmsRes += resource["type"] != null && resource["type"].Type == JTokenType.String && resource["type"].Value<string>().Equals("Microsoft.Web/serverFarms", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            result.SitesRes += resource["type"] != null && resource["type"].Type == JTokenType.String && resource["type"].Value<string>().Equals("Microsoft.Web/Sites", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            result.SourcecontrolsRes += resource["type"] != null && resource["type"].Type == JTokenType.String && resource["type"].Value<string>().Equals("sourcecontrols", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            result.MsDeployExtension += resource["type"] != null && resource["type"].Type == JTokenType.String && resource["type"].Value<string>().Equals("Extensions", StringComparison.OrdinalIgnoreCase) && resource["name"] != null && resource["name"].Type == JTokenType.String && resource["name"].Value<string>().Equals("MSDeploy", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            StringFieldValidate(fileName, resourceField + ".apiVersion", resource["apiVersion"], null, null, true);
            StringFieldValidate(fileName, resourceField + ".name", resource["name"], null, null, true);
            EnforceValidate(fileName, resourceField + ".type", resource["type"], new string[] { "Microsoft.Web/serverfarms", "Microsoft.Web/sites", "Microsoft.Storage/storageAccounts", "config", "Extensions", "microsoft.insights/alertrules", "microsoft.insights/autoscalesettings", "microsoft.insights/components", "SuccessBricks.ClearDB/databases", "Microsoft.DomainRegistration/domains", "Microsoft.Web/sites/hostnameBindings", "Microsoft.Sql/servers", "databases", "firewallrules", "slots", "sourcecontrols", "Microsoft.Scheduler/jobCollections", "jobs", "siteextensions", "extensions/settings" }, true);
            StringFieldValidate(fileName, resourceField + ".kind", resource["kind"], null, null, false);
            StringFieldValidate(fileName, resourceField + ".location", resource["location"], null, null, false);

            var plan = resource["plan"];
            var planField = resourceField + ".plan";
            if (SectionValidate(fileName, planField, plan, false))
            {
                StringFieldValidate(fileName, planField + ".name", plan["name"], null, null, true);
            }

            var sku = resource["sku"];
            var skuField = resourceField + ".sku";
            if (SectionValidate(fileName, skuField, sku, false))
            {
                StringFieldValidate(fileName, skuField + ".name", sku["name"], null, null, true);
                StringFieldValidate(fileName, skuField + ".tier", sku["tier"], null, null, true);
            }

            var tags = resource["tags"];
            var tagsField = resourceField + ".tags";
            if (SectionValidate(fileName, tagsField, tags, false))
            {
                foreach (var child in tags)
                {
                    var tag = child.First();
                    StringFieldValidate(fileName, tag.Path, tag, null, null, true);
                }
            }

            var dependsOn = resource["dependsOn"];
            var dependsOnField = resourceField + ".dependsOn";
            if (SectionValidate(fileName, dependsOnField, dependsOn, false))
            {
                for (int i = 0; i < dependsOn.Count(); i++)
                {
                    StringFieldValidate(fileName, dependsOnField + "[" + i.ToString() + "]", dependsOn[i], null, null, true);
                }
            }

            if (SectionValidate(fileName, resourceField + ".properties", resource["properties"], false))
            {
                ListField(fileName, resourceField + ".properties", resource["properties"]);
            }

            // "resources" field may also contains "resources" field
            var subResources = resource["resources"];
            var subResourcesField = resourceField + ".resources";
            if (SectionValidate(fileName, subResourcesField, subResources, false))
            {
                for (int i = 0; i < subResources.Count(); i++)
                {
                    TemplateResourceValidate(fileName, subResourcesField + "[" + i.ToString() + "]", subResources[i], result);
                }
            }
        }

        private void ListField(string fileName, string field, JToken jToken)
        {
            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Detail: {jToken}");
        }

        // Check if the UIDefinition and Icons path are valid
        private void UIDefinitionPathValidation(string fileName, string field, JToken token)
        {
            if (token == null)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"{field} missed in {fileName}.");
                return;
            }

            if ("UIDefinition.json".Equals(token.Value<string>(), StringComparison.OrdinalIgnoreCase))
            {
                _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Value: \"{token.Value<string>()}\" is a valid path.");
                return;
            }

            _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $" Value:\"{token.Value<string>()}\" is not a valid path.");
        }

        // Enforce the value be the same with a definite string or in several definite string. 
        private void EnforceValidate(string fileName, string field, JToken token, string[] options, bool isRequired)
        {
            if (token == null)
            {
                if (isRequired)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Field: \"{field}\" missed in {fileName}.");
                }
                return;
            }

            if (token.Type != JTokenType.String)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"The value is not  string type. ");
                return;
            }

            if (!options.Contains(token.Value<string>(), StringComparer.OrdinalIgnoreCase))
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Value: \"{token.Value<string>()}\" is not a valid value.");
                return;
            }

            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Value: \"{token.Value<string>()}\" is valid.");
        }

        private void BooleanValidate(string fileName, string field, JToken token, bool isRequired)
        {
            if (token == null)
            {
                if (isRequired)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Field: \"{field}\" missed in {fileName}.");
                }
                return;
            }

            if (token.Type != JTokenType.Boolean)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"The value is not a boolean, please check.");
                return;
            }

            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Value: {token.Value<bool>()}  is a valid boolean value.");
        }

        private void ImageValidate(string fileName, string field, JToken token)
        {
            if (token == null)
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"{field} missed in {fileName}.");
                return;
            }
            var imagePath = Path.Combine(_unzippedDirectory, token.Value<string>());
            if (!File.Exists(imagePath))
            {
                _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"There is no file correspond to the {token.Value<string>()} in package.");
                return;
            }

            using (var image = Image.FromFile(imagePath))
            {
                if (image == null)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Value \"{ token.Value<string>() }\" this file is not an  is not an image.");
                    return;
                }

                if (image.RawFormat.Guid != ImageFormat.Png.Guid)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Value \"{ token.Value<string>() }\" this image is not in PNG format.");
                    return;
                }

                int width;
                int height;
                switch (field)
                {
                    case "icons.small": width = 40; height = 40; break;
                    case "icons.medium": width = 90; height = 90; break;
                    case "icons.large": width = 115; height = 115; break;
                    case "icons.wide": width = 255; height = 115; break;
                    default: width = 533; height = 324; break;
                }

                if (image.Height != height || image.Width != width)
                {
                    _packageValidationResult.Add(ValidationStatus.ERROR, fileName, field, $"Value \"{ token.Value<string>() }\"this image is {image.Width}x{image.Height}, and it should be {width}x{height}.");
                    return;
                }
            }

            _packageValidationResult.Add(ValidationStatus.PASS, fileName, field, $"Value: \"{token.Value<string>()}\"the path is valid the image is valid.");
        }
    }
}