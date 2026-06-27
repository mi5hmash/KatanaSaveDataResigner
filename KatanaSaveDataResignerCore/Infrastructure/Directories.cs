using System.Diagnostics;

namespace KatanaSaveDataResignerCore.Infrastructure;

/// <summary>
/// Provides utility methods and properties for managing application directory paths, including the root and output directories.
/// </summary>
public static class Directories
{
    public static readonly string RootPath = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string Output = Path.Combine(RootPath, "_OUTPUT");
    public static readonly string JsonWorkspace = Path.Combine(RootPath, "_JsonWorkspace");

    /// <summary>
    /// Generates a new output directory path using the current date and time, combined with the specified action name.
    /// </summary>
    /// <param name="action">The name of the action to include in the output directory path.</param>
    /// <returns>A string representing the full path of the new output directory, formatted with the current date, time, and the specified action.</returns>
    public static string GetNewOutputDirectory(string action)
        => Path.Combine(Output, $"{DateTime.Now:yyyy-MM-dd_HHmmssfff}_{action}");

    /// <summary>
    /// Combines the specified output directory path with a user identifier to create a user-specific subdirectory path.
    /// </summary>
    /// <param name="outputDirectory">The base directory path where user-specific subdirectories will be created.</param>
    /// <param name="userId">The user identifier to append to the output directory path.</param>
    /// <returns>A string representing the combined path of the output directory and user identifier.</returns>
    public static string AddUserId(this string outputDirectory, string userId)
        => Path.Combine(outputDirectory, userId);

    /// <summary>
    /// Creates the output folder structure by replicating the parent directories of the specified input files under the given output directory.
    /// </summary>
    /// <param name="filesToProcess">An array of file paths representing the files to process. Each file's parent directory will be recreated under the output directory.</param>
    /// <param name="inputRootPath">The root path of the input directory structure. This path is replaced with the output directory when creating the new folder structure.</param>
    /// <param name="outputDirectory">The path to the root output directory where the folder structure will be created.</param>
    public static void CreateOutputFolderStructure(string[] filesToProcess, string inputRootPath, string outputDirectory)
    {
        var uniqueParentDirectories = filesToProcess
            .Select(Path.GetDirectoryName)
            .Where(dir => dir != null)
            .Distinct()
            .Select(dir => dir?.Replace(inputRootPath, outputDirectory))
            .ToArray();
        foreach (var dir in uniqueParentDirectories)
        {
            if (dir == null) continue;
            Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// Removes trailing directory separator characters from a path.
    /// </summary>
    /// <param name="path">The path to trim.</param>
    /// <returns>The path without trailing directory separators.</returns>
    public static string TrimDirectorySeparator(this string path)
        => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>
    /// Checks if the specified directory exists and recreates it by deleting and creating it again.
    /// </summary>
    /// <param name="path">The path of the directory to recreate.</param>
    /// <returns><see langword="true"/> if the directory was successfully recreated; otherwise, <see langword="false"/>.</returns>
    public static bool RecreateDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Opens the specified directory in the system's default file explorer, if the directory exists.
    /// </summary>
    /// <param name="path">The full path of the directory to open.</param>
    public static void OpenDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string? openCmd = null;
        string? args = null;

        Directory.CreateDirectory(path);
        if (OperatingSystem.IsWindows())
        {
            openCmd = "explorer.exe";
            args = $"\"{path}\"";
        }
        else if (OperatingSystem.IsMacOS())
        {
            openCmd = "open";
            args = $"\"{path}\"";
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            openCmd = "xdg-open";
            args = $"\"{path}\"";
        }

        if (openCmd != null && args != null)
            Process.Start(new ProcessStartInfo
            {
                FileName = openCmd,
                Arguments = args,
                UseShellExecute = false
            });
    }
}