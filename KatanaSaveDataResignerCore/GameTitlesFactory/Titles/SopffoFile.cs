using System.Buffers.Binary;
using System.Security.Cryptography;
using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using KatanaSaveDataResignerCore.Helpers;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

[GameTitleType(GameTitleIdEnum.Sopffo, "STRANGER OF PARADISE FINAL FANTASY ORIGIN")]
[SaveDataFormat(SaveDataFormatEnum.Binary)]
public class SopffoFile : ISaveDataFile
{
    // CONSTANT BYTE ARRAYS
    private static readonly byte[] Key = "CpBtJ2lVBrWMGiKdxHH0VA==".FromBase64<byte>();
    private static readonly byte[] Iv = "qBvSUQRCr6JPWzcVDDvPig==".FromBase64<byte>();

    // PROTECTED VIRTUALS (can be overridden by derived classes)
    protected virtual int UserIdLength => sizeof(uint);
    protected virtual int UserIdOffset => 0x10;
    protected virtual int HeaderDataLengthOffset => 0x14;
    protected virtual int DataLengthOffset => 0x18;
    protected virtual int HeaderDataSize => 0x100;

    // PUBLIC VIRTUALS (interface members that can be overridden by derived classes)
    public static readonly byte[] MagicUserBytes = "RNNUSR"u8.ToArray();
    public static readonly byte[] MagicSystemBytes = "RNNSYS"u8.ToArray();
    public virtual byte[] MagicUser => MagicUserBytes;
    public virtual byte[] MagicSystem => MagicSystemBytes;

    // PUBLIC PROPERTIES (interface members)
    public byte[] FileData { get; set; } = [];

    #region METHODS

    public bool IsEncrypted()
    {
        var magic = FileData.AsSpan();
        return !(magic[..MagicUser.Length].SequenceEqual(MagicUser) || magic[..MagicSystem.Length].SequenceEqual(MagicSystem));
    }

    private static Aes GetAes()
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key = Key;
        aes.IV = Iv;
        return aes;
    }
    
    private static byte[] Decrypt(ReadOnlySpan<byte> dataSpan)
    {
        // Initialize AES
        using var aes = GetAes();
        // Decrypt the entire file in-place
        using MemoryStream msi = new(dataSpan.ToArray());
        using var decryptor = aes.CreateDecryptor();
        using CryptoStream cs = new(msi, decryptor, CryptoStreamMode.Read);
        using MemoryStream mso = new();
        cs.CopyTo(mso);
        return mso.ToArray();
    }

    public void Decrypt()
    {
        if (!IsEncrypted())
            return;

        FileData = Decrypt(FileData);
    }

    public void Encrypt()
    {
        if (IsEncrypted())
            return;

        // Initialize AES
        using var aes = GetAes();
        // Encrypt the entire file in-place
        using var ms = new MemoryStream();
        using var encryptor = aes.CreateEncryptor();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write, true);
        cs.Write(FileData, 0, FileData.Length);
        cs.FlushFinalBlock();
        FileData = ms.ToArray();
    }

    public void Resign(ulong userId)
    {
        // DECRYPT if needed
        if (IsEncrypted()) Decrypt();

        // UPDATE USER_ID
        var userIdData = FileData.AsSpan().Slice(UserIdOffset, UserIdLength);
        BinaryPrimitives.WriteUInt32LittleEndian(userIdData, (uint)userId);

        // RE-ENCRYPT
        Encrypt();
    }

    public ulong GetUserId()
    {
        var headerDataSliceSize = (UserIdOffset + UserIdLength + 15) & ~15; // Round up to the nearest multiple of 16
        var headerData = FileData.AsSpan()[..headerDataSliceSize].ToArray();
        // DECRYPT if needed
        if (IsEncrypted())
            headerData = Decrypt(headerData);
        var userIdSpan = headerData.AsSpan().Slice(UserIdOffset, UserIdLength);
        return BinaryPrimitives.ReadUInt32LittleEndian(userIdSpan);
    }

    public byte[] ExportJson() 
        => throw new NotSupportedException("Sopffo save files do not support exporting JSON data.");

    public void ImportJson(ReadOnlySpan<byte> jsonData)
        => throw new NotSupportedException("Sopffo save files do not support importing JSON data.");

    #endregion
}