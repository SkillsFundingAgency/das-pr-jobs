using SFA.DAS.PR.Jobs.MockServer;

var recruitServer = RecruitApiMockServer.Run();
var commitmentsServer = CommitmentsV2ApiMockServer.Run();

Console.WriteLine(("Press any key to stop the server..."));
Console.ReadKey();

recruitServer.Stop();
commitmentsServer.Stop();
