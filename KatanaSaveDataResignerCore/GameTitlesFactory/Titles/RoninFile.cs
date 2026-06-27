using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using System.Buffers.Binary;
using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

[GameTitleType(GameTitleIdEnum.Ronin, "Rise of the Ronin")]
[SaveDataFormat(SaveDataFormatEnum.Binary)]
public class RoninFile : ISaveDataFile
{
    protected virtual int UserIdLength => sizeof(ulong);
    protected virtual int UserIdOffset => 0x10;
    protected virtual int HeaderDataLengthOffset => 0x18;
    protected virtual int DataLengthOffset => 0x1C;
    protected virtual int HeaderDataSize => 0x100;

    public static readonly byte[] MagicUserBytes = "RNNUSR"u8.ToArray();
    public static readonly byte[] MagicSystemBytes = "RNNSYS"u8.ToArray();
    public virtual byte[] MagicUser => MagicUserBytes;
    public virtual byte[] MagicSystem => MagicSystemBytes;

    public byte[] FileData { get; set; } = [];

    /// <summary>
    /// Ronin's SaveData files are not encrypted, so this method always return false.
    /// </summary>
    public bool IsEncrypted() => false;

    /// <summary>
    /// Throws a NotSupportedException to indicate that decryption is not supported for Ronin save files.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown in all cases because Ronin save files are not encrypted and decryption is not applicable.</exception>
    public void Decrypt() 
        => throw new NotSupportedException("Ronin save files are not encrypted.");

    /// <summary>
    /// Throws a NotSupportedException to indicate that encryption is not supported for Ronin save files.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown in all cases to indicate that Ronin save files should remain decrypted.</exception>
    public void Encrypt()
        => throw new NotSupportedException("Ronin save files should stay decrypted.");

    /// <summary>
    /// Updates the stored user identifier to the specified value, indicating that the user has resigned.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who is resigning. Must be a valid 64-bit unsigned integer.</param>
    public void Resign(ulong userId)
    {
        var userIdData = FileData.AsSpan().Slice(UserIdOffset, UserIdLength);
        BinaryPrimitives.WriteUInt64LittleEndian(userIdData, userId);
    }

    /// <summary>
    /// Retrieves the user identifier from the save data file, allowing for verification of the current user associated with the save data.
    /// </summary>
    /// <returns>The unique identifier of the user associated with the save data.</returns>
    public ulong GetUserId()
    {
        var userIdData = FileData.AsSpan().Slice(UserIdOffset, UserIdLength);
        return BinaryPrimitives.ReadUInt64LittleEndian(userIdData);
    }

    /// <summary>
    /// Throws a NotSupportedException to indicate that exporting JSON data is not supported for Ronin save files.
    /// </summary>
    /// <returns>Never returns a value because this method always throws an exception.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public byte[] ExportJson()
        => throw new NotSupportedException("Ronin save files do not support exporting JSON data.");

    /// <summary>
    /// Throws a NotSupportedException to indicate that importing JSON data is not supported for Ronin save files.
    /// </summary>
    /// <param name="jsonData">The JSON data to import. This parameter is not used because importing is not supported.</param>
    /// <exception cref="NotSupportedException"></exception>
    public void ImportJson(ReadOnlySpan<byte> jsonData)
        => throw new NotSupportedException("Ronin save files do not support importing JSON data.");
}