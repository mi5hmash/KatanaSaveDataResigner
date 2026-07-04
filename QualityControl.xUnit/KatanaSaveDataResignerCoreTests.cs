using System.Text.Json;
using KatanaSaveDataResignerCore;
using KatanaSaveDataResignerCore.GameTitlesFactory;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using Mi5hmasH.Logger;
using Mi5hmasH.Progress;

namespace QualityControl.xUnit;

public sealed class KatanaSaveDataResignerCoreTests : IDisposable
{
    private readonly Core _core;
    private readonly ITestOutputHelper _output;

    public KatanaSaveDataResignerCoreTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("SETUP");

        // Setup
        var logger = new SimpleLogger();
        var progressReporter = new ProgressReporter(null, null);
        _core = new Core(logger, progressReporter);
    }

    public void Dispose()
    {
        _output.WriteLine("CLEANUP");
    }

    private const string UserIdShort = "1";
    private const string UserIdLong = "76561197960265729";

    [Fact]
    public async Task DecryptFilesAsync_DoesNotThrow_WhenNoFiles()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testResult = true;

        // Act
        try
        {
            await _core.DecryptFilesAsync(tempDir, GameTitleIdEnum.Nioh, cts);
        }
        catch
        {
            testResult = false;
        }
        Directory.Delete(tempDir, true);

        // Assert
        Assert.True(testResult);
    }

    [Fact]
    public async Task EncryptFilesAsync_DoesNotThrow_WhenNoFiles()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testResult = true;

        // Act
        try
        {
            await _core.EncryptFilesAsync(tempDir, GameTitleIdEnum.Nioh, cts);
        }
        catch
        {
            testResult = false;
        }
        Directory.Delete(tempDir, true);

        // Assert
        Assert.True(testResult);
    }

    [Fact]
    public async Task ResignFilesAsync_DoesNotThrow_WhenNoFiles()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testResult = true;

        // Act
        try
        {
            await _core.ResignFilesAsync(tempDir, UserIdShort, GameTitleIdEnum.Nioh, cts);
        }
        catch
        {
            testResult = false;
        }
        Directory.Delete(tempDir, true);

        // Assert
        Assert.True(testResult);
    }

    public static TheoryData<GameTitleIdEnum, byte[], byte[]> DecryptFileTheories =>
        new()
        {
            { GameTitleIdEnum.Nioh, Properties.Resources.nioh_encrypted, Properties.Resources.nioh_decrypted },
            { GameTitleIdEnum.Nioh2, Properties.Resources.nioh2_encrypted, Properties.Resources.nioh2_decrypted },
            { GameTitleIdEnum.Nioh3, Properties.Resources.nioh3_encrypted, Properties.Resources.nioh3_decrypted },
            { GameTitleIdEnum.Sopffo, Properties.Resources.sopffo_encrypted, Properties.Resources.sopffo_decrypted },
            { GameTitleIdEnum.Wolong, Properties.Resources.wolong_dummy_encrypted, Properties.Resources.wolong_dummy_decrypted },
            { GameTitleIdEnum.WolongPs4, Properties.Resources.wolongps4_dummy_encrypted, Properties.Resources.wolongps4_dummy_decrypted },
            { GameTitleIdEnum.Ff2Cbr, Properties.Resources.ff2cbr_dummy_encrypted, Properties.Resources.ff2cbr_dummy_decrypted }
        };
    
    [Theory]
    [MemberData(nameof(DecryptFileTheories))]
    public void DecryptFiles_DoesDecrypt(GameTitleIdEnum variant, byte[] encryptedData, byte[] decryptedData)
    {
        // Arrange
        _output.WriteLine($"Title ID: {variant}");
        var file = GameTitleRegistry.GetGameTitle(variant);
        file.FileData = encryptedData;

        // Act
        file.Decrypt();
        var resultData = file.FileData;

        // Assert
        Assert.Equal(decryptedData, (ReadOnlySpan<byte>)resultData);
    }

    public static TheoryData<GameTitleIdEnum, byte[]> EncryptFileTheories =>
        new()
        {
            { GameTitleIdEnum.Nioh, Properties.Resources.nioh_decrypted },
            { GameTitleIdEnum.Nioh2, Properties.Resources.nioh2_decrypted },
            { GameTitleIdEnum.Nioh3, Properties.Resources.nioh3_decrypted },
            { GameTitleIdEnum.Sopffo, Properties.Resources.sopffo_decrypted },
            { GameTitleIdEnum.Wolong, Properties.Resources.wolong_dummy_decrypted },
            { GameTitleIdEnum.WolongPs4, Properties.Resources.wolongps4_dummy_decrypted },
            { GameTitleIdEnum.Ff2Cbr, Properties.Resources.ff2cbr_dummy_decrypted }
        };

    [Theory]
    [MemberData(nameof(EncryptFileTheories))]
    public void EncryptFiles_DoesEncrypt(GameTitleIdEnum variant, byte[] decryptedData)
    {
        // Arrange
        _output.WriteLine($"Title ID: {variant}");
        const int bytesToCompare = 64;
        var file = GameTitleRegistry.GetGameTitle(variant);
        file.FileData = decryptedData;

        // Act
        file.Encrypt();
        file.Decrypt();
        var resultData = file.FileData;

        // Assert
        Assert.Equal(decryptedData.AsSpan()[^bytesToCompare..], ((ReadOnlySpan<byte>)resultData)[^bytesToCompare..]);
    }

    public static TheoryData<GameTitleIdEnum, byte[], string> FindUserIdTheories =>
        new()
        {
            { GameTitleIdEnum.Nioh, Properties.Resources.nioh_encrypted, UserIdShort },
            { GameTitleIdEnum.Nioh2, Properties.Resources.nioh2_encrypted, UserIdShort },
            { GameTitleIdEnum.Nioh3, Properties.Resources.nioh3_encrypted, UserIdLong },
            { GameTitleIdEnum.Sopffo, Properties.Resources.sopffo_encrypted, UserIdShort },
            { GameTitleIdEnum.Ronin, Properties.Resources.ronin_dummy, UserIdLong },
            { GameTitleIdEnum.Wolong, Properties.Resources.wolong_dummy_encrypted, UserIdShort },
            { GameTitleIdEnum.WolongPs4, Properties.Resources.wolongps4_dummy_encrypted, "0" },
            { GameTitleIdEnum.Ff2Cbr, Properties.Resources.ff2cbr_dummy_encrypted, UserIdShort }
        };

    [Theory]
    [MemberData(nameof(FindUserIdTheories))]
    public void FindUserId_DoesFind(GameTitleIdEnum variant, byte[] encryptedData, string expectedUserId)
    {
        // Arrange
        _output.WriteLine($"Title ID: {variant}");
        var file = GameTitleRegistry.GetGameTitle(variant);
        file.FileData = encryptedData;

        // Act
        var userId = file.GetUserId();

        // Assert
        Assert.Equal(expectedUserId, userId.ToString());
    }

    [Fact]
    public void EncryptFiles_Ronin_DoesThrow()
    {
        // Arrange
        var decryptedData = Properties.Resources.ronin_dummy;
        var file = GameTitleRegistry.GetGameTitle(GameTitleIdEnum.Ronin);
        file.FileData = decryptedData;

        // Assert
        Assert.Throws<NotSupportedException>(file.Encrypt);
    }

    [Fact]
    public void DecryptFiles_Ronin_DoesThrow()
    {
        // Arrange
        var encryptedData = Properties.Resources.ronin_dummy;
        var file = GameTitleRegistry.GetGameTitle(GameTitleIdEnum.Ronin);
        file.FileData = encryptedData;

        // Assert
        Assert.Throws<NotSupportedException>(file.Decrypt);
    }

    public static TheoryData<GameTitleIdEnum, byte[]> JsonTheories =>
        new()
        {
            { GameTitleIdEnum.Nioh, Properties.Resources.nioh_encrypted },
            { GameTitleIdEnum.Nioh2, Properties.Resources.nioh2_encrypted },
            { GameTitleIdEnum.Nioh3, Properties.Resources.nioh3_encrypted },
            { GameTitleIdEnum.Sopffo, Properties.Resources.sopffo_encrypted },
            { GameTitleIdEnum.Wolong, Properties.Resources.wolong_dummy_encrypted },
            { GameTitleIdEnum.WolongPs4, Properties.Resources.wolongps4_dummy_encrypted },
            { GameTitleIdEnum.Ff2Cbr, Properties.Resources.ff2cbr_dummy_encrypted }
        };

    [Theory]
    [MemberData(nameof(JsonTheories))]
    public void Json_DoesImportAndExport(GameTitleIdEnum variant, byte[] encryptedData)
    {
        // Arrange
        _output.WriteLine($"Title ID: {variant}");
        var obj = new { Id = 1, Name = "Michael", Active = true };
        var jsonData = JsonSerializer.SerializeToUtf8Bytes(obj);
        var saveDataFormat = GameTitleRegistry.GetGameTitleSaveDataFormat(variant);
        var file = GameTitleRegistry.GetGameTitle(variant);
        file.FileData = encryptedData;

        // Act & Assert
        if (saveDataFormat == SaveDataFormatEnum.Json)
        {
            _output.WriteLine("Format: JSON");
            file.ImportJson(jsonData);
            var resultData = file.ExportJson();

            Assert.Equal(jsonData, (ReadOnlySpan<byte>)resultData);
        }
        else
        {
            _output.WriteLine("Format: BINARY");
            Assert.Throws<NotSupportedException>(() => file.ImportJson(jsonData));
        }
    }
}