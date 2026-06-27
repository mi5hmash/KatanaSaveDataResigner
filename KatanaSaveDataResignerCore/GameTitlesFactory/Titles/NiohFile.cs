using System.Buffers.Binary;
using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using KatanaSaveDataResignerCore.Helpers;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

[GameTitleType(GameTitleIdEnum.Nioh, "NIOH")]
[SaveDataFormat(SaveDataFormatEnum.Binary)]
public class NiohFile : ISaveDataFile
{
    // CONSTANTS
    private const int RoundKeyCount = 44;
    private const int KeyLength = 16;

    // CONSTANT BYTE ARRAYS
    private static readonly byte[] SBox = "HC8DU6MBSdqmzeCKGacE1AYa2kkI4vaynuEiSc57fl6gCSpjr0nOcHs8I4D6F0fyYmJsWRDMKZy1RljHRBPnONWvJ4PU1aCe43Y7hQTZ1phgZtR4U+rKDo1WU0Ti772pmxAKoROT8EMLfDmKR9/TxQ40MaauWrjn5jFDwKoP4IISTNHfi6WscMU9G46TF015Ts5jxDMOFFfw2Blbm2Fx8iszfv0sC7YjILnUkRmUBKQwE4rx0AXsXqxK1NalF3/55fYAKdeTLV4s8YGjt2M5V8Izhy2oPwLMCGd0YNjw2mdAZIdVu3/yEMkDFLWAZsuR9h95WIi8lcIGX+kJMu2bhQ==".FromBase64<byte>();
    private static readonly byte[] HeaderKey1 = "NTEfzfhSTqJ5zFGSS1hI/g==".FromBase64<byte>();
    private static readonly byte[] HeaderKey2 = "DoyVzWMp9sJT7Po01bR/Xg==".FromBase64<byte>();
    private static readonly byte[] HeaderState1 = "G9/dVyfLzoc66sKe4FspJQ==".FromBase64<byte>();
    private static readonly byte[] HeaderState2 = "/UxAoo7OGYt3AbwMFLdXvQ==".FromBase64<byte>();
    private static readonly byte[] DataKey0 = "VFtevf14Q1V8fXxydMdEkQ==".FromBase64<byte>();
    private static readonly byte[] DataState0 = "Vwd4cd/s6OSHYsd+afkEXQ==".FromBase64<byte>();

    // PROTECTED VIRTUALS (can be overridden by derived classes)
    protected virtual int UserIdLength => sizeof(uint);
    protected virtual int UserIdOffset => 0x10;
    protected virtual int HeaderDataLengthOffset => 0x14;
    protected virtual int DataLengthOffset => 0x18;
    protected virtual byte[] KeyOffsets => [0x40, 0x50, 0x60, 0x70];
    protected virtual int HeaderDataSize => 0x148;
    protected virtual int KeyGenerationSize => 0x40;

    // PUBLIC VIRTUALS (interface members that can be overridden by derived classes)
    public static readonly byte[] MagicUserBytes = "NIOHUSR"u8.ToArray();
    public static readonly byte[] MagicSystemBytes = "NIOHSYS"u8.ToArray();
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

    private static void DeencryptHeader(Span<byte> headerData)
    {
        // Expand headerKey1
        Span<uint> headerRoundKeys1 = stackalloc uint[RoundKeyCount];
        CustomAes128Ctr.ExpandKey(headerRoundKeys1, HeaderKey1, SBox);

        // Expand headerKey2
        Span<uint> headerRoundKeys2 = stackalloc uint[RoundKeyCount];
        CustomAes128Ctr.ExpandKey(headerRoundKeys2, HeaderKey2, SBox);

        // DEENCRYPT HEADER PART 1 & 2
        CustomAes128Ctr.Crypt(headerData, HeaderState1, headerRoundKeys1, SBox);
        CustomAes128Ctr.Crypt(headerData, HeaderState2, headerRoundKeys2, SBox);
    }

    public void Decrypt()
    {
        if (!IsEncrypted())
            return;

        //DECRYPT HEADER
        var headerData = FileData.AsSpan()[..HeaderDataSize];
        DeencryptHeader(headerData);
        
        // DECRYPT DATA
        var data = FileData.AsSpan()[HeaderDataSize..];
        DecryptData(headerData, data, KeyOffsets);
        
        return;
        
        static void DecryptData(Span<byte> headerData, Span<byte> data, ReadOnlySpan<byte> keyOffsets)
        {
            // Slice keys and IVs for data decryption from decrypted header
            var dataKey1 = headerData.Slice(keyOffsets[0], KeyLength);
            var dataState1 = headerData.Slice(keyOffsets[1], KeyLength);
            var dataKey2 = headerData.Slice(keyOffsets[2], KeyLength);
            var dataState2 = headerData.Slice(keyOffsets[3], KeyLength);

            // Expand dataKey0
            Span<uint> dataRoundKeys0 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(dataRoundKeys0, DataKey0, SBox);

            // DECRYPT dataKey1 and dataState1
            CustomAes128Ctr.Crypt(dataKey1, DataState0, dataRoundKeys0, SBox);
            CustomAes128Ctr.Crypt(dataState1, DataState0, dataRoundKeys0, SBox);

            // Expand dataKey1
            Span<uint> dataRoundKeys1 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(dataRoundKeys1, dataKey1, SBox);

            // DECRYPT dataKey2 and dataState2
            CustomAes128Ctr.Crypt(dataKey2, dataState1, dataRoundKeys1, SBox);
            CustomAes128Ctr.Crypt(dataState2, dataState1, dataRoundKeys1, SBox);

            // Expand dataKey2
            Span<uint> dataRoundKeys2 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(dataRoundKeys2, dataKey2, SBox);

            // DECRYPT DATA PARTS
            CustomAes128Ctr.Crypt(data, dataState2, dataRoundKeys2, SBox);
            CustomAes128Ctr.Crypt(data, dataState1, dataRoundKeys1, SBox);
        }
    }

    public void Encrypt()
    {
        if (IsEncrypted())
            return;

        // Regenerate Keys and States
        var headerData = FileData.AsSpan()[..HeaderDataSize];
        CustomAes128Ctr.GenerateKey(headerData.Slice(KeyOffsets[0], KeyGenerationSize));

        // ENCRYPT DATA
        var data = FileData.AsSpan()[HeaderDataSize..];
        EncryptData(headerData, data, KeyOffsets);

        // ENCRYPT HEADER
        DeencryptHeader(headerData);

        return;

        static void EncryptData(Span<byte> headerData, Span<byte> data, ReadOnlySpan<byte> keyOffsets)
        {
            // Slice keys and IVs for data encryption from decrypted header
            var dataKey1 = headerData.Slice(keyOffsets[0], KeyLength);
            var dataState1 = headerData.Slice(keyOffsets[1], KeyLength);
            var dataKey2 = headerData.Slice(keyOffsets[2], KeyLength);
            var dataState2 = headerData.Slice(keyOffsets[3], KeyLength);

            // Expand dataKey1
            Span<uint> dataRoundKeys1 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(dataRoundKeys1, dataKey1, SBox);

            // Expand dataKey2
            Span<uint> dataRoundKeys2 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(dataRoundKeys2, dataKey2, SBox);

            // ENCRYPT DATA PARTS
            CustomAes128Ctr.Crypt(data, dataState2, dataRoundKeys2, SBox);
            CustomAes128Ctr.Crypt(data, dataState1, dataRoundKeys1, SBox);

            // ENCRYPT dataKey2 and dataState2
            CustomAes128Ctr.Crypt(dataKey2, dataState1, dataRoundKeys1, SBox);
            CustomAes128Ctr.Crypt(dataState2, dataState1, dataRoundKeys1, SBox);

            // Expand dataKey0
            Span<uint> dataRoundKeys0 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(dataRoundKeys0, DataKey0, SBox);

            // ENCRYPT dataKey1 and dataState1
            CustomAes128Ctr.Crypt(dataKey1, DataState0, dataRoundKeys0, SBox);
            CustomAes128Ctr.Crypt(dataState1, DataState0, dataRoundKeys0, SBox);
        }
    }
    
    public void Resign(ulong userId)
    {
        // Expand header keys
        Span<uint> headerRoundKeys1 = stackalloc uint[RoundKeyCount];
        CustomAes128Ctr.ExpandKey(headerRoundKeys1, HeaderKey1, SBox);

        Span<uint> headerRoundKeys2 = stackalloc uint[RoundKeyCount];
        CustomAes128Ctr.ExpandKey(headerRoundKeys2, HeaderKey2, SBox);

        // DECRYPT relevant header line(s) if needed
        var userIdData = FileData.AsSpan().Slice(UserIdOffset, UserIdLength);
        if (IsEncrypted())
        {
            CustomAes128Ctr.CryptBlock(userIdData, 1, HeaderState1, headerRoundKeys1, SBox);
            CustomAes128Ctr.CryptBlock(userIdData, 1, HeaderState2, headerRoundKeys2, SBox);
        }

        // UPDATE USER_ID (32-bit vs 64-bit)
        if (UserIdLength == 4)
            BinaryPrimitives.WriteUInt32LittleEndian(userIdData, (uint)userId);
        else
            BinaryPrimitives.WriteUInt64LittleEndian(userIdData, userId);

        // RE-ENCRYPT
        CustomAes128Ctr.CryptBlock(userIdData, 1, HeaderState1, headerRoundKeys1, SBox);
        CustomAes128Ctr.CryptBlock(userIdData, 1, HeaderState2, headerRoundKeys2, SBox);
    }

    public ulong GetUserId()
    {
        Span<byte> userIdData = stackalloc byte[UserIdLength];
        FileData.AsSpan().Slice(UserIdOffset, UserIdLength).CopyTo(userIdData);
        if (IsEncrypted())
        {
            Span<uint> headerRoundKeys1 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(headerRoundKeys1, HeaderKey1, SBox);

            Span<uint> headerRoundKeys2 = stackalloc uint[RoundKeyCount];
            CustomAes128Ctr.ExpandKey(headerRoundKeys2, HeaderKey2, SBox);

            CustomAes128Ctr.CryptBlock(userIdData, 1, HeaderState1, headerRoundKeys1, SBox);
            CustomAes128Ctr.CryptBlock(userIdData, 1, HeaderState2, headerRoundKeys2, SBox);
        }

        if (UserIdLength == 4)
            return BinaryPrimitives.ReadUInt32LittleEndian(userIdData);
        
        return BinaryPrimitives.ReadUInt64LittleEndian(userIdData);
    }

    /// <summary>
    /// Throws a NotSupportedException to indicate that exporting JSON data is not supported for Nioh save files.
    /// </summary>
    /// <returns>Never returns a value because this method always throws an exception.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public byte[] ExportJson()
        => throw new NotSupportedException("Nioh save files do not support exporting JSON data.");

    /// <summary>
    /// Throws a NotSupportedException to indicate that importing JSON data is not supported for Nioh save files.
    /// </summary>
    /// <param name="jsonData">The JSON data to import. This parameter is not used because importing is not supported.</param>
    /// <exception cref="NotSupportedException"></exception>
    public void ImportJson(ReadOnlySpan<byte> jsonData)
        => throw new NotSupportedException("Nioh save files do not support importing JSON data.");

    #endregion
}