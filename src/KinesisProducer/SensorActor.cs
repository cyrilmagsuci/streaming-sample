using System.IO;
using System.Text.Json;
using Akka;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Kafka.Dsl;
using Akka.Streams.Kafka.Messages;
using Akka.Streams.Kafka.Settings;
using Confluent.Kafka;
using Shared;

namespace KinesisProducer
{
    public class SensorActor : ReceiveActor
    {
        private ICancelable _job;
        private readonly SensorSetting _setting;
        private readonly Random _random;
        private IActorRef _source;
        private readonly ILoggingAdapter _log;
        private const string ActorSystemName = "FireAlert";
        private const string KafkaTopic = "fire-alerted";

        public SensorActor(string sensor, IMaterializer materializer)
        {
            _log = Context.GetLogger();
            _random = new Random();
            var sensorInfo = sensor.Split(",");
            _setting = new SensorSetting
            {
                SensorName = sensorInfo[2],
                Latitude = double.Parse(sensorInfo[0]),
                Longitude = double.Parse(sensorInfo[1]),
            };

            var system = Context.System;

            var producerSettings = ProducerSettings<Null, SensorData>.Create(system, null, new SensorDataSerializer())
                .WithBootstrapServers("localhost:19092")
                .WithProperty("security.protocol", "PLAINTEXT");
                // .WithProperties(new Dictionary<string, string>
                //  {
                //      {"security.protocol", "SASL_SSL"},
                //      {"sasl.mechanism", "SCRAM-SHA-512"},
                //      {"sasl.username", "dev"},
                //      {"sasl.password", "0m3lswFjUKHsgX0iavl7gSmf2t46x8"},
                //  });
   
            _source = Source.ActorRef<SensorData>(1000, OverflowStrategy.Fail)
                .Select(elem => ProducerMessage.Single(new ProducerRecord<Null, SensorData>(KafkaTopic, elem)))
                .Via(KafkaProducer.FlexiFlow<Null, SensorData, NotUsed>(producerSettings))
                .To(Sink.ActorRef<IResults<Null, SensorData, NotUsed>>(Self, "done", exception => exception.ToString()))
                .Run(materializer);

            _job = Context.System.Scheduler.Advanced
                .ScheduleRepeatedlyCancelable(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1),
                () =>
                {
                    var sensorData = new SensorData
                    {
                        SensorName = _setting.SensorName,
                        SensorId = _setting.SensorId,
                        Coordinate = new Coordinate
                        {
                            Latitude = _setting.Latitude,
                            Longitude = _setting.Longitude
                        },
                        Reading = _random.Next(75, 1000)
                    };
                    _source.Tell(sensorData);
                });

            Receive<Result<Null, SensorData, NotUsed>>(re =>
            {
                _log.Info(JsonSerializer.Serialize(re));
            });
        }

        public static Props Prop(string sensor, IMaterializer materializer)
        {
            return Props.Create(() => new SensorActor(sensor, materializer));
        }

        protected override void PreStart()
        {
            base.PreStart();
        }

        protected override void PostStop()
        {
            _job?.Cancel();
            base.PostStop();
        }
    }
}