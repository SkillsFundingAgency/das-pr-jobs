## ⛔Never push sensitive information such as client id's, secrets or keys into repositories including in the README file⛔

# Provider Relationships Jobs

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://sfa-gov-uk.visualstudio.com/Digital%20Apprenticeship%20Service/_apis/build/status%2Fdas-pr-jobs?repoName=SkillsFundingAgency%2Fdas-pr-jobs&branchName=main)](https://sfa-gov-uk.visualstudio.com/Digital%20Apprenticeship%20Service/_build/latest?definitionId=3710&repoName=SkillsFundingAgency%2Fdas-pr-jobs&branchName=main)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SkillsFundingAgency_das-pr-jobs&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=SkillsFundingAgency_das-pr-jobs)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)

## About

This service contains the background processing for Provider Relationships.

It runs a set of scheduled Azure Functions and NServiceBus message handlers to:

- Keep provider data in sync
- Process and expire outstanding relationship requests
- Queue and send provider/employer notifications
- Clean up old notification data

## 🚀 Installation

### Pre-Requisites

* A clone of this repository
* Visual Studio or similar IDE
* .NET 10 
* A storage emulator (for example Azurite)
* SQL Server
* Access to Azure Service Bus
* Access to required API/config resources in your target environment
* NServiceBus trial license (for local development) - [Get a trial license](https://particular.net/)
* Firewall rules for ports 5671 and 5672 should open on development machine to allow connection to Azure Service Bus, raise an IT request with network security team

### Dependencies

* Azure Functions (isolated worker)
* NServiceBus.AzureFunctions.Worker.ServiceBus
* Azure Table Storage configuration (SFA.DAS.Configuration.AzureTableStorage)
* SQL Server database used by `SFA.DAS.PR.Data`
* APIs:
  * ROATP Service API : https://github.com/SkillsFundingAgency/das-roatp-service
  * PAS Account API : 
  * Commitments V2 API : https://github.com/SkillsFundingAgency/das-commitments
  * Employer Accounts API : https://github.com/SkillsFundingAgency/das-employer-accounts

### Config
* Create a Configuration table in your (Development) local storage account.
* Add a row to the Configuration table with fields:
  * PartitionKey: LOCAL
  * RowKey: SFA.DAS.PR.Jobs_1.0
  * Data: {The contents of the [SFA.DAS.PR.Jobs.json](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-pr-jobs/SFA.DAS.PR.Jobs.json) Obtain from the `das-employer-config` for `das-pr-jobs` repo}
  * Update the `SqlConnectionString` property to match your local setup.
  * Update `NServiceBusConfiguration` section of the config `NServiceBusLicense` with NServiceBus trial license string. 

* Following config files are required to be loaded in local storage emulator
  * [SFA.DAS.PR.Jobs.json](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-pr-jobs/SFA.DAS.PR.Jobs.json)
  * [SFA.DAS.Encoding](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-shared-config/SFA.DAS.Encoding.json)

* In the SFA.DAS.PR.Jobs project, add `local.settings.json` file with following content:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true;",
    "ConfigNames": "SFA.DAS.PR.Jobs,SFA.DAS.Encoding",
    "EnvironmentName": "LOCAL",
    "AzureWebJobsServiceBus": "<service-bus-connection-string>",
    "ExpiredRequestsFunctionSchedule": "0 3 * * *",
    "NotificationsCleanUpFunctionSchedule": "0 3 * * * *",
    "UpdateProvidersFunctionSchedule": "0 0/3 * * * *",
    "SendNotificationsFunctionSchedule": "0 */5 * * * *",
    "AzureWebJobs.UpdateProvidersFunction.Disabled": "true",
    "AzureWebJobs.SendNotificationsFunction.Disabled": "true"
  }
}
```

## Technologies

* .Net 10
* Azure Functions V4 (isolated worker)
* NServiceBus
* Entity Framework Core
* Azure Table Storage
* NUnit
* Moq
* FluentAssertions
