using System;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Kafka.Dsl;
using Akka.Streams.Kafka.Helpers;
using Akka.Streams.Kafka.Messages;
using Akka.Streams.Kafka.Settings;
using Confluent.Kafka;
using Shared;

namespace KinesisSample
{
    public class ConsumerWorkerActor: ReceiveActor
    {
        public static Props Props(ActorSystem clusterSystem, IActorRef aggregator, ConsumerSettings<Null, SensorData> settings, ISubscription subscription, IMaterializer materializer)
            => Akka.Actor.Props.Create(() => new ConsumerWorkerActor(clusterSystem, aggregator, settings, subscription, materializer));
        
        private readonly ILoggingAdapter _log;
        private readonly ConsumerSettings<Null, SensorData> _settings;
        private readonly ISubscription _subscription;
        private readonly IMaterializer _materializer;
        private DrainingControl<NotUsed> _control;
        private readonly Random _rnd = new Random();
        private readonly ActorSystem _clusterSystem;
        private IActorRef _aggregator;
        
        public ConsumerWorkerActor(ActorSystem clusterSystem, IActorRef aggregator, ConsumerSettings<Null, SensorData> settings, ISubscription subscription, IMaterializer materializer)
        {
            _clusterSystem = clusterSystem;
            _aggregator = aggregator;
            _settings = settings;
            _subscription = subscription;
            _materializer = materializer;
            _log = Context.GetLogger();

            Receive<CommittableMessage<Null, SensorData>>(msg =>
            {
                if (_rnd.Next(0, 100) == 0)
                    throw new Exception("BOOM!");
                
                var record = msg.Record;
                _log.Info($"{record.Topic}[{record.Partition}][{record.Offset}]: {record.Message.Value}");
                Sender.Tell(msg.CommitableOffset);
                
                //var sdata = JsonSerializer.Deserialize<SensorData>(Encoding.UTF8.GetString(record.Message.Value));
                SensorData sdata = record.Message.Value;
                _aggregator.Tell(sdata);
            });
        }

        protected override void PostRestart(Exception reason)
        {
            base.PostRestart(reason);
            _log.Info("Worker restarted");
        }

        protected override void PreStart()
        {
            base.PreStart();
            
            var committerDefaults = CommitterSettings.Create(_clusterSystem) // CommitterSettings.Create(Context.System)
                .WithMaxInterval(TimeSpan.FromMilliseconds(500));
            
            _control = KafkaConsumer.CommittableSource(_settings, _subscription)
                .Ask<ICommittable>(Self, TimeSpan.FromSeconds(1), 1)
                .ToMaterialized(Committer.Sink(committerDefaults), DrainingControl<NotUsed>.Create)
                .Run(_materializer);
            _log.Info("Worker started");
        }

        protected override void PostStop()
        {
            base.PostStop();
            _control?.Shutdown().Wait();
            _log.Info("Worker stopped");
        }
    }    
}