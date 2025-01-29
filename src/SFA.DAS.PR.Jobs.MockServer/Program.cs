using SFA.DAS.PR.Jobs.MockServer;

var commitmentsServer = CommitmentsV2ApiMockServer.Run();

Console.WriteLine(("Press any key to stop the server..."));
Console.ReadKey();

commitmentsServer.Stop();
