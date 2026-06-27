using KatanaSaveDataResignerCore.GameTitlesFactory.Attributes;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using KatanaSaveDataResignerCore.Helpers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace KatanaSaveDataResignerCore.GameTitlesFactory.Titles;

[GameTitleType(GameTitleIdEnum.Wolong, "[PC] Wo Long: Fallen Dynasty")]
[SaveDataFormat(SaveDataFormatEnum.Json)]
public class WolongFile : ISaveDataFile
{
    // CONSTANTS
    private const int ContainerLength = 4;
    private const int ContainerLengthBytes = 16;
    private const int ChecksumSize = 0x20;
    private const int HeroLevelOffset = 0x1C;
    private const int HeroChapterIdOffset = 0x20;
    private const int HeroBattlementImageIdOffset = 0x24;

    // CONSTANT BYTE ARRAYS
    private static readonly int[] Sequence = [1, 2, 3, 0, 1, 2, 3];
    private static readonly uint[] PrivateKey = "u0CECl+cfUDBjpAo++5Rjw==".FromBase64<uint>();
    private static readonly byte[] HashTableE0 = "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".FromBase64<byte>();
    private static readonly byte[] HashTableD0 = "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".FromBase64<byte>();
    private static readonly byte[] HashTableE1 = "UMTjOub+TYrdP+abcohmx3ol95XwaAlza442rqzovtz8tGw5DNxlSmdSU+TLuu04+6uYaPd3/SKQJa7GW59D/kCSQ3q35b5YJ8AQnnxfU2CQgoyHJ2cy3wCnIkF8+HEhbZLNBEr1/9tKUt2aNqqsu4eXYdXNYp4OhzBDlLGa7y+SX9mKXz1HhNgNBBBpl+s/56ZReLibFvxglhLsCQH509ctp4ErOzw5xymqWRTQq1A=".FromBase64<byte>();
    private static readonly byte[] HashTableD1 = "1y2ngSs7PDnHKapZFNCrUCl0T3plgUJv+NqasGJd1suXpftXTPUNFZ1b2N+ah0x7ZVfQRttQ9kLRrtXKB9yUpJSEWH6+ByYECv4jiNZyQW4fYzFUKoN+erT5BYzcjGLmylIEdzXgTy6eenv2aHVnaob9vmX/sktZq5o02PYPHJzu00RkeU/1PFQof4FdlShEc6OEaZecsVgtZ4q9Cb1XxVDE4zrm/k2K3T/mm3KIZsc=".FromBase64<byte>();
    private static readonly byte[] HashTableC = "810MZdvnQKKZE6sJA39HX4KEyulhUPTGw22xuVNUrMT/q3esR76Rn5X2+FllTqVaTyLL9AlKCpns4x3+52cWsiRwBOBRghtdheKWV86G1ZSTvzrAsbWkyES8zNdrfi6IfSsDF0npaQC0XBnqIPLK7pecbDI0qI+dcQboOwx8vbBy84tiJXscutEm9wV/P5KAjl9QTMZY4bahdd7H8YHNI9vcJ91Vatr8QqanowGz0O12+ku3Dbh05m6gjThz0y8weIctUm85hD1AVCELFTNW5EHB8MJG+8/v2cR6udZgEWY2ihq7mir9N8UUmDWiEIxtwwLJ+YNDra6vBw4pD5BT2JsTPtRNY1vrHvUfnuVFMRJkaCypPAjfedIoiV6qYUgY".FromBase64<byte>();

    // PROTECTED VIRTUALS (can be overridden by derived classes)
    protected virtual int UserIdLength => sizeof(uint);
    protected virtual int DataHeaderPatternOffset => 0x8;
    protected virtual int DataHeaderPatternSize => sizeof(ulong);
    protected virtual int UserIdOffset => 0x10;
    protected virtual int HeaderDataSize => 0x100;
    protected virtual int JsonDataOffset => 0x108;
    protected virtual int HeaderDataLengthOffset => 0x14;
    protected virtual int DataLengthOffset => 0x18;
    /// <summary>
    /// The offset of a time of file creation in time64_t format.
    /// </summary>
    protected virtual int FileCreationTime64Offset => 0x30;
    protected virtual int DataChecksumOffset => 0x3C;
    protected virtual int HeaderChecksumOffset => 0x5C;

    // PUBLIC VIRTUALS (interface members that can be overridden by derived classes)
    public static readonly byte[] MagicUserBytes = "WLNUSR"u8.ToArray();
    public static readonly byte[] MagicSystemBytes = "WLNSYS"u8.ToArray();
    public virtual byte[] MagicUser => MagicUserBytes;
    public virtual byte[] MagicSystem => MagicSystemBytes;

    public byte[] FileData { get; set; } = [];

    public virtual bool IsEncrypted()
    {
        var fileDataSpan = FileData.AsSpan();
        var dataHeaderPatternFromHeader = fileDataSpan.Slice(DataHeaderPatternOffset, DataHeaderPatternSize);
        var dataHeaderPatternFromData = fileDataSpan.Slice(HeaderDataSize, DataHeaderPatternSize);
        return !dataHeaderPatternFromHeader.SequenceEqual(dataHeaderPatternFromData);
    }

    /// <summary>
    /// Decrypts the file data in-place. If the data is not encrypted, this method does nothing.
    /// </summary>
    public virtual void Decrypt()
    {
        if (!IsEncrypted())
            return;

        var dataSpan = FileData.AsSpan()[HeaderDataSize..];
        DecryptData(dataSpan);
    }

    /// <summary>
    /// Decrypts the data in-place. The data must be encrypted before calling this method, otherwise the result will be corrupted.
    /// </summary>
    /// <param name="dataSpan">The span of data to decrypt.</param>
    protected static void DecryptData(Span<byte> dataSpan)
    {
        // Allocate stack
        Span<uint> localContainerA = stackalloc uint[ContainerLength];
        Span<uint> localContainerB = stackalloc uint[ContainerLength];
        Span<uint> localContainerC = stackalloc uint[ContainerLength];
        // Create pointers
        var hashTableD0SpanByte0 = new ReadOnlySpan<byte>(HashTableD0);
        var hashTableD0SpanByte1 = new ReadOnlySpan<byte>(HashTableD0, 1, HashTableD0.Length - 1);
        var hashTableD0SpanUint1 = MemoryMarshal.Cast<byte, uint>(hashTableD0SpanByte1);
        var hashTableD0SpanByte2 = new ReadOnlySpan<byte>(HashTableD0, 2, HashTableD0.Length - 2);
        var hashTableD0SpanUint2 = MemoryMarshal.Cast<byte, uint>(hashTableD0SpanByte2);
        var hashTableD0SpanByte3 = new ReadOnlySpan<byte>(HashTableD0, 3, HashTableD0.Length - 3);
        var hashTableD0SpanUint3 = MemoryMarshal.Cast<byte, uint>(hashTableD0SpanByte3);
        var hashTableD0SpanByte4 = new ReadOnlySpan<byte>(HashTableD0, 4, HashTableD0.Length - 4);
        var hashTableD0SpanUint4 = MemoryMarshal.Cast<byte, uint>(hashTableD0SpanByte4);
        var hashTableD1SpanByte0 = new ReadOnlySpan<byte>(HashTableD1, 0, 32);
        var hashTableD1SpanUint0 = MemoryMarshal.Cast<byte, uint>(hashTableD1SpanByte0);
        var hashTableD1SpanByte20 = new ReadOnlySpan<byte>(HashTableD1, 32, 128);
        var hashTableD1SpanUint20 = MemoryMarshal.Cast<byte, uint>(hashTableD1SpanByte20);
        var hashTableD1SpanByte100 = new ReadOnlySpan<byte>(HashTableD1, 160, 16);
        var hashTableD1SpanUint100 = MemoryMarshal.Cast<byte, uint>(hashTableD1SpanByte100);
        var dataSpanUint = MemoryMarshal.Cast<byte, uint>(dataSpan);
        var localContainerASpanByte = MemoryMarshal.Cast<uint, byte>(localContainerA);

        var laps = dataSpan.Length / ContainerLengthBytes;
        PrivateKey.CopyTo(localContainerC);
        for (var z = 0; z < laps; z++)
        {
            for (var i = 0; i < ContainerLength; i++)
                localContainerA[i] = dataSpanUint[z * ContainerLength + i] ^ hashTableD1SpanUint0[i];

            for (var i = 0; i < ContainerLength; i++)
                localContainerB[i] = hashTableD1SpanUint0[^(4 - i)] 
                                     ^ hashTableD0SpanUint1[2 * (byte)(localContainerA[Sequence[i + 2]] >> 8)] 
                                     ^ hashTableD0SpanUint2[2 * (byte)((localContainerA[Sequence[i + 1]] & 0xFF0000) >> 16)] 
                                     ^ hashTableD0SpanUint3[2 * (byte)((localContainerA[Sequence[i]] & 0xFF000000) >> 24)] 
                                     ^ hashTableD0SpanUint4[2 * (byte)localContainerA[Sequence[i + 3]]];

            for (var i = 0; i < ContainerLength; i++)
            {
                for (var j = 0; j < ContainerLength; j++)
                    localContainerA[j] = hashTableD1SpanUint20[i * 8 + Sequence[j + 3]] 
                                         ^ hashTableD0SpanUint1[2 * (byte)((localContainerB[Sequence[j + 2]] & 0xFF0000) >> 16)] 
                                         ^ hashTableD0SpanUint2[2 * (byte)(localContainerB[Sequence[j + 1]] >> 8)] 
                                         ^ hashTableD0SpanUint3[2 * (byte)localContainerB[Sequence[j]]] 
                                         ^ hashTableD0SpanUint4[2 * (byte)((localContainerB[Sequence[j + 3]] & 0xFF000000) >> 24)];

                for (var j = 0; j < ContainerLength; j++)
                    localContainerB[j] = hashTableD1SpanUint20[i * 8 + Sequence[j + 3] + 4] 
                                         ^ hashTableD0SpanUint1[2 * (byte)((localContainerA[Sequence[j + 2]] & 0xFF0000) >> 16)] 
                                         ^ hashTableD0SpanUint2[2 * (byte)(localContainerA[Sequence[j + 1]] >> 8)] 
                                         ^ hashTableD0SpanUint3[2 * (byte)localContainerA[Sequence[j]]] 
                                         ^ hashTableD0SpanUint4[2 * (byte)((localContainerA[Sequence[j + 3]] & 0xFF000000) >> 24)];
            }

            for (var i = 0; i < ContainerLength; i++)
            {
                localContainerASpanByte[i * ContainerLength + 0] = hashTableD0SpanByte0[(int)(8 * ((localContainerB[Sequence[i + 3]] & 0xFF000000) >> 24))];
                localContainerASpanByte[i * ContainerLength + 1] = hashTableD0SpanByte0[(int)(8 * ((localContainerB[Sequence[i + 2]] & 0xFF0000) >> 16))];
                localContainerASpanByte[i * ContainerLength + 2] = hashTableD0SpanByte0[8 * (byte)(localContainerB[Sequence[i + 1]] >> 8)];
                localContainerASpanByte[i * ContainerLength + 3] = hashTableD0SpanByte0[8 * (byte)localContainerB[Sequence[i]]];
            }

            for (var i = 0; i < ContainerLength; i++)
            {
                localContainerA[i] ^= hashTableD1SpanUint100[i] ^ localContainerC[i];
                localContainerC[i] = dataSpanUint[z * ContainerLength + i];
                dataSpanUint[z * ContainerLength + i] = localContainerA[i];
            }
        }
    }

    /// <summary>
    /// Calculates the checksum of the file data.</summary>
    /// <param name="checksumContainer">The span of bytes to store the checksum.</param>
    /// <param name="data">The span of data to calculate the checksum for.</param>
    private static void CalculateChecksum(Span<byte> checksumContainer, ReadOnlySpan<byte> data)
    {
        // Create pointers
        var hashTableC0 = new ReadOnlySpan<byte>(HashTableC, 0, 32);
        var hashTableC20 = new ReadOnlySpan<byte>(HashTableC, 32, HashTableC.Length - 32);

        for (var i = 0; i < data.Length; i++)
        {
            var v1 = data[i] + checksumContainer[0] + (checksumContainer[1] << 8);
            checksumContainer[0] = (byte)v1;
            checksumContainer[1] = (byte)(v1 >> 8);

            var pos = 2 * ((i >> 2) & 1);
            v1 = data[i] + checksumContainer[pos + 2] + (checksumContainer[pos + 3] << 8);
            checksumContainer[pos + 2] = (byte)v1;
            checksumContainer[pos + 3] = (byte)(v1 >> 8);

            pos = 2 * (i & 3) + 6;
            v1 = data[i] + checksumContainer[pos] + (checksumContainer[pos + 1] << 8);
            checksumContainer[pos] = (byte)v1;
            checksumContainer[pos + 1] = (byte)(v1 >> 8);
        }

        for (var i = 0; i < 31; i++)
            checksumContainer[i + 1] += (byte)(2 * i + checksumContainer[i]);

        for (var i = 0; i < 32; i++)
            checksumContainer[i] = (byte)(hashTableC20[checksumContainer[i]] + hashTableC0[i]);
    }

    /// <summary>
    /// Calculates the checksum of the data and updates the header accordingly.
    /// </summary>
    /// <param name="dataSpan">The span of data to calculate the checksum for.</param>
    /// <param name="headerData">The span of header data to update with the checksum.</param>
    private void CalculateDataChecksum(Span<byte> dataSpan, Span<byte> headerData)
    {
        var dataChecksumData = headerData.Slice(DataChecksumOffset, ChecksumSize);
        dataChecksumData.Clear();
        CalculateChecksum(dataChecksumData, dataSpan);
    }

    /// <summary>
    /// Calculates the checksum of the header and updates the header accordingly.
    /// </summary>
    /// <param name="headerData">The span of header data to update with the checksum.</param>
    private void CalculateHeaderChecksum(Span<byte> headerData)
    {
        var headerChecksumData = headerData.Slice(HeaderChecksumOffset, ChecksumSize);
        headerChecksumData.Clear();
        Span<byte> headerChecksumContainer = stackalloc byte[ChecksumSize];
        CalculateChecksum(headerChecksumContainer, headerData);
        headerChecksumContainer.CopyTo(headerChecksumData);
    }

    /// <summary>
    /// Encrypts the data in-place.
    /// </summary>
    public virtual void Encrypt()
    {
        if (IsEncrypted())
            return;

        var fileDataSpan = FileData.AsSpan();
        var headerData = fileDataSpan[..HeaderDataSize];
        // Recalculate checksums
        var dataSpan = fileDataSpan[HeaderDataSize..];
        CalculateDataChecksum(dataSpan, headerData);
        CalculateHeaderChecksum(headerData);
        // Encrypt data
        EncryptData(dataSpan);
    }
    
    /// <summary>
    /// Encrypts the data in-place. The data must be decrypted before calling this method, otherwise the result will be corrupted.
    /// </summary>
    /// <param name="dataSpan">The span of data to encrypt.</param>
    protected static void EncryptData(Span<byte> dataSpan)
    {
        // Allocate stack
        Span<uint> localContainerA = stackalloc uint[ContainerLength];
        Span<uint> localContainerB = stackalloc uint[ContainerLength];
        Span<uint> localContainerC = stackalloc uint[ContainerLength];
        // Create pointers
        var hashTableE0SpanByte1 = new ReadOnlySpan<byte>(HashTableE0, 1, HashTableE0.Length - 1);
        var hashTableE0SpanUint1 = MemoryMarshal.Cast<byte, uint>(hashTableE0SpanByte1);
        var hashTableE0SpanByte2 = new ReadOnlySpan<byte>(HashTableE0, 2, HashTableE0.Length - 2);
        var hashTableE0SpanUint2 = MemoryMarshal.Cast<byte, uint>(hashTableE0SpanByte2);
        var hashTableE0SpanByte3 = new ReadOnlySpan<byte>(HashTableE0, 3, HashTableE0.Length - 3);
        var hashTableE0SpanUint3 = MemoryMarshal.Cast<byte, uint>(hashTableE0SpanByte3);
        var hashTableE0SpanByte4 = new ReadOnlySpan<byte>(HashTableE0, 4, HashTableE0.Length - 4);
        var hashTableE0SpanUint4 = MemoryMarshal.Cast<byte, uint>(hashTableE0SpanByte4);
        var hashTableE1SpanByte0 = new ReadOnlySpan<byte>(HashTableE1, 0, 32);
        var hashTableE1SpanUint0 = MemoryMarshal.Cast<byte, uint>(hashTableE1SpanByte0);
        var hashTableE1SpanByte20 = new ReadOnlySpan<byte>(HashTableE1, 32, 128);
        var hashTableE1SpanUint20 = MemoryMarshal.Cast<byte, uint>(hashTableE1SpanByte20);
        var hashTableE1SpanByte100 = new ReadOnlySpan<byte>(HashTableE1, 160, 16);
        var hashTableE1SpanUint100 = MemoryMarshal.Cast<byte, uint>(hashTableE1SpanByte100);
        var dataSpanUint = MemoryMarshal.Cast<byte, uint>(dataSpan);
        var localContainerASpanByte = MemoryMarshal.Cast<uint, byte>(localContainerA);

        var laps = dataSpan.Length / ContainerLengthBytes;
        PrivateKey.CopyTo(localContainerC);
        for (var z = 0; z < laps; z++)
        {
            for (var i = 0; i < ContainerLength; i++)
                localContainerA[i] = dataSpanUint[z * ContainerLength + i] 
                                     ^ localContainerC[i] 
                                     ^ hashTableE1SpanUint0[i];

            for (var i = 0; i < ContainerLength; i++)
                localContainerB[i] = hashTableE1SpanUint0[^(4 - i)] 
                                     ^ hashTableE0SpanUint1[2 * (byte)(localContainerA[Sequence[i]] >> 8)] 
                                     ^ hashTableE0SpanUint2[2 * (byte)((localContainerA[Sequence[i + 1]] & 0xFF0000) >> 16)] 
                                     ^ hashTableE0SpanUint3[2 * (byte)((localContainerA[Sequence[i + 2]] & 0xFF000000) >> 24)] 
                                     ^ hashTableE0SpanUint4[2 * (byte)localContainerA[Sequence[i + 3]]];

            for (var i = 0; i < ContainerLength; i++)
            {
                for (var j = 0; j < ContainerLength; j++)
                    localContainerA[j] = hashTableE1SpanUint20[i * 8 + Sequence[j]] 
                                         ^ hashTableE0SpanUint1[2 * (byte)((localContainerB[Sequence[j + 1]] & 0xFF0000) >> 16)] 
                                         ^ hashTableE0SpanUint2[2 * (byte)(localContainerB[Sequence[j + 2]] >> 8)] 
                                         ^ hashTableE0SpanUint3[2 * (byte)localContainerB[Sequence[j + 3]]] 
                                         ^ hashTableE0SpanUint4[((byte)((localContainerB[Sequence[j]] & 0xFF000000) >> 24) << 3) / 4];

                for (var j = 0; j < ContainerLength; j++)
                    localContainerB[j] = hashTableE1SpanUint20[i * 8 + Sequence[j + 3] + 4] 
                                         ^ hashTableE0SpanUint1[2 * (byte)((localContainerA[Sequence[j + 3]] & 0xFF0000) >> 16)] 
                                         ^ hashTableE0SpanUint2[2 * (byte)(localContainerA[Sequence[j]] >> 8)] 
                                         ^ hashTableE0SpanUint3[2 * (byte)localContainerA[Sequence[j + 1]]] 
                                         ^ hashTableE0SpanUint4[((byte)((localContainerA[Sequence[j + 2]] & 0xFF000000) >> 24) << 3) / 4];
            }

            for (var i = 0; i < ContainerLength; i++)
            {
                localContainerASpanByte[i * ContainerLength + 0] = hashTableE0SpanByte1[(int)(8 * ((localContainerB[Sequence[i + 3]] & 0xFF000000) >> 24))];
                localContainerASpanByte[i * ContainerLength + 1] = hashTableE0SpanByte1[(int)(8 * ((localContainerB[Sequence[i]] & 0xFF0000) >> 16))];
                localContainerASpanByte[i * ContainerLength + 2] = hashTableE0SpanByte1[8 * (byte)(localContainerB[Sequence[i + 1]] >> 8)];
                localContainerASpanByte[i * ContainerLength + 3] = hashTableE0SpanByte1[8 * (byte)localContainerB[Sequence[i + 2]]];
            }

            for (var i = 0; i < ContainerLength; i++)
            {
                localContainerC[i] = localContainerA[i] ^ hashTableE1SpanUint100[i];
                dataSpanUint[z * ContainerLength + i] = localContainerC[i];
            }
        }
    }
    
    /// <summary>
    /// Resigns the save file by updating the user ID and recalculating the checksums.
    /// </summary>
    /// <param name="userId">The new user ID to set in the save file.</param>
    public virtual void Resign(ulong userId)
    {
        var fileDataSpan = FileData.AsSpan();
        var headerData = fileDataSpan[..HeaderDataSize];

        // Encrypt data if not already encrypted
        if (!IsEncrypted())
        {
            // Recalculate data checksum
            var dataSpan = fileDataSpan[HeaderDataSize..];
            CalculateDataChecksum(dataSpan, headerData);
            // Encrypt data
            EncryptData(dataSpan);
        }

        // UPDATE USER_ID (32-bit vs 64-bit)
        var userIdData = headerData.Slice(UserIdOffset, UserIdLength);
        if (UserIdLength == 4)
            BinaryPrimitives.WriteUInt32LittleEndian(userIdData, (uint)userId);
        else
            BinaryPrimitives.WriteUInt64LittleEndian(userIdData, userId);

        // Update header checksum
        CalculateHeaderChecksum(headerData);
    }

    /// <summary>
    /// Reads the user ID from the file data. The user ID is located at a specific offset in the header and can be either 32-bit or 64-bit depending on the implementation. The method reads the appropriate number of bytes from the file data and converts it to an unsigned long integer.
    /// </summary>
    /// <returns>The user ID as an unsigned long integer.</returns>
    public virtual ulong GetUserId()
    {
        var userIdData = FileData.AsSpan().Slice(UserIdOffset, UserIdLength);
        if (UserIdLength == 4)
            return BinaryPrimitives.ReadUInt32LittleEndian(userIdData);

        return BinaryPrimitives.ReadUInt64LittleEndian(userIdData);
    }
    
    /// <summary>
    /// Validates the length of the file data to ensure it is sufficient to contain the expected header and JSON data.
    /// </summary>
    /// <param name="fileDataSpan">The span of the file data to validate.</param>
    /// <param name="minFileSize">The minimum required file size.</param>
    /// <exception cref="InvalidOperationException">Thrown if the file data is too small to contain valid save data.</exception>
    protected static void ValidateFileDataLength(ReadOnlySpan<byte> fileDataSpan, int minFileSize)
    {
        if (fileDataSpan.Length <= minFileSize)
            throw new InvalidOperationException("File data is too small to contain valid save data.");
    }

    /// <summary>
    /// Finds the end of the JSON data in the file data. The JSON data is expected to be terminated by a null byte (0x00) or the last occurrence of 0x7D.
    /// </summary>
    /// <param name="data">The span of the file data to search.</param>
    /// <returns>The index of the end of the JSON data.</returns>
    protected static int FindEof(ReadOnlySpan<byte> data)
    {
        // Look for the first occurrence of the null byte (0x00) which indicates the end of the JSON data
        var zeroIndex = data.IndexOf((byte)0x00);
        if (zeroIndex > 0)
        {
            // Check if the data before the null byte is 0x7D
            if (data[zeroIndex - 1] == 0x7D)
                return zeroIndex - 1;
        }
        // otherwise, look for the last occurrence of 0x7D which indicates the end of the JSON data
        var last7D = data.LastIndexOf((byte)0x7D);
        return last7D;
    }

    /// <summary>
    /// Exports the JSON data from the save file.
    /// </summary>
    /// <returns>The JSON data as a byte array.</returns>
    public virtual byte[] ExportJson()
    {
        var fileDataSpan = FileData.AsSpan();
        ValidateFileDataLength(fileDataSpan, DataHeaderPatternSize);
        Decrypt();
        var dataSpan = fileDataSpan[JsonDataOffset..];
        var eof = FindEof(dataSpan) + 1;
        return dataSpan[..eof].ToArray();
    }

    /// <summary>
    /// Imports the provided JSON data into the save file. The method first validates the file data length, decrypts the data if necessary, and then copies the provided JSON data into the appropriate location in the file data.
    /// </summary>
    /// <param name="jsonData">The JSON data to import.</param>
    /// <exception cref="InvalidOperationException">Thrown if the JSON data is longer than the available space in the file.</exception>
    public virtual void ImportJson(ReadOnlySpan<byte> jsonData)
    {
        var fileDataSpan = FileData.AsSpan();
        ValidateFileDataLength(fileDataSpan, DataHeaderPatternSize);
        Decrypt();
        var dataSpan = fileDataSpan[JsonDataOffset..];
        if (jsonData.Length > dataSpan.Length)
            throw new InvalidOperationException("The JSON data to import is longer then the input data.");

        dataSpan.Clear();
        jsonData.CopyTo(dataSpan);
        Encrypt();
    }
}