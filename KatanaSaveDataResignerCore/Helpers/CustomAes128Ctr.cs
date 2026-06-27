using System.Buffers.Binary;

namespace KatanaSaveDataResignerCore.Helpers;

/// <summary>
/// Provides static methods for performing AES-128 encryption and decryption in custom counter (CTR) mode, including key expansion and block transformations.
/// </summary>
public static class CustomAes128Ctr
{
    /// <summary>
    /// Encrypts or decrypts the specified data buffer using AES in counter (CTR) mode with the provided counter block, round keys, and substitution box.
    /// </summary>
    /// <param name="data">The buffer containing the data to be encrypted or decrypted. The operation is performed in-place, modifying the contents of this span.</param>
    /// <param name="counterBlock">A 16-byte block used as the initial counter value for AES-CTR mode. The first 12 bytes are used as the initialization vector (IV), and the last 4 bytes represent the starting counter.</param>
    /// <param name="roundKeys">A span containing the expanded AES round keys required for block encryption.</param>
    /// <param name="sBox">A span containing the AES substitution box (S-box) used during block encryption.</param>
    public static void Crypt(Span<byte> data, ReadOnlySpan<byte> counterBlock, ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> sBox)
    {
        var iv = counterBlock[..12];
        var counter = BinaryPrimitives.ReadUInt32BigEndian(counterBlock[^4..]);
        Span<byte> ctrBlock = stackalloc byte[16];

        var offset = 0;
        var remaining = data.Length;
        while (remaining > 0)
        {
            // Copy IV into counter block
            iv.CopyTo(ctrBlock);
            // Write counter into last 4 bytes
            BinaryPrimitives.WriteUInt32BigEndian(ctrBlock[12..], counter);

            // Encrypt counter block
            AesEncryptBlock(ctrBlock, roundKeys, sBox);

            // XOR block
            var blockSize = remaining >= 16 ? 16 : remaining;
            var block = data.Slice(offset, blockSize);
            for (var i = 0; i < blockSize; i++)
                block[i] ^= ctrBlock[i];

            counter++;
            offset += blockSize;
            remaining -= blockSize;
        }
    }

    /// <summary>
    /// Encrypts or decrypts a block of data in-place using AES in counter (CTR) mode.
    /// </summary>
    /// <param name="line">The block of data to be encrypted or decrypted. The operation is performed in-place, modifying the contents of this span.</param>
    /// <param name="blockPosition">The zero-based index of the block within the stream. Used to increment the counter for the AES CTR mode.</param>
    /// <param name="counterBlock">A 16-byte span containing the initialization vector (IV) and initial counter value for AES CTR mode. The first 12 bytes are used as the IV, and the last 4 bytes as the counter.</param>
    /// <param name="roundKeys">A span containing the expanded AES round keys used for encryption.</param>
    /// <param name="sBox">A span containing the AES substitution box (S-box) used during encryption.</param>
    public static void CryptBlock(Span<byte> line, uint blockPosition, ReadOnlySpan<byte> counterBlock, ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> sBox)
    {
        var iv = counterBlock[..12];
        var counter = BinaryPrimitives.ReadUInt32BigEndian(counterBlock[^4..]);
        Span<byte> ctrBlock = stackalloc byte[16];
        // Copy IV into counter block
        iv.CopyTo(ctrBlock);
        // Write counter into last 4 bytes
        BinaryPrimitives.WriteUInt32BigEndian(ctrBlock[12..], counter + blockPosition);
        // Encrypt counter block
        AesEncryptBlock(ctrBlock, roundKeys, sBox);
        // XOR block
        for (var i = 0; i < line.Length; i++)
            line[i] ^= ctrBlock[i];
    }
    
    /// <summary>
    /// Encrypts a single 16-byte block using the AES algorithm with the specified round keys and substitution box.
    /// </summary>
    /// <param name="state">A span containing the 16-byte input block to be encrypted. The encrypted output is written in place to this span.</param>
    /// <param name="roundKeys">A read-only span containing the expanded AES round keys. Must provide keys for all rounds required by the algorithm.</param>
    /// <param name="sBox">A read-only span containing the substitution box (S-box) used for the SubBytes transformation. Must be 256 bytes in length.</param>
    private static void AesEncryptBlock(Span<byte> state, ReadOnlySpan<uint> roundKeys, ReadOnlySpan<byte> sBox)
    {
        // Round 0: AddRoundKey
        AddRoundKey(state, 0, roundKeys);

        // Rounds 1..9: SubBytes, ShiftRows, MixColumns, AddRoundKey
        for (var round = 1; round <= 9; round++)
        {
            SubBytes(state, sBox);
            ShiftRows(state);
            MixColumns(state);
            AddRoundKey(state, round, roundKeys);
        }

        // Round 10: SubBytes, ShiftRows, AddRoundKey
        SubBytes(state, sBox);
        ShiftRows(state);
        AddRoundKey(state, 10, roundKeys);
    }

    /// <summary>
    /// Applies the round key to the AES cipher state by performing a bitwise XOR of the state bytes with the corresponding round key bytes.
    /// </summary>
    /// <param name="state">A span of bytes representing the current AES cipher state. The state will be updated in place with the round key applied.</param>
    /// <param name="round">The zero-based index of the AES round for which the round key should be applied.</param>
    /// <param name="roundKeys">A read-only span containing the expanded AES round keys. The method uses four consecutive 32-bit words starting at the specified round.</param>
    private static void AddRoundKey(Span<byte> state, int round, ReadOnlySpan<uint> roundKeys)
    {
        var w = round * 4; // 4 uints per round

        for (var i = 0; i < 4; i++)
        {
            var word = roundKeys[w + i];

            // Extract bytes in reverse order to match original code
            state[i * 4 + 0] ^= (byte)(word >> 24);
            state[i * 4 + 1] ^= (byte)(word >> 16);
            state[i * 4 + 2] ^= (byte)(word >> 8);
            state[i * 4 + 3] ^= (byte)(word >> 0);
        }
    }

    /// <summary>
    /// Substitutes each byte in the specified span using the provided substitution box (S-box).
    /// </summary>
    /// <param name="s">A span of bytes to be substituted. Each byte in this span will be replaced according to the mapping defined by <paramref name="sBox"/>.</param>
    /// <param name="sBox">A read-only span representing the substitution box. Each index corresponds to a possible byte value, and the value at that index is the substituted byte.</param>
    private static void SubBytes(Span<byte> s, ReadOnlySpan<byte> sBox)
    {
        for (var i = 0; i < 16; i++)
            s[i] = sBox[s[i]];
    }

    /// <summary>
    /// Performs the AES ShiftRows transformation on the specified state array.
    /// </summary>
    /// <param name="s">A span representing the 16-byte AES state array. The transformation is applied in place.</param>
    private static void ShiftRows(Span<byte> s)
    {
        // Row 1: [1, 5, 9, 13] rotate left by 1
        {
            var tmp = s[1];
            s[1] = s[5];
            s[5] = s[9];
            s[9] = s[13];
            s[13] = tmp;
        }

        // Row 2: [2, 6, 10, 14] rotate left by 2 (swap pairs)
        {
            (s[2], s[10]) = (s[10], s[2]);
            (s[6], s[14]) = (s[14], s[6]);
        }

        // Row 3: [3, 7, 11, 15] rotate left by 3 (right by 1)
        {
            var tmp = s[15];
            s[15] = s[11];
            s[11] = s[7];
            s[7] = s[3];
            s[3] = tmp;
        }
    }

    /// <summary>
    /// Transforms the specified state matrix by mixing each column according to the AES MixColumns operation.
    /// </summary>
    /// <param name="s">A span representing the state matrix to be transformed. Must contain exactly 16 bytes, arranged in column-major order.</param>
    private static void MixColumns(Span<byte> s)
    {
        for (var i = 0; i < 16; i += 4)
            MixOneColumn(ref s[i + 0], ref s[i + 1], ref s[i + 2], ref s[i + 3]);
    }

    /// <summary>
    /// Transforms the specified four-byte column using a mixing operation commonly employed in cryptographic algorithms.
    /// </summary>
    /// <param name="b0">A reference to the first byte of the column to be mixed. The value will be updated in place.</param>
    /// <param name="b1">A reference to the second byte of the column to be mixed. The value will be updated in place.</param>
    /// <param name="b2">A reference to the third byte of the column to be mixed. The value will be updated in place.</param>
    /// <param name="b3">A reference to the fourth byte of the column to be mixed. The value will be updated in place.</param>
    private static void MixOneColumn(ref byte b0, ref byte b1, ref byte b2, ref byte b3)
    {
        byte a0 = b0, a1 = b1, a2 = b2, a3 = b3;

        b0 = (byte)(a2 ^ a3 ^ a1 ^ XTime((byte)(a0 ^ a1)));
        b1 = (byte)(a0 ^ a3 ^ a2 ^ XTime((byte)(a1 ^ a2)));
        b2 = (byte)(a0 ^ a1 ^ a3 ^ XTime((byte)(a2 ^ a3)));
        b3 = (byte)(a2 ^ a0 ^ a1 ^ XTime((byte)(a3 ^ a0)));
    }

    /// <summary>
    /// Performs the AES xtime operation, multiplying the specified byte by 2 in the finite field GF(2^8).
    /// </summary>
    /// <param name="x">The byte value to be multiplied by 2 in the AES finite field operation.</param>
    /// <returns>A byte representing the result of multiplying the input by 2 in GF(2^8), as defined by the AES algorithm.</returns>
    private static byte XTime(byte x)
    {
        var shifted = (byte)(x << 1);
        var mask = (byte)((sbyte)x >> 7);
        return (byte)(shifted ^ (mask & 0x1B));
    }

    /// <summary>
    /// Expands the provided AES key into a sequence of round keys using the specified S-box.
    /// </summary>
    /// <param name="roundKeys">A span of 44 unsigned 32-bit integers that will be populated with the expanded round keys.</param>
    /// <param name="key">A read-only span containing the original AES key as 16 bytes. Must be exactly 16 bytes for AES-128 key expansion.</param>
    /// <param name="sBox">A read-only span containing the substitution box (S-box) used for key expansion.</param>
    public static void ExpandKey(Span<uint> roundKeys, ReadOnlySpan<byte> key, ReadOnlySpan<byte> sBox)
    {
        // rCon on stack
        ReadOnlySpan<uint> rCon =
        [
            0x00000000, 0x01000000, 0x02000000, 0x04000000,
            0x08000000, 0x10000000, 0x20000000, 0x40000000,
            0x80000000, 0x1B000000, 0x36000000
        ];

        // w[0..3] from key (big-endian)
        for (var i = 0; i < 4; i++)
        {
            var o = i * 4;
            roundKeys[i] = ((uint)key[o] << 24) |
                           ((uint)key[o + 1] << 16) |
                           ((uint)key[o + 2] << 8) |
                           key[o + 3];
        }

        // Expand w[4..43]
        for (var i = 4; i < 44; i++)
        {
            var temp = roundKeys[i - 1];

            if ((i & 3) == 0)
            {
                // RotWord
                temp = (temp << 8) | (temp >> 24);

                // SubWord using custom S-box
                var b0 = (byte)(temp >> 24);
                var b1 = (byte)(temp >> 16);
                var b2 = (byte)(temp >> 8);
                var b3 = (byte)temp;

                temp = ((uint)sBox[b0] << 24) |
                       ((uint)sBox[b1] << 16) |
                       ((uint)sBox[b2] << 8) |
                       sBox[b3];

                temp ^= rCon[i >> 2];
            }

            roundKeys[i] = roundKeys[i - 4] ^ temp;
        }
    }

    /// <summary>
    /// Fills the specified span with cryptographically random bytes suitable for use as a key.
    /// </summary>
    /// <param name="key">A span of bytes to be populated with random values.</param>
    public static void GenerateKey(Span<byte> key)
    {
        Random random = new();
        for (var i = 0; i < key.Length; i++) key[i] = (byte)random.Next(byte.MaxValue + 1);
    }
}