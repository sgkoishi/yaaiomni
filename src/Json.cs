using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using static Chireiden.TShock.Omni.Config;
using static Chireiden.TShock.Omni.Config.DebugPacketSettings;
using static Chireiden.TShock.Omni.JsonUtils;

namespace Chireiden.TShock.Omni;

public static partial class Utils
{
    public static Config DeserializeConfig(string value)
    {
        var jss = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = new List<JsonConverter>
            {
                new OptionalConverter(),
                new LimiterConverter(),
                new StringEnumConverter(),
                new PacketFilterConverter(),
            },
            ContractResolver = new OptionalSerializeContractResolver(),
            Formatting = Formatting.Indented,
        };

        return JsonConvert.DeserializeObject<Config>(value, jss) ?? throw new Exception("Config is null");
    }

    public static string SerializeConfig(Config value, bool skip = true)
    {
        var jss = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = new List<JsonConverter>
            {
                new OptionalConverter(),
                new LimiterConverter(),
                new StringEnumConverter(),
                new PacketFilterConverter(),
            },
            ContractResolver = skip ? new OptionalSerializeContractResolver() : new DefaultContractResolver(),
            Formatting = Formatting.Indented,
        };

        return JsonConvert.SerializeObject(value, jss);
    }
}
public static class JsonUtils
{
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
                var str = Math.Round(value.RateLimit) == value.RateLimit ? $"{(int) value.RateLimit}" : $"{value.RateLimit}";
                str += "/";
                str += Math.Round(value.Maximum) == value.Maximum ? $"{(int) value.Maximum}" : $"{value.Maximum}";
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
                Maximum = double.Parse(split[1])
            };
        }
    }

    public class PacketFilterConverter : JsonConverter<PacketFilter>
    {
        public override void WriteJson(JsonWriter writer, PacketFilter? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                var trueflag = true;
                var falseflag = false;
                foreach (var index in Enumerable.Range(0, Config.DebugPacketSettings.PacketFilter.MaxPacket + 1))
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
                var str = string.Join(",", Enumerable.Range(0, PacketFilter.MaxPacket + 1).Where(index => value.Handle(index)));
                writer.WriteValue(str);
            }
        }

        public override PacketFilter ReadJson(JsonReader reader, Type objectType, PacketFilter? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
}