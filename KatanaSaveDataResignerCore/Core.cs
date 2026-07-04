using KatanaSaveDataResignerCore.GameTitlesFactory;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using KatanaSaveDataResignerCore.Infrastructure;
using Mi5hmasH.Logger;
using Mi5hmasH.Progress;

namespace KatanaSaveDataResignerCore;

public class Core(SimpleLogger logger, ProgressReporter progressReporter)
{
    /// <summary>
    /// Creates a new ParallelOptions instance configured with the specified cancellation token and an optimal degree of parallelism for the current environment.
    /// </summary>
    /// <param name="cts">The CancellationTokenSource whose token will be used to support cancellation of parallel operations.</param>
    /// <returns>A ParallelOptions object initialized with the provided cancellation token and a maximum degree of parallelism based on the number of available processors.</returns>
    private static ParallelOptions GetParallelOptions(CancellationTokenSource cts)
        => new()
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
        };

    /// <summary>
    /// Marks the progress reporting as complete by reporting 100% progress.
    /// </summary>
    /// <param name="progressTracker">The progress tracker used to report progress.</param>
    /// <param name="errorCounter">The error counter used to report errors.</param>
    private void LogAllTasksCompleted(ProgressTracker progressTracker, ErrorCounter errorCounter)
        => logger.LogInfo($"{progressTracker} All tasks completed. {errorCounter}");
    
    /// <summary>
    /// Asynchronously decrypts all .bin files found in the specified input directory and its subdirectories, saving the decrypted files to a newly created output directory while preserving the original folder structure.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the .bin files to decrypt. Must be a valid directory path.</param>
    /// /// <param name="titleId">The ID of the game title for which the save data files belong.</param>
    /// <param name="cts">The CancellationTokenSource used to support cancellation of the decryption operation.</param>
    public async Task DecryptFilesAsync(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
        => await Task.Run(() => DecryptFiles(inputDir, titleId, cts));
    
    /// <summary>
    /// Decrypts all .bin files found in the specified input directory and its subdirectories, saving the decrypted files to a newly created output directory while preserving the original folder structure.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the .bin files to decrypt. Must be a valid directory path.</param>
    /// <param name="titleId">The ID of the game title for which the save data files belong.</param>
    /// <param name="cts">The CancellationTokenSource used to support cancellation of the decryption operation.</param>
    public void DecryptFiles(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // DECRYPT
        logger.LogInfo($"Decrypting [{progressTracker.Total}] files...");
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("decrypted");
        Directory.CreateDirectory(outputDir);
        // Crate the folder structure in the newly created output directory
        Directories.CreateOutputFolderStructure(filesToProcess, inputDir, outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            Parallel.For(0, progressTracker.Total, po, (ctr, _) =>
            {
                var fileName = Path.GetFileName(filesToProcess[ctr]);
                var group = $"Task {ctr}";
                try
                {
                    var file = GameTitleRegistry.GetGameTitle(titleId);
                    file.FileData = File.ReadAllBytes(filesToProcess[ctr]);
                    logger.LogInfo($"{progressTracker} Decrypting the [{fileName}] file...", group);
                    file.Decrypt();
                    // Save the decrypted data to the output directory, preserving the folder structure
                    var outputFilePath = filesToProcess[ctr].Replace(inputDir, outputDir);
                    File.WriteAllBytes(outputFilePath, file.FileData);
                    logger.LogInfo($"{progressTracker} Decrypted the [{fileName}] file.", group);
                }
                catch (NotSupportedException ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to decrypt the [{fileName}] file: {ex.Message}", group);
                }
                catch (Exception ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to decrypt the [{fileName}] file: {ex}", group);
                }
                finally
                {
                    progressTracker.Increment();
                    progressReporter.Report(progressTracker.Percentage);
                }
            });
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }

    /// <summary>
    /// Asynchronously encrypts all .bin files found within the specified input directory and its subdirectories, saving the encrypted files to a newly created output directory while preserving the original folder structure.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing .bin files to encrypt. Must be a valid directory path.</param>
    /// <param name="titleId">The ID of the game title for which the save data files belong.</param>
    /// <param name="cts">The CancellationTokenSource used to support cancellation of the encryption operation.</param>
    public async Task EncryptFilesAsync(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
        => await Task.Run(() => EncryptFiles(inputDir, titleId, cts));

    /// <summary>
    /// Encrypts all .bin files found within the specified input directory and its subdirectories, saving the encrypted files to a newly created output directory while preserving the original folder structure.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing .bin files to encrypt. Must be a valid directory path.</param>
    /// <param name="titleId">The ID of the game title for which the save data files belong.</param>
    /// <param name="cts">The CancellationTokenSource used to support cancellation of the encryption operation.</param>
    public void EncryptFiles(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // ENCRYPT
        logger.LogInfo($"Encrypting [{progressTracker.Total}] files...");
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("encrypted");
        Directory.CreateDirectory(outputDir);
        // Crate the folder structure in the newly created output directory
        Directories.CreateOutputFolderStructure(filesToProcess, inputDir, outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            Parallel.For(0, progressTracker.Total, po, (ctr, _) =>
            {
                var fileName = Path.GetFileName(filesToProcess[ctr]);
                var group = $"Task {ctr}";
                try
                {
                    var file = GameTitleRegistry.GetGameTitle(titleId);
                    file.FileData = File.ReadAllBytes(filesToProcess[ctr]);
                    logger.LogInfo($"{progressTracker} Encrypting the [{fileName}] file...", group);
                    file.Encrypt();
                    // Save the encrypted data to the output directory, preserving the folder structure
                    var outputFilePath = filesToProcess[ctr].Replace(inputDir, outputDir);
                    File.WriteAllBytes(outputFilePath, file.FileData);
                    logger.LogInfo($"{progressTracker} Encrypted the [{fileName}] file.", group);
                }
                catch (NotSupportedException ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to encrypt the [{fileName}] file: {ex.Message}", group);
                }
                catch (Exception ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to encrypt the [{fileName}] file: {ex}", group);
                }
                finally
                {
                    progressTracker.Increment();
                    progressReporter.Report(progressTracker.Percentage);
                }
            });
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        { 
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }

    /// <summary>
    /// Asynchronously re-signs all .bin files found in the specified input directory and its subdirectories, saving the re-signed files to a new output directory with the folder structure preserved.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing .bin files to be re-signed. All matching files in this directory and its subdirectories will be processed.</param>
    /// <param name="userId">The user identifier used for re-signing the files. Must be a valid string representation of an unsigned 64-bit integer.</param>
    /// <param name="titleId">The ID of the game title for which the save data files belong.</param>
    /// <param name="cts">A CancellationTokenSource used to support cancellation of the re-signing operation. If cancellation is requested, the operation will terminate early.</param>
    public async Task ResignFilesAsync(string inputDir, string userId, GameTitleIdEnum titleId, CancellationTokenSource cts)
        => await Task.Run(() => ResignFiles(inputDir, userId, titleId, cts));

    /// <summary>
    /// Re-signs all .bin files found in the specified input directory and its subdirectories, saving the re-signed files to a new output directory with the folder structure preserved.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing .bin files to be re-signed. All matching files in this directory and its subdirectories will be processed.</param>
    /// <param name="userId">The user identifier used for re-signing the files. Must be a valid string representation of an unsigned 64-bit integer.</param>
    /// <param name="titleId">The ID of the game title for which the save data files belong.</param>
    /// <param name="cts">A CancellationTokenSource used to support cancellation of the re-signing operation. If cancellation is requested, the operation will terminate early.</param>
    public void ResignFiles(string inputDir, string userId, GameTitleIdEnum titleId, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // RE-SIGN
        logger.LogInfo($"Re-signing [{progressTracker.Total}] files...");
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("resigned").AddUserId(userId);
        Directory.CreateDirectory(outputDir);
        // Crate the folder structure in the newly created output directory
        Directories.CreateOutputFolderStructure(filesToProcess, inputDir, outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            Parallel.For(0, progressTracker.Total, po, (ctr, _) =>
            {
                var fileName = Path.GetFileName(filesToProcess[ctr]);
                var group = $"Task {ctr}";
                try
                {
                    var file = GameTitleRegistry.GetGameTitle(titleId);
                    file.FileData = File.ReadAllBytes(filesToProcess[ctr]);
                    logger.LogInfo($"{progressTracker} Re-signing the [{fileName}] file...", group);
                    file.Resign(ulong.Parse(userId));
                    // Save the re-signed data to the output directory, preserving the folder structure
                    var outputFilePath = filesToProcess[ctr].Replace(inputDir, outputDir);
                    File.WriteAllBytes(outputFilePath, file.FileData);
                    logger.LogInfo($"{progressTracker} Re-signed the [{fileName}] file.", group);
                }
                catch (NotSupportedException ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to re-sign the [{fileName}] file: {ex.Message}", group);
                }
                catch (Exception ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to re-sign the [{fileName}] file: {ex}", group);
                }
                finally
                {
                    progressTracker.Increment();
                    progressReporter.Report(progressTracker.Percentage);
                }
            });
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }
    
    /// <summary>
    /// Asynchronously retrieves the user identifier from the first file found in the specified input directory and its subdirectories.
    /// </summary>
    /// <param name="inputDir">The input directory containing the files to process.</param>
    /// <param name="titleId">The game title identifier.</param>
    /// <returns>The user identifier if found; otherwise, null.</returns>
    public async Task<ulong?> FindUserIdAsync(string inputDir, GameTitleIdEnum titleId)
        => await Task.Run(() => FindUserId(inputDir, titleId));

    /// <summary>
    /// Extracts the user identifier from the first file found in the specified input directory and its subdirectories.
    /// </summary>
    /// <param name="inputDir">The input directory containing the files to process.</param>
    /// <param name="titleId">The game title identifier.</param>
    /// <returns>The user identifier if found; otherwise, null.</returns>
    public ulong? FindUserId(string inputDir, GameTitleIdEnum titleId)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return null;
        }

        // GET USER ID FROM THE FIRST FILE
        var file = GameTitleRegistry.GetGameTitle(titleId);
        file.FileData = File.ReadAllBytes(filesToProcess[0]);
        var userId = file.GetUserId();
        logger.LogInfo($"Found UserID: {userId}.");
        return userId;
    }

    /// <summary>
    /// Asynchronously exports the JSON data from all files found in the specified input directory and its subdirectories.
    /// </summary>
    /// <param name="inputDir">The input directory containing the files to process.</param>
    /// <param name="titleId">The game title identifier.</param>
    /// <param name="cts">The cancellation token source.</param>
    public async Task ExportJsonAsync(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
        => await Task.Run(() => ExportJson(inputDir, titleId, cts));

    /// <summary>
    /// Exports the JSON data from all files found in the specified input directory and its subdirectories.
    /// </summary>
    /// <param name="inputDir">The input directory containing the files to process.</param>
    /// <param name="titleId">The game title identifier.</param>
    /// <param name="cts">The cancellation token source.</param>
    public void ExportJson(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // EXPORT
        logger.LogInfo($"Exporting JSON data from [{progressTracker.Total}] files...");
        // Create a JSON WORKSPACE directory
        var outputDir = Directories.JsonWorkspace;
        _ = Directories.RecreateDirectory(outputDir);
        // Crate the folder structure in the newly created directory
        Directories.CreateOutputFolderStructure(filesToProcess, inputDir, outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            const string jsonExt = ".json";
            Parallel.For(0, progressTracker.Total, po, (ctr, _) =>
            {
                var fileName = Path.GetFileName(filesToProcess[ctr]);
                var group = $"Task {ctr}";
                try
                {
                    var file = GameTitleRegistry.GetGameTitle(titleId);
                    file.FileData = File.ReadAllBytes(filesToProcess[ctr]);
                    logger.LogInfo($"{progressTracker} Exporting JSON data from the [{fileName}] file...", group);
                    var jsonData = file.ExportJson();
                    // Save the exported JSON data to the output directory, preserving the folder structure
                    var outputFilePath = Path.ChangeExtension(filesToProcess[ctr].Replace(inputDir, outputDir), jsonExt);
                    File.WriteAllBytes(outputFilePath, jsonData);
                    if (jsonData.Length == 0)
                        errorCounter.AddWarning($"{progressTracker} JSON data exported from the [{fileName}] file is empty.", group);
                    else
                        logger.LogInfo($"{progressTracker} Exported JSON data from the [{fileName}] file.", group);
                }
                catch (NotSupportedException ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to export JSON data from the [{fileName}] file: {ex.Message}", group);
                }
                catch (Exception ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to export JSON data from the [{fileName}] file: {ex}", group);
                }
                finally
                {
                    progressTracker.Increment();
                    progressReporter.Report(progressTracker.Percentage);
                }
            });
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }

    /// <summary>
    /// Asynchronously imports the JSON data into all files found in the specified input directory and its subdirectories.
    /// </summary>
    /// <param name="inputDir">The input directory containing the files to process.</param>
    /// <param name="titleId">The game title identifier.</param>
    /// <param name="cts">The cancellation token source.</param>
    public async Task ImportJsonAsync(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
        => await Task.Run(() => ImportJson(inputDir, titleId, cts));

    /// <summary>
    /// Imports the JSON data into all files found in the specified input directory and its subdirectories.
    /// </summary>
    /// <param name="inputDir">The input directory containing the files to process.</param>
    /// <param name="titleId">The game title identifier.</param>
    /// <param name="cts">The cancellation token source.</param>
    public void ImportJson(string inputDir, GameTitleIdEnum titleId, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // IMPORT
        logger.LogInfo($"Importing JSON data into [{progressTracker.Total}] files...");
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("importedJSON");
        Directory.CreateDirectory(outputDir);
        // Crate the folder structure in the newly created directory
        Directories.CreateOutputFolderStructure(filesToProcess, inputDir, outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            const string jsonExt = ".json";
            Parallel.For(0, progressTracker.Total, po, (ctr, _) =>
            {
                var fileName = Path.GetFileName(filesToProcess[ctr]);
                var group = $"Task {ctr}";
                try
                {
                    var file = GameTitleRegistry.GetGameTitle(titleId);
                    file.FileData = File.ReadAllBytes(filesToProcess[ctr]);
                    logger.LogInfo($"{progressTracker} Importing JSON data into the [{fileName}] file...", group);
                    var jsonFile = Path.ChangeExtension(filesToProcess[ctr].Replace(inputDir, Directories.JsonWorkspace), jsonExt);
                    var jsonData = File.ReadAllBytes(jsonFile);
                    file.ImportJson(jsonData);
                    // Save the imported JSON data to the output directory, preserving the folder structure
                    var outputFilePath = filesToProcess[ctr].Replace(inputDir, outputDir);
                    File.WriteAllBytes(outputFilePath, file.FileData);
                    logger.LogInfo($"{progressTracker} Imported JSON data into the [{fileName}] file.", group);
                }
                catch (NotSupportedException ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to import JSON data into the [{fileName}] file: {ex.Message}", group);
                }
                catch (Exception ex)
                {
                    errorCounter.AddError($"{progressTracker} Failed to import JSON data into the [{fileName}] file: {ex}", group);
                }
                finally
                {
                    progressTracker.Increment();
                    progressReporter.Report(progressTracker.Percentage);
                }
            });
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }
}