﻿using AutoFixture;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

public static class NotificationData
{
    public static Notification Create(Guid notificationId, NotificationType notificationType, long ukprn, long accountLegalEntityId, string templateName)
        => TestHelpers.CreateFixture()
            .Build<Notification>()
            .With(a => a.Id, notificationId)
            .With(a => a.NotificationType, notificationType.ToString())
            .With(a => a.SentTime, (DateTime?)null)
            .With(a => a.TemplateName, templateName)
            .With(a => a.Ukprn, ukprn)
            .With(a => a.AccountLegalEntityId, accountLegalEntityId)
         .Create();
}
