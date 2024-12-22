using ClusterLightHouse;
var par = args.ToList();

string ActorSystemName = "FireAlert";

var lighthouseService = new LighthouseService(actorSystemName: Environment.GetEnvironmentVariable("ACTORSYSTEM") ?? ActorSystemName);
lighthouseService.Start();

Console.WriteLine("Press Control + C to terminate.");
Console.CancelKeyPress += async (sender, eventArgs) =>
{
    await lighthouseService.StopAsync();
};
lighthouseService.TerminationHandle.Wait();