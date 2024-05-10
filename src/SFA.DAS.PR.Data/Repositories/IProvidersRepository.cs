
namespace SFA.DAS.PR.Data.Repositories;

public interface IProvidersRepository
{
    Task<int> GetCount(CancellationToken cancellationToken);
}