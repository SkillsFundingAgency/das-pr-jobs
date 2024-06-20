using Microsoft.Extensions.Hosting;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Jobs.Functions;
namespace SFA.DAS.PR.Jobs.MessageHandlers.TestHarness;

public class RaiseEventService(IMessageSession _messageSession, IHostApplicationLifetime _applicationLifetime) : IHostedService
{
    private readonly List<string> EventNames =
    [
        "CreatedAccountEvent",
        "ChangedAccountNameEvent",
        "AddedLegalEntityEvent",
        "UpdatedLegalEntityEvent",
        "RemovedLegalEntityEvent",
        "HelloWorldEvent"
    ];


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var input = GetEventName();
            if (input.Equals("x", StringComparison.CurrentCultureIgnoreCase))
            {
                _applicationLifetime.StopApplication();
                return;
            }
            if (EventNames.Select(e => e.ToLower()).Contains(input.ToLower()))
            {
                await RaiseEvent(input);
            }
        }
    }

    public async Task RaiseEvent(string input)
    {
        var userRef = Guid.NewGuid();
        const string userName = "Bob Loblaw";
        const long accountId = 10;
        const string accountPublicHashedId = "ACCPUB";
        const string originalAccountName = "Account Name";
        const string updatedAccountName = "New Account Name";
        const long legalEntityId = 20;
        const long accountLegalEntityId = 1020;
        const string accountLegalEntityPublicHashedId = "ALEPUB";
        const string originalAccountLegalEntityName = "Legal Entity";
        const string updatedAccountLegalEntityName = "New Legal Entity";
        const string accountHashedId = "AHEAHE";

        object? eventToRaise = input switch
        {
            "HelloWorldEvent" => new HelloWorldEvent(""),
            "CreatedAccountEvent" => new CreatedAccountEvent
            {
                AccountId = accountId,
                PublicHashedId = accountPublicHashedId,
                Name = originalAccountName,
                UserName = userName,
                UserRef = userRef,
                Created = DateTime.UtcNow,
                HashedId = accountHashedId
            },
            "ChangedAccountNameEvent" => new ChangedAccountNameEvent
            {
                AccountId = accountId,
                PreviousName = originalAccountName,
                CurrentName = updatedAccountName,
                UserName = userName,
                UserRef = userRef,
                Created = DateTime.UtcNow,
                HashedAccountId = accountPublicHashedId
            },
            "AddedLegalEntityEvent" => new AddedLegalEntityEvent
            {
                AccountLegalEntityId = accountLegalEntityId,
                AccountLegalEntityPublicHashedId = accountLegalEntityPublicHashedId,
                OrganisationName = originalAccountLegalEntityName,
                AccountId = accountId,
                LegalEntityId = legalEntityId,
                AgreementId = 2,
                UserName = userName,
                UserRef = userRef,
                Created = DateTime.UtcNow
            },
            "UpdatedLegalEntityEvent" => new UpdatedLegalEntityEvent
            {
                AccountLegalEntityId = accountLegalEntityId,
                Name = updatedAccountLegalEntityName,
                Address = "New LE Address",
                UserName = userName,
                UserRef = userRef,
                Created = DateTime.UtcNow
            },
            "RemovedLegalEntityEvent" => new RemovedLegalEntityEvent
            {
                AccountLegalEntityId = accountLegalEntityId,
                OrganisationName = updatedAccountLegalEntityName,
                AccountId = accountId,
                LegalEntityId = legalEntityId,
                AgreementId = 2,
                AgreementSigned = true,
                UserName = userName,
                UserRef = userRef,
                Created = DateTime.UtcNow
            },
            _ => null
        };
        if (eventToRaise != null)
        {
            await _messageSession.Publish(eventToRaise);
            Console.WriteLine($"{input} message sent");
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Empty);
        }
    }

    private string GetEventName()
    {
        Console.WriteLine("Event Names:");
        EventNames.ForEach(Console.WriteLine);
        Console.WriteLine(string.Empty);
        Console.WriteLine("Enter `x` or `Ctrl+C` to exit");
        Console.Write("Enter name of the event to raise: ");
        var key = Console.ReadLine();
        Console.WriteLine($"You have selected {key}");
        return key ?? "x";
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
