using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SaveDataFormatAttribute(SaveDataFormatEnum saveDataFormat) : Attribute
{
    public SaveDataFormatEnum SaveDataFormat { get; } = saveDataFormat;
}