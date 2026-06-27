using System.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mi5hmasH.Progress;

namespace KatanaSaveDataResignerWpf.Helpers;

/// <summary>
/// Manages the "Super User" feature, which unlocks specific functionality after a certain number of user interactions within a specified time frame.
/// </summary>
public partial class SuperUserManager : ObservableObject
{
    private readonly ProgressReporter _progressReporter;
    private readonly DispatcherTimer _timer = new(DispatcherPriority.DataBind); // setting a higher priority is important!
    private readonly uint _superUserThreshold;
    private uint _superUserClicks;

    [ObservableProperty] private bool _isSuperUser;

    /// <summary>
    /// Initializes a new instance of the SuperUserManager class with the specified progress reporter and SuperUser threshold.
    /// </summary>
    /// <param name="progressReporter">The progress reporter used to report progress messages.</param>
    /// <param name="timeSpanMs">The time span in milliseconds within which the user must perform the required number of clicks to become a SuperUser.</param>
    /// <param name="superUserThreshold">The number of clicks required to become a SuperUser.</param>
    public SuperUserManager(ProgressReporter progressReporter, long timeSpanMs = 500, uint superUserThreshold = 3)
    {
        _progressReporter = progressReporter;
        _superUserThreshold = superUserThreshold;
        _timer.Interval = TimeSpan.FromMilliseconds(timeSpanMs);
        _timer.Tick += (_, _) => ResetCounter();
    }

    /// <summary>
    /// Handles the click event that contributes to becoming a SuperUser. If the user clicks the required number of times within the specified time frame, they become a SuperUser.
    /// </summary>
    [RelayCommand]
    public void SuperUserTriggerClick()
    {
        if (IsSuperUser) return;

        _superUserClicks++;
        _timer.Stop();

        if (_superUserClicks < _superUserThreshold) _timer.Start();
        else EnableSuperUser();
    }

    /// <summary>
    /// Resets the click counter and stops the timer. This method is called when the timer elapses without the user reaching the required number of clicks.
    /// </summary>
    private void ResetCounter()
    {
        _superUserClicks = 0;
        _timer.Stop();
    }

    /// <summary>
    /// Enables the SuperUser status, reports a message, and plays a sound. This method is called when the user successfully clicks the required number of times within the specified time frame.
    /// </summary>
    private void EnableSuperUser()
    {
        IsSuperUser = true;
        _progressReporter.Report("You're a SuperUser now! 🎉");
        // play sound
        SystemSounds.Beep.Play();
    }
}