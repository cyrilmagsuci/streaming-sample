using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Hosting;
using Akka.Streams;

namespace KinesisProducer
{
    /// <summary>
    /// <see cref="IHostedService"/> that runs and manages <see cref="ActorSystem"/> in background of application.
    /// </summary>
    public class AkkaService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;

        private readonly ActorSystem _system;
        private readonly IMaterializer _materializer;
        private const string ActorSystemName = "FireAlert";

        private const string Sensors = "30.667222,-97.643056,d123"; //"30.667222,-97.643056,d123;32.15,-97.933333,d2457;31.159167,-106.288611,d0iu";
        // ACTORSYSTEM: "FireAlert"
        // streamName: "fire-alert"
        // CLUSTER_SEEDS: "[akka.tcp://FireAlert@light-house-1:4053,akka.tcp://FireAlert@light-house-2:4054]"
        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = appLifetime;
            var vrs = Environment.GetEnvironmentVariables();
            var sensors = (vrs["sensors"]?.ToString() ?? Sensors).Split(";").ToList();
            
            
            var config = ConfigurationFactory.ParseString(File.ReadAllText("producer.hocon"));

            _system = ActorSystem.Create((vrs["ACTORSYSTEM"]?.ToString() ?? ActorSystemName), config);
            
            _materializer = _system.Materializer();
            foreach(var sensor in sensors)
            {
                _system.ActorOf(SensorActor.Prop(sensor, _materializer));
            }

            _system.WhenTerminated.ContinueWith(tr =>
            {
                _applicationLifetime.StopApplication();
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // strictly speaking this may not be necessary - terminating the ActorSystem would also work
            // but this call guarantees that the shutdown of the cluster is graceful regardless
            await _system.Terminate();
        }
    }

}
