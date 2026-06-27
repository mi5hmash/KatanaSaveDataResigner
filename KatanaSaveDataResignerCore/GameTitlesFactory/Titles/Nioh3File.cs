using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

[GameTitleType(GameTitleIdEnum.Nioh3, "NIOH 3")]
public sealed class Nioh3File : NiohFile
{
    protected override int UserIdLength => sizeof(ulong);
    protected override int UserIdOffset => 0x10;
    protected override int HeaderDataLengthOffset => 0x18;
    protected override int DataLengthOffset => 0x1C;
    protected override byte[] KeyOffsets => [0x49, 0x59, 0x69, 0x79];
    protected override int HeaderDataSize => 0x158;

    public new static readonly byte[] MagicUserBytes = "RNNUSR"u8.ToArray();
    public new static readonly byte[] MagicSystemBytes = "RNNSYS"u8.ToArray();
    public override byte[] MagicUser => MagicUserBytes;
    public override byte[] MagicSystem => MagicSystemBytes;
}