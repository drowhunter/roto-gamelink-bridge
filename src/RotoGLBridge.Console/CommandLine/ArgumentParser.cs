using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RotoGLBridge.ConsoleApp;


internal class ArgumentParser<T> where T : class
{

    private const StringSplitOptions splitoptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    private static BindingFlags _flags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public;

    public static T Parse(string[] args)
    {
        var argD = string.Join(" ", args).Split("--", splitoptions)
            .ToDictionary(k => KebabToPascal(k.Split(' ', splitoptions)[0]),
                v => v.Split(' ', splitoptions).Length > 1 ? v.Split(' ', splitoptions)[1] : "true");

        if (argD.ContainsKey("Help"))
        {
            Console.WriteLine(Usage());
            return null!;
        }

        var missing = Validate(argD);




        var options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.Converters.Add(new CommaSeparatedStringToArrayConverter());
        options.Converters.Add(new StringToBooleanConverter());

        var d = JsonSerializer.Serialize(argD);

        return JsonSerializer.Deserialize<T>(d, options)!;
    }

    private static string PascalToKebab(string pascal) => string.Concat(pascal.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString() : x.ToString())).ToLower();

    private static string KebabToPascal(string kebab) => string.Join("", kebab.Split('-').Select(_ => _.Substring(0, 1).ToUpper() + _.Substring(1)));

    private static List<string> Validate(Dictionary<string, string> args)
    {
        var requiredProps = typeof(T).GetProperties(_flags).Where(pi => pi.GetCustomAttribute<RequiredAttribute>() != null).Select(_ => _.Name).ToList();

        var missingProps = requiredProps.Where(_ => !args.ContainsKey(_)).Select((s, i) => s).Select(PascalToKebab).ToList();

        if (missingProps.Any())
        {
            throw new ArgumentException($"Missing required arguments: \n\n{Usage(missingProps.ToArray())}");
        }
        return missingProps;
    }
    private static string Usage(IEnumerable<string>? kebabs = null)
    {
        var props = typeof(T).GetProperties(_flags);
        if (kebabs == null || !kebabs.Any())
        {
            kebabs = props.Select(p => ArgumentParser<T>.PascalToKebab(p.Name));
        }

        var sb = new StringBuilder("Usage:\nudpproxy.exe [arguments]\n\nArguments:\n\n");

        foreach (var prop in props.Where(p => kebabs.Select(KebabToPascal).Any(_ => _ == p.Name)))
        {
            sb.Append($"--{PascalToKebab(prop.Name)} ");

            switch (prop.PropertyType)
            {
                case Type t when t == typeof(bool):
                    sb.Append(isRequired(prop, ""));
                    break;
                case Type t when t == typeof(float):
                    sb.Append(isRequired(prop, "<float>"));
                    break;
                case Type t when t == typeof(int):
                    sb.Append(isRequired(prop, "<int>"));
                    break;
                case Type t when t == typeof(int[]):
                    sb.Append(isRequired(prop, "<int,int,...>"));
                    break;
                case Type t when t == typeof(string):
                    sb.Append(isRequired(prop, "<string>"));
                    break;
            }

            var d = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (d != null)
            {
                sb.AppendFormat("\t\t{0}", d);
            }

            sb.AppendLine();

        }

        string isRequired(PropertyInfo prop, string val) => (prop.GetCustomAttribute<RequiredAttribute>() == null ? val : $"{val} (required)") + " ";


        return sb.ToString();
    }
}

internal class StringToBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (bool.TryParse(stringValue, out var result))
            {
                return result;
            }
            throw new JsonException($"Unable to convert \"{stringValue}\" to a boolean.");
        }
        throw new JsonException($"Unexpected token parsing boolean. Expected String, got {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLower());
    }
}



// Create a Custom JsonConverter
internal class CommaSeparatedStringToArrayConverter : JsonConverter<int[]>
{
    private const StringSplitOptions splitoptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    public override int[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<int>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int number))
                {
                    list.Add(number);
                }
            }
            return list.ToArray();
        }

        var stringValue = reader.GetString();
        var retval = stringValue?.Split(',', splitoptions)
                          .Select(int.Parse)
                          .ToArray();
        return retval ?? [];
    }

    public override void Write(Utf8JsonWriter writer, int[] value, JsonSerializerOptions options)
    {
        var joinedString = string.Join(",", value);
        writer.WriteStringValue(joinedString);
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert != typeof(string) && typeof(IEnumerable).IsAssignableFrom(typeToConvert);
    }


}
