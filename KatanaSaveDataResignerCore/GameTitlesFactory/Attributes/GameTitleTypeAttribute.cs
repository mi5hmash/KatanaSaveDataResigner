using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GameTitleTypeAttribute(GameTitleIdEnum gameTitleIdEnum, string? friendlyName = null) : Attribute
{
    public GameTitleIdEnum GameTitleId { get; } = gameTitleIdEnum;
    public string FriendlyName { get; } = friendlyName ?? gameTitleIdEnum.ToString();
}