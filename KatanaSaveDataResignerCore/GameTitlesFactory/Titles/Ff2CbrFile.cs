using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

[GameTitleType(GameTitleIdEnum.Ff2Cbr, "FATAL FRAME II - Crimson Butterfly REMAKE")]
[SaveDataFormat(SaveDataFormatEnum.Json)]
public class Ff2CbrFile : WolongFile
{
    protected override int JsonDataOffset => 0x110;
    protected override int DataChecksumOffset => 0x50;
    protected override int HeaderChecksumOffset => 0x70;
}