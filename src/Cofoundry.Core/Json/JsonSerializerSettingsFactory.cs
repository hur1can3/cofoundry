using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cofoundry.Core.Json
{
    /// <summary>
    /// Gets the default JsonSerializerSettings used by Cofoundry and assigned
    /// to the default Json serializer in asp.net MVC and web api.
    /// </summary>
    public class JsonSerializerSettingsFactory : IJsonSerializerSettingsFactory
    {
        /// <summary>
        /// Creates a new JsonSerializerSettings instance .
        /// </summary>
        public Newtonsoft.Json.JsonSerializerSettings Create()
        {
            Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions settings = new Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions();

            return Configure(settings);
        }

        /// <summary>
        /// Applies the json serializer settings to an existing settings instance.
        /// </summary>
        /// <param name="settings">An existing settings instance to apply updated settings to.</param>
        public Newtonsoft.Json.JsonSerializerSettings Configure(Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            settings.SerializerSettings.Converters.Add(new StringEnumConverter());
            settings.SerializerSettings.Converters.Add(new HtmlStringJsonConverter());
            settings.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            return settings.SerializerSettings;
        }
    }
}