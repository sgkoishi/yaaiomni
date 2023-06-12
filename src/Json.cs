using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Chireiden.TShock.Omni.Json;

public class OptionalSerializeContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        if (property.PropertyType?.IsGenericType == true && typeof(Optional<>) == property.PropertyType.GetGenericTypeDefinition())
        {
            property.ShouldSerialize = value => property.ValueProvider?.GetValue(value) is Optional o ? !o.IsDefaultValue() : true;
        }
        return property;
    }
}

public class OptionalConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType && typeof(Optional<>) == objectType.GetGenericTypeDefinition();
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else if (value is Optional o)
        {
            writer.WriteValue(o.ObjectValue);
        }
        else
        {
            throw new Exception("Unreachable code: WriteJson is not a Optional");
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var type = objectType.GenericTypeArguments[0];
        var obj = serializer.Deserialize(reader, type);
        if (existingValue is Optional o)
        {
            o.ObjectValue = obj!;
        }
        else
        {
            throw new Exception("Unreachable code: WriteJson is not a Optional");
        }
        return existingValue;
    }
}

public class LimiterConverter : JsonConverter<Config.LimiterConfig>
{
    public override void WriteJson(JsonWriter writer, Config.LimiterConfig? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteValue("0/1");
        }
        else
        {
            var str = Math.Round(value.RateLimit) == value.RateLimit ? $"{(int) value.RateLimit}" : $"{value.RateLimit}";
            str += "/";
            str += Math.Round(value.Maximum) == value.Maximum ? $"{(int) value.Maximum}" : $"{value.Maximum}";
            writer.WriteValue(str);
        }
    }

    public override Config.LimiterConfig ReadJson(JsonReader reader, Type objectType, Config.LimiterConfig? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var s = reader?.Value?.ToString() ?? "0/1";
        var split = s.Split('/');
        return new Config.LimiterConfig
        {
            RateLimit = double.Parse(split[0]),
            Maximum = double.Parse(split[1])
        };
    }
}