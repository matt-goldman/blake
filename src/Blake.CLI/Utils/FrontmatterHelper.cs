using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Blake.CLI.Generator;

public static class FrontmatterHelper
{
    public static Dictionary<string, object> ParseFrontmatter(string markdown, out string cleanedContent)
    {
        var regex = new Regex("^---\\s*$(.*?)^---\\s*$", RegexOptions.Singleline | RegexOptions.Multiline);
        var match = regex.Match(markdown);

        if (match.Success)
        {
            var yamlText = match.Groups[1].Value.Trim();
            cleanedContent = markdown.Substring(match.Index + match.Length).Trim();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var result = deserializer.Deserialize<Dictionary<string, object>>(yamlText);
            return result ?? new();
        }
        else
        {
            cleanedContent = markdown;
            return new();
        }
    }

    public static T MapToMetadata<T>(Dictionary<string, object> dict) where T : new()
    {
        var obj = new T();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            var key = dict.Keys.FirstOrDefault(k => k.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
            if (key == null) continue;

            var value = dict[key];

            try
            {
                if (prop.PropertyType == typeof(DateTime?) && DateTime.TryParse(value?.ToString(), out var dt))
                {
                    prop.SetValue(obj, dt);
                }
                else if (prop.PropertyType == typeof(bool) && bool.TryParse(value?.ToString(), out var b))
                {
                    prop.SetValue(obj, b);
                }
                else if (prop.PropertyType == typeof(List<string>) && value is IEnumerable<object> list)
                {
                    prop.SetValue(obj, list.Select(v => v.ToString() ?? string.Empty).ToList());
                }
                else if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(obj, value?.ToString() ?? string.Empty);
                }
                else
                {
                    prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                }
            }
            catch
            {
                // Ignore mapping errors silently; could log if desired
            }
        }

        return obj;
    }
}
