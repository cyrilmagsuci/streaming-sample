using System;
using System.Text.Json;
using Confluent.Kafka;
using Shared;

public class SensorDataDeserializer : IDeserializer<SensorData>
{
    public SensorData Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<SensorData>(data);
    }
}