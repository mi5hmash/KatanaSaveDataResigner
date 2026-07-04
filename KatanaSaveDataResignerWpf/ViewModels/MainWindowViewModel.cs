using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Media;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KatanaSaveDataResignerCore;
using KatanaSaveDataResignerCore.GameTitlesFactory;
using KatanaSaveDataResignerCore.GameTitlesFactory.Enums;
using KatanaSaveDataResignerCore.Infrastructure;
using KatanaSaveDataResignerWpf.Fonts;
using KatanaSaveDataResignerWpf.Helpers;
using KatanaSaveDataResignerWpf.Settings;
using Mi5hmasH.AppInfo;
using Mi5hmasH.AppSettings;
using Mi5hmasH.AppSettings.FlavorsFactory.Flavors;
using Mi5hmasH.Logger;
using Mi5hmasH.Logger.Enums;
using Mi5hmasH.Logger.LogProvidersFactory.LogProviders;
using Mi5hmasH.Logger.Models;
using Mi5hmasH.Progress;
using Microsoft.Win32;

namespace KatanaSaveDataResignerWpf.ViewModels;

public partial class MainWindowViewModel : ObservableValidator
{
    #region APP_INFO
    public readonly MyAppInfo AppInfo = new("KatanaSaveDataResigner");
    public string AppTitle => AppInfo.Name;
    public static string AppAuthor => MyAppInfo.Author;
    public static string AppVersion => $"v{MyAppInfo.Version}";

    [RelayCommand] private static void VisitAuthorsGithub() => Urls.OpenAuthorsGithub();
    [RelayCommand] private static void VisitProjectsRepo() => Urls.OpenProjectsRepo();
    #endregion

    #region ICONS
    public static string DecryptIcon => IconFont.Decrypt;
    public static string EncryptIcon => IconFont.Encrypt;
    public static string ExportIcon => IconFont.Export;
    public static string FolderIcon => IconFont.Folder;
    public static string FolderSymlinkIcon => IconFont.FolderSymlink;
    public static string GithubIcon => IconFont.Github;
    public static string ImportIcon => IconFont.Import;
    public static string KeyIcon => IconFont.Key;
    public static string ResignIcon => IconFont.Resign;
    public static string XCircleIcon => IconFont.XCircle;
    #endregion

    #region UI_STATE

    [ObservableProperty] 
    public partial bool IsBusy { get; set; }
    [ObservableProperty] 
    public partial bool IsAbortAllowed { get; set; }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDoJson));
    }
    #endregion

    #region PROGRESS_REPORTER
    [ObservableProperty] 
    public partial int ProgressValue { get; set; }
    [ObservableProperty] 
    public partial string ProgressText { get; set; } = "Loading...";
    private readonly ProgressReporter _progressReporter;
    #endregion

    #region LOGGER
    private readonly SimpleLogger _logger;
    private void InitializeLogger()
    {
        // Configure StatusBarLogProvider
        var statusBarLogProvider = new StatusBarLogProvider(_progressReporter.Report);
        _logger.AddProvider(statusBarLogProvider);
        // Configure FileLogProvider
        var fileLogProvider = new FileLogProvider(MyAppInfo.RootPath, 2);
        fileLogProvider.CreateLogFile();
        _logger.AddProvider(fileLogProvider);
        // Add event handler for unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is not Exception exception) return;
            var logEntry = new LogEntry(LogSeverityEnum.Critical, $"Unhandled Exception: {exception}");
            fileLogProvider.Log(logEntry);
            fileLogProvider.Flush();
        };
        // Flush log providers on process exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) => _logger.Flush();
    }
    #endregion

    #region USER_ID
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [Range(0, ulong.MaxValue)] 
    public partial string UserId { get; set; } = "0";
    #endregion

    #region INPUT_FOLDER_PATH
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    public partial string InputFolderPath { get; set; } = MyAppInfo.RootPath.TrimDirectorySeparator();
    
    partial void OnInputFolderPathChanged(string value)
    {
        if (Directory.Exists(value))
        {
            value = value.TrimDirectorySeparator();
            InputFolderPath = value;
            return;
        }
        if (File.Exists(value))
        {
            InputFolderPath = Path.GetDirectoryName(value) ?? string.Empty;
            _progressReporter.Report("Input Folder Path is valid.");
            return;
        }
        _progressReporter.Report("Invalid Input Folder Path!");
        InputFolderPath = string.Empty;
    }

    [RelayCommand]
    private void SelectInputFolderPath()
    {
        OpenFileDialog openFileDialog = new()
        {
            InitialDirectory = InputFolderPath,
            Filter = "All Files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true) InputFolderPath = openFileDialog.FileName;
    }
    #endregion

    #region OUTPUT_FOLDER_PATH
    [RelayCommand]
    private static void OpenOutputDirectory()
        => Directories.OpenDirectory(Directories.Output);
    #endregion

    #region JSON_WORKSPACE_FOLDER_PATH
    [RelayCommand]
    private static void OpenJsonWorkspaceDirectory()
        => Directories.OpenDirectory(Directories.JsonWorkspace);
    #endregion

    #region APP_SETTINGS
    private readonly AppSettingsManager<MyAppSettings, Json> _appSettingsManager;
    private void InitializeAppSettings()
    {
        _appSettingsManager.SetEncryptor("ed+Qt3UXB3ZiMWEe1yUFOft+MFlmPtJY5yBY8Vpgico=");
        try { _appSettingsManager.Load(); }
        catch {
            // ignore
        }
        // Apply loaded settings
        LoadAppSettings();
        // Save settings on exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) => SaveAppSettings();
    }
    private void LoadAppSettings()
    {
        UserId = _appSettingsManager.Settings.UserId.ToString();
        SuperUserManager.IsSuperUser = _appSettingsManager.Settings.IsSu;
    }
    private void SaveAppSettings()
    {
        if (!HasErrors)
        {
            _appSettingsManager.Settings.UserId = Convert.ToUInt64((string?)UserId);
        }
        _appSettingsManager.Settings.IsSu = SuperUserManager.IsSuperUser;
        _appSettingsManager.Save();
    }
    #endregion

    #region GAME_TITLE
    [ObservableProperty] 
    public partial List<KeyValuePair<GameTitleIdEnum, string>> GameTitles { get; set; } = GameTitleRegistry.GameTitlesFriendlyNames
        .OrderBy(x => x.Value)
        .ToList();
    [ObservableProperty] 
    public partial GameTitleIdEnum SelectedGameTitle { get; set; }

    private SaveDataFormatEnum _saveDataFormat;
    public bool CanDoJson => SuperUserManager.IsSuperUser && 
                             _saveDataFormat == SaveDataFormatEnum.Json;
    partial void OnSelectedGameTitleChanged(GameTitleIdEnum value)
    {
        _saveDataFormat = GameTitleRegistry.GetGameTitleSaveDataFormat(value);
        OnPropertyChanged(nameof(CanDoJson));
    }
    #endregion

    #region FILE_DROP
    public void OnFileDrop(string operationType, StringCollection filePaths)
    {
        if (filePaths.Count < 1) return;
        if (operationType == "GetInputPath") InputFolderPath = filePaths[0] ?? string.Empty;
    }
    #endregion

    #region SUPERUSER
    [ObservableProperty] 
    public partial SuperUserManager SuperUserManager { get; set; }
    private void OnSuperUserManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SuperUserManager.IsSuperUser))
            OnPropertyChanged(nameof(CanDoJson));
    }
    #endregion

    #region WINDOW_RESIZE_UNLOCK
    [RelayCommand]
    private static void UnlockWindowResize(Window window)
        => window.ResizeMode = ResizeMode.CanResizeWithGrip;
    #endregion

    private CancellationTokenSource _cts = new();
    private readonly Core _core;
    
    public MainWindowViewModel()
    {
        // Initialize ProgressReporter
        _progressReporter = new ProgressReporter(
            new Progress<string>(s => ProgressText = s),
            new Progress<int>(i => ProgressValue = i)
        );
        // Initialize Logger
        _logger = new SimpleLogger
        {
            LoggedAppName = AppInfo.Name
        };
        InitializeLogger();
        // Initialize Core
        _core = new Core(_logger, _progressReporter);
        // Initialize SuperUserManager
        SuperUserManager = new SuperUserManager(_progressReporter);
        SuperUserManager.PropertyChanged += OnSuperUserManagerPropertyChanged;
        // Initialize AppSettings
        _appSettingsManager = new AppSettingsManager<MyAppSettings, Json>(null, MyAppInfo.RootPath);
        InitializeAppSettings();
        // Set initial game title
        OnSelectedGameTitleChanged(SelectedGameTitle);
        // Finalize setup
        _progressReporter.Report("Ready", 100);
    }

    #region ACTIONS

    public bool CanSubmit => !HasErrors;

    [RelayCommand]
    public void AbortAction()
    {
        if (!IsAbortAllowed || !IsBusy) return;
        _cts.Cancel();
    }

    private async Task PerformAction(Func<Task> function, bool canBeAborted = false)
    {
        if (IsBusy) return;
        if (!CanSubmit) return;
        IsBusy = true;
        if (canBeAborted) IsAbortAllowed = true;
        try
        {
            await function();
        }
        finally
        {
            // play sound
            if (_cts.IsCancellationRequested)
                SystemSounds.Beep.Play();
            else
            {
                using var sp = new SoundPlayer(Properties.Resources.typewriter_machine);
                sp.Play();
            }
            // reset flags
            if (canBeAborted) IsAbortAllowed = false;
            IsBusy = false;

            // flush logs
            await _logger.FlushAsync();
        }
    }
    
    [RelayCommand]
    private async Task DecryptAllAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.DecryptFilesAsync(InputFolderPath, SelectedGameTitle, _cts), true);
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task EncryptAllAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.EncryptFilesAsync(InputFolderPath, SelectedGameTitle, _cts), true);
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task ResignAllAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.ResignFilesAsync(InputFolderPath, UserId, SelectedGameTitle, _cts), true);
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task FindUserIdAsync()
    {
        _cts = new CancellationTokenSource();
        var userId = await _core.FindUserIdAsync(InputFolderPath, SelectedGameTitle);
        if (userId != null) UserId = userId.ToString()!;
        SystemSounds.Beep.Play();
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task ExportJsonAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.ExportJsonAsync(InputFolderPath, SelectedGameTitle, _cts), true);
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task ImportJsonAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.ImportJsonAsync(InputFolderPath, SelectedGameTitle, _cts), true);
        _cts.Dispose();
    }

    #endregion
}