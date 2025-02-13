using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.PR.Data.Repositories;

namespace SFA.DAS.PR.Data.Extensions;

[ExcludeFromCodeCoverage]
public static class AddPrDataContextExtension
{
    public static IServiceCollection AddPrDataContext(this IServiceCollection services, IConfiguration configuration)
    {
        var sqlConnectionString = configuration["SqlConnectionString"]!;

        services.AddDbContext<ProviderRelationshipsDataContext>((serviceProvider, options) =>
        {
            SqlConnection connection = null!;

            connection = new SqlConnection(sqlConnectionString);

            options.UseSqlServer(
                connection,
                o => o.CommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds));
        });

        services.AddTransient<IProviderRelationshipsDataContext, ProviderRelationshipsDataContext>(provider => provider.GetService<ProviderRelationshipsDataContext>()!);

        RegisterRepositories(services);

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddTransient<IProvidersRepository, ProvidersRepository>();
        services.AddTransient<INotificationRepository, NotificationRepository>();
        services.AddTransient<IAccountLegalEntityRepository, AccountLegalEntityRepository>();
        services.AddTransient<IAccountProviderRepository, AccountProviderRepository>();
        services.AddTransient<IAccountProviderLegalEntityRepository, AccountProviderLegalEntityRepository>();
        services.AddTransient<IRequestsRepository, RequestsRepository>();
        services.AddTransient<IAccountProviderLegalEntityRepository, AccountProviderLegalEntityRepository>();
        services.AddTransient<IAccountProviderRepository, AccountProviderRepository>();
        services.AddTransient<IProviderRepository, ProviderRepository>();
        services.AddTransient<IJobAuditRepository, JobAuditRepository>();
        services.AddTransient<IAccountRepository, AccountRepository>();
        services.AddTransient<IPermissionAuditRepository, PermissionAuditRepository>();
    }
}
