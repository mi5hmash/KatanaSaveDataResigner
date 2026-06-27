using System.Reflection;
using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

namespace KatanaSaveDataResignerCore.GameTitlesFactory;

public static class GameTitleRegistry
{
    private static readonly Dictionary<GameTitleIdEnum, SaveDataFormatEnum> GameTitleSaveDataFormats = [];
    private static readonly Dictionary<GameTitleIdEnum, Type> GameTitleTypes = [];
    public static readonly Dictionary<GameTitleIdEnum, string> GameTitlesFriendlyNames = [];

    static GameTitleRegistry()
    {
        var ns = typeof(NiohFile).Namespace!;
        var baseType = typeof(ISaveDataFile);

        var elements = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Select(t => new
            {
                Type = t,
                GameTitleAttribute = t.GetCustomAttribute<GameTitleTypeAttribute>(false),
                SaveDataFormatAttribute = t.GetCustomAttribute<SaveDataFormatAttribute>()
            })
            .Where(x =>
                x.Type.Namespace != null &&
                x.Type.Namespace.StartsWith(ns) &&
                x.GameTitleAttribute != null &&
                x.SaveDataFormatAttribute != null &&
                !x.Type.IsAbstract &&
                baseType.IsAssignableFrom(x.Type));

        foreach (var element in elements)
        {
            var type = element.Type;
            if (!GameTitleSaveDataFormats.TryAdd(element.GameTitleAttribute!.GameTitleId, element.SaveDataFormatAttribute!.SaveDataFormat) || 
                !GameTitleTypes.TryAdd(element.GameTitleAttribute!.GameTitleId, type))
                throw new InvalidOperationException($"Duplicate GameTitleId '{element.GameTitleAttribute!.GameTitleId}' in {element.Type.FullName}");
            GameTitlesFriendlyNames[element.GameTitleAttribute!.GameTitleId] = element.GameTitleAttribute!.FriendlyName;
        }
    }
    
    public static GameTitleIdEnum ToGameTitleId(this string value) 
        => Enum.TryParse<GameTitleIdEnum>(value, true, out var id)
            ? id
            : throw new ArgumentException($"Unknown GameTitleId: {value}");

    private static T GetOrThrow<T>(Dictionary<GameTitleIdEnum, T> dict, GameTitleIdEnum id)
        => dict.TryGetValue(id, out var value)
            ? value
            : throw new NotSupportedException($"The game title '{id}' is not supported.");
    
    public static ISaveDataFile GetGameTitle(GameTitleIdEnum id)
    {
        var type = GetOrThrow(GameTitleTypes, id);
        return (ISaveDataFile)Activator.CreateInstance(type)!;
    }

    public static SaveDataFormatEnum GetGameTitleSaveDataFormat(GameTitleIdEnum id)
        => GetOrThrow(GameTitleSaveDataFormats, id);

}