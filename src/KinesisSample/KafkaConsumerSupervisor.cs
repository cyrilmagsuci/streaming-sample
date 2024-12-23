// using System;
// using Akka.Actor;
// using Akka.Streams.Kafka.Settings;
// using Akka.Util.Internal;
// using Confluent.Kafka;
// using Shared;
//
// namespace KinesisSample
// {
//     public class KafkaConsumerSupervisor: ReceiveActor
//     {
//         private static readonly AtomicCounter WorkerId = new AtomicCounter();
//         public static Props Props(IActorRef aggregator, ConsumerSettings<Null, SensorData> settings, ISubscription subscription, int partitions)
//             => Akka.Actor.Props.Create(() => new KafkaConsumerSupervisor(settings, subscription, partitions));
//     
//         private readonly ConsumerSettings<Null, SensorData> _settings;
//         private readonly ISubscription _subscription;
//         private readonly int _partitions;
//
//         public KafkaConsumerSupervisor(ConsumerSettings<Null, SensorData> settings, ISubscription subscription, int partitions)
//         {
//             _settings = settings;
//             _subscription = subscription;
//             _partitions = partitions;
//         }
//
//         protected override SupervisorStrategy SupervisorStrategy()
//             => new OneForOneStrategy(ex =>
//             {
//                 return ex switch
//                 {
//                     Exception { Message: "BOOM!" } => Directive.Restart,
//                     _ => Directive.Escalate
//                 };
//             });
//
//         protected override void PreStart()
//         {
//             base.PreStart();
//             for (var i = 0; i < _partitions; i++)
//             {
//                 var id = WorkerId.IncrementAndGet();
//                 Context.ActorOf(ConsumerWorkerActor.Props(_settings, _subscription), $"worker-{id}");
//             }
//         }
//     }    
// }
