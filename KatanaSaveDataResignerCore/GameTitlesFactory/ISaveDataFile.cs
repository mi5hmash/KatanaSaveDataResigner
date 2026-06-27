namespace KatanaSaveDataResignerCore.GameTitlesFactory;

public interface ISaveDataFile
{
    byte[] FileData { get; set; }
    byte[] MagicUser { get; }
    byte[] MagicSystem { get; }
    
    bool IsEncrypted();
    void Decrypt();
    void Encrypt();
    void Resign(ulong userId);
    ulong GetUserId();
    byte[] ExportJson();
    void ImportJson(ReadOnlySpan<byte> jsonData);
}