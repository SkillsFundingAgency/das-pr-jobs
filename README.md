## ⛔Never push sensitive information such as client id's, secrets or keys into repositories including in the README file⛔

# Provider Relationships Jobs

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_apis/build/status%2Fdas-pr-jobs?repoName=SkillsFundingAgency%2Fdas-pr-jobs&branchName=main)](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_build/latest?definitionId=3710&repoName=SkillsFundingAgency%2Fdas-pr-jobs&branchName=main)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SkillsFundingAgency_das-pr-jobs&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=SkillsFundingAgency_das-pr-jobs)
[![Confluence Project](https://img.shields.io/badge/Confluence-Project-blue)](https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/4368171030/Solution+Architecture+-+PR#Initial-view-of-solution-architecture-for--EP%2FPP)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)

## About 
The PR jobs repository defines functions that are required to maintain the provider relationships database. Here we have a mix of event handlers and functions: 

### Event Handlers
#### Employer Accounts
This function app is listening to following events raised from employer accounts to synchronise the account and legal entity data in the PR database. 
- `AddedLegalEntityEvent`
- `ChangedAccountNameEvent`
- `CreatedAccountEvent`
- `RemovedLegalEntityEvent`
- `UpdatedLegalEntityEvent`

#### Recruit
The function app is also listening to `VacancyApprovedEvent` to establish relationship between the provider and employer that are part of the vacancy published. 

#### Approvals
The function app is also subscribed to `CohortAssignedToProviderEvent` to establish relationship between the provider and employer.

### Functions
- `SendNotificationsFunction` is triggered every 5 minutes in production. It looks for all the pending notifications and raises `SendEmailCommand` for notifications api to pick up. 
- `NotificationsCleanUpFunction` is triggered once a day to delete older notification records. This is necessary as notifications table can get huge very quickly and older notification records are of no use analytically. 
- `ExpiredRequestsFunction` is triggered once a day, goes through all the pending requests that were created before a given number of days and updates their status to expired. 
- `UpdateProvidersFunction` is triggered once a day to synchronise with the local cache of providers list with ROATP

## How It Works
The jobs project has direct access to the PR database, the definition of which is in `das-pr-api` repository. It connects to the NServiceBus instance so it can listen to the global events.

To test event handlers, run the `SFA.DAS.PR.Jobs.MessageHandlers.TestHarness`. This is a command line tool and it allows you to raise events which the jobs has subscribed to. 

Optionally if the dependency APIs are not running locally, then fire up `SFA.DAS.PR.Jobs.MockServer` which can mimic responses from inner apis. 

`PingFunction` along with `HelloWorldEventHandler` allows you to test the local setup, invoke this to make sure that the NServiceBus event handlers and message receivers are correctly setup. 


## 🚀 Installation

### Pre-Requisites

* Clone this repository
* Clone `das-pr-api` repository
* An Azure storage emulator
* An Azure Service Bus instance

### Dependencies
- Roatp API `das-roatp-service` to update the local cache of providers
- PAS API `das-providerapprenticeshipservice`to send notifications to provider users
- Commitments V2 API `das-commimtments` to get employer and provider details with regards to Cohort
- Employer Accounts API `das-employer-accounts` to get employer account and legal entities details
- NServiceBus to send notifications to employers and listen to global events

### Config
* Create a Configuration table in your (Development) local storage account.
* Obtain the [SFA.DAS.PR.Jobs.json](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-pr-jobs/SFA.DAS.PR.Jobs.json) from the `das-employer-config` and adjust the `SqlConnectionString` property to match your local setup.
* Add a row to the Configuration table with fields:
  * PartitionKey: LOCAL
  * RowKey: SFA.DAS.Roatp.Api_1.0
  * Data: {The contents of the SFA.DAS.PR.Jobs.json file}
* In `SFA.DAS.PR.Jobs` project add `local.settings.json` file with following content:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true;",
    "ConfigNames": "SFA.DAS.PR.Jobs",
    "EnvironmentName": "LOCAL",
    "AzureWebJobsServiceBus": "",
    "NotificationsCleanUpFunctionSchedule": "0 0 0 * * *",
    "UpdateProvidersFunctionSchedule": "0 0/3 * * * *",
    "SendNotificationsFunctionSchedule": "0 */5 * * * *",
    "ExpiredRequestsFunctionSchedule": "0 0 0 * * *"
  }
}
```

To raise and listen to the events, it is required that you add your service bus instance connection string to `AzureWebJobsServiceBus` setting above. 

The timer triggered functions schedule is required to be local settings as isolated model cannot read from custom configuration sources. 

All the functions can be disabled by adding following to the local settings:
```json
    "AzureWebJobs.UpdateProvidersFunction.Disabled": "true",
    "AzureWebJobs.SendNotificationsFunction.Disabled": "true",
    "AzureWebJobs.NotificationsCleanUpFunction.Disabled": "true",
    "AzureWebJobs.ExpiredRequestsFunction.Disabled": "true"
```

## Technologies
* .NetCore 8
* Azure Functions V4
* NServiceBus
* Azure Table Storage
* NUnit
* Moq
* FluentAssertions
