using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace azureGalleryPackageValidator.Mvc.PackageVerify
{
    public class PackageValidationManifest
    {
        [JsonProperty("$schema")]
        public string schema { get; set; }
        public string name { get; set; }
        public string publisher { get; set; }
        public string version { get; set; }
        public string displayName { get; set; }
        public string publisherDisplayName { get; set; }
        public string publisherLegalName { get; set; }
        public string summary { get; set; }
        public string longSummary { get; set; }
        public string description { get; set; }
        public IList<Properties> properties { get; set; }
        public UiDefinition uiDefinition { get; set; }
        public IList<Artifacts> artifacts { get; set; }
        public Icons icons { get; set; }
        public IList<Links> links { get; set; }
        public IList<Products> products { get; set; }
        public IList<string> screenshots { get; set; }
        public IList<string> categories { get; set; }

    }
    public class Properties
    {
        public string displayName { get; set; }
        public string value { get; set; }
    }

    public class UiDefinition
    {
        public string path { get; set; }
    }

    public class Artifacts
    {
        public string name { get; set; }
        public string type { get; set; }
        public string path { get; set; }
        public bool isDefault { get; set; }
    }

    public class Icons
    {
        public string small { get; set; }
        public string medium { get; set; }
        public string large { get; set; }
        public string wide { get; set; }
        public string hero { get; set; }
    }

    public class Links
    {
        public string displayName { get; set; }
        public string uri { get; set; }
    }

    public class Plans
    {
        public string planId { get; set; }
        public string displayName { get; set; }
        public string summary { get; set; }
        public string description { get; set; }
    }

    public class OfferDetails
    {
        public string publisherId { get; set; }
        public string offerId { get; set; }
        public IList<Plans> plans { get; set; }
    }

    public class Products
    {
        public string displayName { get; set; }
        public string publisherDisplayName { get; set; }
        public string legalTerms { get; set; }
        public string privacyPolicy { get; set; }
        public string pricingDetailsUri { get; set; }
        public OfferDetails offerDetails { get; set; }
    }
}
