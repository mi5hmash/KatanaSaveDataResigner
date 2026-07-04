using KatanaSaveDataResignerCore;
using KatanaSaveDataResignerCore.GameTitlesFactory;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using KatanaSaveDataResignerCore.Infrastructure;
using Mi5hmasH.AppInfo;
using Mi5hmasH.ConsoleHelper;
using Mi5hmasH.Logger;
using Mi5hmasH.Logger.Enums;
using Mi5hmasH.Logger.LogProvidersFactory.LogProviders;
using Mi5hmasH.Logger.Models;
using Mi5hmasH.Progress;

#region SETUP

// CONSTANTS
const string breakLine = "---";

// Initialize APP_INFO
var appInfo = new MyAppInfo("katana-savedata-resigner-cli");

// Initialize LOGGER
var logger = new SimpleLogger
{
    LoggedAppName = appInfo.Name
};
// Configure ConsoleLogProvider
var consoleLogProvider = new ConsoleLogProvider();
logger.AddProvider(consoleLogProvider);
// Configure FileLogProvider
var fileLogProvider = new FileLogProvider(MyAppInfo.RootPath, 2);
fileLogProvider.CreateLogFile();
logger.AddProvider(fileLogProvider);
// Add event handler for unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is not Exception exception) return;
    var logEntry = new LogEntry(LogSeverityEnum.Critical, $"Unhandled Exception: {exception}");
    fileLogProvider.Log(logEntry);
    fileLogProvider.Flush();
};
// Flush log providers on process exit
AppDomain.CurrentDomain.ProcessExit += (_, _) => logger.Flush();

//Initialize ProgressReporter
var progressReporter = new ProgressReporter(new Progress<string>(Console.WriteLine), null);

// Initialize CORE
var core = new Core(logger, progressReporter);

// Print HEADER
ConsoleHelper.PrintHeader(appInfo, breakLine);

// Say HELLO
ConsoleHelper.SayHello(breakLine);

// Get ARGUMENTS from command line
#if DEBUG
// For debugging purposes, you can manually set the arguments...
if (args.Length < 1)
{
    // ...below
    const string localArgs = "-m TEST";
    args = ConsoleHelper.GetArgs(localArgs);
}
#endif
var arguments = ConsoleHelper.ReadArguments(args);
#if DEBUG
// Write the arguments to the console for debugging purposes
ConsoleHelper.WriteArguments(arguments);
Console.WriteLine(breakLine);
#endif

#endregion

#region MAIN

// Optional argument: doNotWait
var doNotWait = arguments.ContainsKey("-q");

// Show HELP if no arguments are provided or if -h is provided
if (arguments.Count == 0 || arguments.ContainsKey("-h"))
{
    PrintHelp();
    goto EXIT;
}

// Get MODE
arguments.TryGetValue("-m", out var mode);
switch (mode)
{
    case "decrypt" or "d":
        await DecryptAll();
        break;
    case "encrypt" or "e":
        await EncryptAll();
        break;
    case "resign" or "r":
        await ResignAll();
        break;
    case "findUid" or "f":
        await FindUserId();
        break;
    case "exportJson" or "ej":
        await ExportJson();
        break;
    case "importJson" or "ij":
        await ImportJson();
        break;

    default:
        throw new ArgumentException($"Unknown mode: '{mode}'.");
}

// EXIT the application
EXIT:
Console.WriteLine(breakLine); // print a break line
ConsoleHelper.SayGoodbye(breakLine);
if (!doNotWait) ConsoleHelper.PressAnyKeyToExit();
return;

#endregion

#region HELPERS

static void PrintHelp()
{
    const string userId = "76561197960265729";
    var activeGameTitle = nameof(GameTitleIdEnum.Nioh).ToLower();
    var listOfActiveTitles = GameTitleRegistry.GameTitlesFriendlyNames
        .OrderBy(x => x.Value)
        .ToList();
    var maxKeyLength = listOfActiveTitles.Max(x => x.Key.ToString().Length);
    var inputPath = Path.Combine(".", "InputDirectory");
    var exeName = Path.Combine(".", Path.GetFileName(Environment.ProcessPath) ?? "ThisExecutableFileName.exe");
    var helpMessage = $"""
                       Usage: {exeName} -m <mode> [options]

                       Modes:
                         -m d   Decrypt SaveData files
                         -m e   Encrypt SaveData files
                         -m r   Re-sign SaveData files
                         -m f   Find User ID from the first SaveData file
                         -m ej  Export JSON data from SaveData files
                         -m ij  Import JSON data into SaveData files

                       Options:
                         -p <input_folder_path>  Path to folder containing SaveData files
                         -u <user_id>            User ID (used in re-sign mode)
                         -g <active_game_title>  Active Game Title
                         -q                      Don't wait for user input to exit after operation completes (auto-close)
                         -h                      Show this help message
                         
                       List of Game Titles:
                       {string.Join(
                           Environment.NewLine, 
                           listOfActiveTitles.Select(x => $"  {x.Key.ToString().PadRight(maxKeyLength).ToLower()} <- {x.Value}")
                           )}

                       Examples:
                         Decrypt:       {exeName} -m d -g {activeGameTitle} -p "{inputPath}"
                         Encrypt:       {exeName} -m e -g {activeGameTitle} -p "{inputPath}"
                         Re-sign:       {exeName} -m r -g {activeGameTitle} -p "{inputPath}" -u {userId}
                         Find User ID:  {exeName} -m f -g {activeGameTitle} -p "{inputPath}"
                         Export JSON:   {exeName} -m ej -g {activeGameTitle} -p "{inputPath}"
                         Import JSON:   {exeName} -m ij -g {activeGameTitle} -p "{inputPath}"
                       """;
    Console.WriteLine(helpMessage);
}

string GetValidatedInputRootPath()
{
    arguments.TryGetValue("-p", out var inputRootPath);
    if (File.Exists(inputRootPath)) inputRootPath = Path.GetDirectoryName(inputRootPath);
    return !Directory.Exists(inputRootPath)
        ? throw new DirectoryNotFoundException($"The provided path '{inputRootPath}' is not a valid directory or does not exist.")
        : inputRootPath.TrimDirectorySeparator();
}

GameTitleIdEnum GetGameTitleId()
{
    arguments.TryGetValue("-g", out var activeTitle);
    return (activeTitle ?? string.Empty).ToGameTitleId();
}

async Task CommonAction(Func<string, GameTitleIdEnum, CancellationTokenSource, Task> action)
{
    using var cts = new CancellationTokenSource();
    var inputRootPath = GetValidatedInputRootPath();
    var gameTitleId = GetGameTitleId();
    await action(inputRootPath, gameTitleId, cts);
}

#endregion

#region MODES

async Task DecryptAll() => await CommonAction(core.DecryptFilesAsync);

async Task EncryptAll() => await CommonAction(core.EncryptFilesAsync);

async Task ImportJson() => await CommonAction(core.ImportJsonAsync);

async Task ExportJson() => await CommonAction(core.ExportJsonAsync);

async Task ResignAll()
{
    using var cts = new CancellationTokenSource();
    arguments.TryGetValue("-u", out var userId);
    if (string.IsNullOrEmpty(userId))
        throw new ArgumentException("Output User ID is missing.");
    var inputRootPath = GetValidatedInputRootPath();
    var gameTitleId = GetGameTitleId();
    await core.ResignFilesAsync(inputRootPath, userId, gameTitleId, cts);
}

async Task FindUserId()
{
    var inputRootPath = GetValidatedInputRootPath();
    var gameTitleId = GetGameTitleId();
    await core.FindUserIdAsync(inputRootPath, gameTitleId);
}

#endregion