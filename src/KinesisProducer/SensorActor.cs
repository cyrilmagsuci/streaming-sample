// using Akka.Actor;
// using Akka.Event;
// using Akka.Streams;
// using Akka.Streams.Dsl;
// using Shared;
// using System.Text;
// using System.Text.Json;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Akka;
// using Akka.Actor;
// using Akka.Configuration;
// using Akka.Streams;
// using Akka.Streams.Dsl;
// using Akka.Streams.Kafka.Dsl;
// using Akka.Streams.Kafka.Messages;
// using Akka.Streams.Kafka.Settings;
// using Confluent.Kafka;
// using Config = Akka.Configuration.Config;
//
// namespace KinesisProducer
// {
//     public class SensorActor:ReceiveActor
//     {
//         private ICancelable _job;
//         private readonly SensorSetting _setting;
//         private readonly Random _random;
//         // private readonly Func<IAmazonKinesis> _clientFactory;
//         private IActorRef _source;
//         private readonly ILoggingAdapter _log;
//         private const string ActorSystemName = "FireAlert";
//         
//         private const string KafaTopic = "fire-alerted";
//
//         public SensorActor(string sensor, IMaterializer materializer)
//         {
//             _log = Context.GetLogger();
//             // _clientFactory = () => new AmazonKinesisClient(
//             //     new BasicAWSCredentials(Environment.GetEnvironmentVariable("accessKey"),
//             //     Environment.GetEnvironmentVariable("accessSecret")),
//             //     RegionEndpoint.USEast1);
//
//             _random = new Random();
//             var sensorInfo = sensor.Split(",");
//             _setting = new SensorSetting
//             {
//                 SensorName = sensorInfo[2],
//                 Latitude = double.Parse(sensorInfo[0]),
//                 Longitude = double.Parse(sensorInfo[1]),
//             };
//             // _source = Source.ActorRef<SensorData>(1000, OverflowStrategy.Fail)
//             //  .Select(data => new PutRecordsRequestEntry
//             //  {
//             //      PartitionKey = _random.Next().ToString(),
//             //      Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)))
//             //  })
//             //  .Via(KinesisFlow.Create(Environment.GetEnvironmentVariable("streamName"), KinesisFlowSettings.Default, _clientFactory))
//             //  .To(Sink.ActorRef<PutRecordsResultEntry>(Self, "done"))
//             //  .Run(materializer);
//             //
//             //
//             
//             // Config fallbackConfig = ConfigurationFactory.ParseString(@"
//             //         akka.suppress-json-serializer-warning=true
//             //         akka.loglevel = DEBUG
//             //         akka.kafka.default-dispatcher {
//             //           type = Dispatcher
//             //           executor = ""fork-join-executor""
//             //           fork-join-executor {
//             //             parallelism-min = 2
//             //             parallelism-factor = 2.0
//             //             parallelism-max = 10
//             //           }
//             //           throughput = 100
//             //         }
//             //     ").WithFallback(ConfigurationFactory.FromResource<ConsumerSettings<object, object>>("Akka.Streams.Kafka.reference.conf"));
//
//            // var system = ActorSystem.Create(ActorSystemName, fallbackConfig);
//
//             var system = Context.System;
//            var producerConfig = ConfigurationFactory.ParseString(File.ReadAllText("producer.hocon"));
//             
//             // var producerSettings1 = ProducerSettings<Null, SensorData>.Create(producerConfig, null, null)
//             //     .WithBootstrapServers("localhost:19092");
//             
//             
//             
//             //var producerSettings = ProducerSettings<Null, string>.Create(system, null, null)
//               //  .WithBootstrapServers("localhost:9092");
//
// // OR you can use Config instance
//             var config = producerConfig.GetConfig("akka.kafka.producer");
//             var producerSettings = ProducerSettings<Null, SensorData>.Create(config, null, null)
//                 .WithBootstrapServers("localhost:19092");
//             
//           //   var config = new ProducerConfig
//           //   {
//           //       BootstrapServers = "localhost:19092",
//           //       ClientId = "client1", 
//           //       EnableIdempotence = true
//           //   };
//           // //  var producerSettings1 = ProducerSettings<Null, SensorData>.Create(config, null, null);
//           //   var producerSettings = ProducerSettings<Null, SensorData>.Create(system, null, null)
//           //       .WithProducerConfig(config);
// //
// // // OR you can use Config instance
// //             var config = system.Settings.Config.GetConfig("akka.kafka.producer");
// //             var producerSettings = ProducerSettings<Null, string>.Create(config, null, null)
// //                 .WithBootstrapServers("localhost:9092");
// //             
//             // var producerSettings4 = ProducerSettings<Null, SensorData>.Create(system, null, null)
//             //     .WithBootstrapServers($"localhost:19092")
//             //     .WithProperties(new Dictionary<string, string>
//             //     {
//             //         {"security.protocol", "SASL_SSL"},
//             //         {"sasl.mechanism", "SCRAM-SHA-512"},
//             //         {"sasl.username", "dev"},
//             //         {"sasl.password", "0m3lswFjUKHsgX0iavl7gSmf2t46x8"},
//             //     });
//
//             Source
//                 .From(Enumerable.Range(1, 100))
//                 .Select(c => new SensorData
//                 {
//                     SensorName = "sensor1",
//                     SensorId = "sensor1",
//                     Coordinate = new Coordinate
//                     {
//                         Latitude = 1.0,
//                         Longitude = 1.0
//                     },
//                     Reading = _random.Next(75, 1000)
//                 })
//                 .Select(elem => new ProducerRecord<Null, SensorData>(KafaTopic, elem))
//                 .RunWith(KafkaProducer.PlainSink(producerSettings), materializer);
//             
//             _source = Source.ActorRef<SensorData>(1000, OverflowStrategy.Fail)
//                 .Select(elem => ProducerMessage.Single(new ProducerRecord<Null, SensorData>(KafaTopic, elem)))
//                 .Via(KafkaProducer.FlexiFlow<Null, SensorData, NotUsed>(producerSettings))
//                 .To(Sink.ActorRef<IResults<Null, SensorData, NotUsed>>(Self, "done", exception => exception.ToString()))
//                 .Run(materializer);
//
//             var source = Source
//                 .Cycle(() => Enumerable.Range(1, 100).GetEnumerator())
//                 .Select(elem =>  new SensorData
//                     {
//                         SensorName = "sensor1",
//                         SensorId = "sensor1",
//                         Coordinate = new Coordinate
//                         {
//                             Latitude = 1.0,
//                             Longitude = 1.0
//                         },
//                         Reading = _random.Next(75, 1000)
//                     }
//                 )
//                 .Select(elem => ProducerMessage.Single(new ProducerRecord<Null, SensorData>("akka100", elem)))
//                 .Via(KafkaProducer.FlexiFlow<Null, SensorData, NotUsed>(producerSettings))
//                 .Select(result =>
//                 {
//                     var response = result as Result<Null, SensorData, NotUsed>;
//                     Console.WriteLine($"Producer: {response.Metadata.Topic}/{response.Metadata.Partition} {response.Metadata.Offset}: {response.Metadata.Value}");
//                     return result;
//                 })
//                 .To(Sink.ActorRef<IResults<Null, SensorData, NotUsed>>(Self, "done", exception => exception.ToString()))
//                 .Run(materializer);
//             
//             _job = Context.System.Scheduler.Advanced
//                 .ScheduleRepeatedlyCancelable(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1),
//                 () =>
//                 {
//                     var sensorData = new SensorData
//                     {
//                         SensorName = _setting.SensorName,
//                         SensorId = _setting.SensorId,
//                         Coordinate = new Coordinate
//                         {
//                             Latitude = _setting.Latitude,
//                             Longitude = _setting.Longitude
//                         },
//                         Reading = _random.Next(75, 1000)
//                     };
//                     _source.Tell(sensorData);
//                 });
//             Receive<Result<Null, SensorData, NotUsed>>(re =>
//             {
//                 _log.Info(JsonSerializer.Serialize(re));
//             });
//         }
//         public static Props Prop(string sensor, IMaterializer materializer)
//         {
//             return Props.Create(()=> new SensorActor(sensor, materializer));
//         }
//         protected override void PreStart()
//         {
//             base.PreStart();
//         }
//         protected override void PostStop()
//         {
//             _job?.Cancel();
//             base.PostStop();
//         }
//     }
// }
