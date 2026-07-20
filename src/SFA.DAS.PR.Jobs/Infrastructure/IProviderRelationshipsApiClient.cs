using Refit;
using SFA.DAS.PR.Jobs.OuterApi.Requests;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IProviderRelationshipsApiClient
{
    [Delete("/permissions")]
    Task RemovePermission([Query] RemovePermissionsRequest request, CancellationToken cancellationToken = default);
}
