using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Configuration;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using Akka.DependencyInjection;
// using Amazon.Kinesis;
// using Akka.Streams.Kinesis;
using Akka.Streams;
// using Amazon.Runtime;
// using Amazon;
using Akka.Streams.Dsl;
using System.Text.Json;
using Shared;
using System.Text;
// using Amazon.Kinesis.Model;
using System.Linq;
using Akka.Streams.Kafka.Settings;
using Akka.Util.Internal;
using Confluent.Kafka;

namespace KinesisSample
{
    /// <summary>
    /// <see cref="IHostedService"/> that runs and manages <see cref="ActorSystem"/> in background of application.
    /// </summary>
    public class AkkaService : IHostedService
    {
        private ActorSystem ClusterSystem;
        private readonly IServiceProvider _serviceProvider;
        private static readonly AtomicCounter WorkerId = new AtomicCounter();
        private int _partitions = 2;
        private const string KafkaTopic = "fire-alerted";
        
        private readonly IHostApplicationLifetime _applicationLifetime;
        private string _streamName;
       // Func<IAmazonKinesis> _clientFactory;
        private IMaterializer _materializer;
        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = appLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var clusterSeeds = Environment.GetEnvironmentVariable("CLUSTER_SEEDS")?.Trim();
            var clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("app.conf"));
            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes").ToList();
            if (!string.IsNullOrEmpty(clusterSeeds))
            {
                var tempSeeds = clusterSeeds.Trim('[', ']').Split(',').ToList();
                if (tempSeeds.Any())
                {
                    seeds = tempSeeds;
                }
            }
            var injectedClusterConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [", (current, seed) => current + @"""" + seed + @""", ");
            injectedClusterConfigString += "]";
            var config = clusterConfig
                .WithFallback(ConfigurationFactory.ParseString(injectedClusterConfigString));
                //.BootstrapFromDocker();
            var bootstrap = BootstrapSetup.Create()
               .WithConfig(config) // load HOCON
               .WithActorRefProvider(ProviderSelection.Cluster.Instance); // launch Akka.Cluster

            // N.B. `WithActorRefProvider` isn't actually needed here - the HOCON file already specifies Akka.Cluster

            // enable DI support inside this ActorSystem, if needed
            var diSetup = DependencyResolverSetup.Create(_serviceProvider);

            // merge this setup (and any others) together into ActorSystemSetup
            var actorSystemSetup = bootstrap.And(diSetup);

            // start ActorSystem
            ClusterSystem = ActorSystem.Create("FireAlert", actorSystemSetup);
            _materializer = ClusterSystem.Materializer();

            // use the ServiceProvider ActorSystem Extension to start DI'd actors
            var sp = DependencyResolver.For(ClusterSystem);
            // add a continuation task that will guarantee 
            // shutdown of application if ActorSystem terminates first
            ClusterSystem.WhenTerminated.ContinueWith(tr =>
            {
                _applicationLifetime.StopApplication();
            });

            var vrs = Environment.GetEnvironmentVariables();
           
            var aggregator = ClusterSystem.ActorOf(AggregatorActor.Prop(ClusterSystem));
            
            var consumerSettings = ConsumerSettings<Null, SensorData>.Create(ClusterSystem, null, new SensorDataDeserializer())
                .WithBootstrapServers("localhost:19092")
                .WithGroupId("group-1")
                .WithProperty("security.protocol", "PLAINTEXT")
                .WithProperty("session.timeout.ms", "6000");
            var subscription = Subscriptions.Topics(KafkaTopic);
            
            for (var i = 0; i < _partitions; i++)
            {
                var id = WorkerId.IncrementAndGet();
                ClusterSystem.ActorOf(ConsumerWorkerActor.Props(ClusterSystem, aggregator, consumerSettings, subscription, _materializer), $"worker-{id}");
            }
            
    
        //    ClusterSystem.ActorOf(KafkaConsumerSupervisor<string, string>.Props(consumerSettings, subscription, 3), "kafka");

            
            // foreach(var shard in shards)
            // {
            //
            //     ClusterSystem.ActorOf(Props.Create(() => new ShardActor(aggregator, shard, _clientFactory, _streamName, _materializer)));
            // }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // strictly speaking this may not be necessary - terminating the ActorSystem would also work
            // but this call guarantees that the shutdown of the cluster is graceful regardless
            await CoordinatedShutdown.Get(ClusterSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }

}
