using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using static Chireiden.TShock.Omni.Config;
using static Chireiden.TShock.Omni.Config.DebugPacketSettings;

namespace Chireiden.TShock.Omni.Json;


public static class JsonUtils
{
    private static readonly List<JsonConverter> _jsonconverters = new List<JsonConverter>
    {
        new OptionalConverter(),
        new LimiterConverter(),
        new StringEnumConverter(),
        new PacketFilterConverter(),
    };

    public static T DeserializeConfig<T>(string value)
    {
        var jss = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = _jsonconverters,
            ContractResolver = new CustomContractResolver { WarnExtensionData = true },
        };

        return JsonConvert.DeserializeObject<T>(value, jss) ?? throw new NullReferenceException("The config is empty");
    }

    public static string SerializeConfig<T>(T value, bool skip = true)
    {
        var jss = new JsonSerializerSettings
        {
            Converters = _jsonconverters,
            ContractResolver = new CustomContractResolver { SkipOptionalDefault = skip },
            Formatting = Formatting.Indented,
        };

        return JsonConvert.SerializeObject(value, jss);
    }
}

public class CustomContractResolver : DefaultContractResolver
{
    public bool SkipOptionalDefault { get; set; } = false;
    public bool WarnExtensionData { get; set; } = false;
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        if (this.SkipOptionalDefault
            && property.PropertyType?.IsGenericType == true
            && typeof(Optional<>) == property.PropertyType.GetGenericTypeDefinition())
        {
            property.ShouldSerialize = value => property.ValueProvider?.GetValue(value) is not Optional o || !o.IsHiddenValue();
        }
        return property;
    }

    public override JsonContract ResolveContract(Type type)
    {
        var jc = base.ResolveContract(type);
        if (this.WarnExtensionData && jc is JsonObjectContract joc)
        {
            var orig = joc.ExtensionDataSetter;
            joc.ExtensionDataSetter = (obj, key, value) =>
            {
                Console.WriteLine($"Json deserialize got undefined pair in object {obj.GetType()} (\"{key}\": {value})");
                orig?.Invoke(obj, key, value);
            };
        }
        return jc;
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
            serializer.Serialize(writer, o.ObjectValue);
        }
        else
        {
            throw new Exception("Unreachable code: WriteJson is not a Optional");
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (existingValue is not Optional o)
        {
            throw new Exception("Unreachable code: ReadJson is not a Optional");
        }
        var type = objectType.GenericTypeArguments[0];
        try
        {
            o.ObjectValue = serializer.Deserialize(reader, type);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return existingValue;
    }
}

public class LimiterConverter : JsonConverter<LimiterConfig>
{
    public override void WriteJson(JsonWriter writer, LimiterConfig? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteValue("0/1");
        }
        else
        {
            var str = $"{(decimal) value.RateLimit}/{(decimal) value.Maximum}";
            if (!string.IsNullOrWhiteSpace(value.Action))
            {
                str += $"/{value.Action}";
            }
            writer.WriteValue(str);
        }
    }

    public override LimiterConfig ReadJson(JsonReader reader, Type objectType, LimiterConfig? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var s = reader?.Value?.ToString() ?? "0/1";
        var split = s.Split('/');
        return new LimiterConfig
        {
            RateLimit = double.Parse(split[0]),
            Maximum = double.Parse(split[1]),
            Action = split.Length == 3 ? split[2] : null
        };
    }
}

public class PacketFilterConverter : JsonConverter<PacketFilter>
{
    public override void WriteJson(JsonWriter writer, PacketFilter value, JsonSerializer serializer)
    {
        var trueflag = true;
        var falseflag = false;
        foreach (var index in Enumerable.Range(0, Config.PacketFilter.MaxPacket + 1))
        {
            var item = value.Handle(index);
            trueflag &= item;
            falseflag |= item;
        }
        if (trueflag)
        {
            writer.WriteValue(true);
            return;
        }
        if (!falseflag)
        {
            writer.WriteValue(false);
            return;
        }
        var str = string.Join(",", Enumerable.Range(0, PacketFilter.MaxPacket + 1).Where(value.Handle));
        writer.WriteValue(str);
    }

    public override PacketFilter ReadJson(JsonReader reader, Type objectType, PacketFilter existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        static byte ParsePacket(string value)
        {
            return Enum.TryParse<PacketTypes>(value, out var type)
                ? (byte) type
                : byte.TryParse(value, out var result) ? result : throw new Exception($"Unknown packet type: {value}");
        }

        var type = reader?.ValueType;
        if (type == typeof(bool))
        {
            return new PacketFilter((bool) reader!.Value!);
        }
        if (type == typeof(string))
        {
            var s = reader?.Value?.ToString() ?? "";
            var split = s.Split(',').Select(ParsePacket).ToArray();
            return new PacketFilter(split);
        }

        throw new Exception("Unreachable code: PacketFilter is not a bool or string");
    }
}