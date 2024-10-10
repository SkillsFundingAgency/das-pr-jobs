using Microsoft.Extensions.Configuration;
using SFA.DAS.Configuration.AzureTableStorage;
using SFA.DAS.Encoding;
using SFA.DAS.PR.Jobs.Configuration;
using System.Configuration;

namespace SFA.DAS.PR.Jobs.Extensions;

public static class AddConfigurationExtension
{
    public static void AddConfiguration(this IConfigurationBuilder builder)
    {
        builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true);

        var config = builder.Build();

        builder.AddAzureTableStorage(options =>
        {
            options.ConfigurationKeys = config["ConfigNames"]!.Split(",");
            options.StorageConnectionString = config["ConfigurationStorageConnectionString"];
            options.EnvironmentName = config["EnvironmentName"];
            options.PreFixConfigurationKeys = false;
            options.ConfigurationKeysRawJsonResult = new[] { ConfigurationKeys.EncodingConfig };
        });
    }
}
