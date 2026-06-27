using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

[GameTitleType(GameTitleIdEnum.WolongPs4, "[PS4] Wo Long: Fallen Dynasty")]
[SaveDataFormat(SaveDataFormatEnum.Json)]
public sealed class WolongPs4File : WolongFile
{
    private const int DataOffset = 0x8;
    private const int FooterSize = 0x40;

    public override byte[] MagicUser 
        => throw new NotSupportedException("Wolong PS4 save files do not have a MagicUser.");
    public override byte[] MagicSystem 
        => throw new NotSupportedException("Wolong PS4 save files do not have a MagicSystem.");

    public override bool IsEncrypted() 
        => FileData[8] != 0x7B && FileData[9] != 0x22;

    public override void Decrypt()
    {
        if (!IsEncrypted())
            return;

        var dataSpan = FileData.AsSpan()[DataOffset..];
        DecryptData(dataSpan);
    }

    public override void Encrypt()
    {
        if (IsEncrypted())
            return;

        var dataSpan = FileData.AsSpan()[DataOffset..];
        EncryptData(dataSpan);
    }

    public override void Resign(ulong userId) 
        => throw new InvalidOperationException("PS4 SaveData Files can't be resigned with this tool.");

    public override ulong GetUserId() => 0;
    
    public override byte[] ExportJson()
    {
        var fileDataSpan = FileData.AsSpan();
        ValidateFileDataLength(fileDataSpan, DataOffset + FooterSize);
        Decrypt();
        var dataSpan = fileDataSpan[DataOffset..^FooterSize];
        var eof = FindEof(dataSpan) + 1;
        return dataSpan[..eof].ToArray();
    }

    public override void ImportJson(ReadOnlySpan<byte> jsonData)
    {
        var fileDataSpan = FileData.AsSpan();
        ValidateFileDataLength(fileDataSpan, DataOffset + FooterSize);
        Decrypt();
        var dataSpan = fileDataSpan[DataOffset..^FooterSize];
        if (jsonData.Length > dataSpan.Length)
            throw new InvalidOperationException("The JSON data to import is longer then the input data.");

        dataSpan.Clear();
        jsonData.CopyTo(dataSpan);
        Encrypt();
    }
}