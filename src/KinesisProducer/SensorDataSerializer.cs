using System.Text.Json;
using Confluent.Kafka;
using Shared;

public class SensorDataSerializer : ISerializer<SensorData>
{
    public byte[] Serialize(SensorData data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}