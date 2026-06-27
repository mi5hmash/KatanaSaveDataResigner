[![License: MIT](https://img.shields.io/badge/License-MIT-blueviolet.svg)](https://opensource.org/license/mit)
[![Release Version](https://img.shields.io/github/v/tag/mi5hmash/KatanaSaveDataResigner?label=Version)](https://github.com/mi5hmash/KatanaSaveDataResigner/releases/latest)
[![Visual Studio 2026](https://custom-icon-badges.demolab.com/badge/Visual%20Studio%202026-F0ECF8.svg?&logo=visual-studio-26)](https://visualstudio.microsoft.com/)
[![.NET10](https://img.shields.io/badge/.NET%2010-512BD4?logo=dotnet&logoColor=fff)](#)

> [!IMPORTANT]
> **This software is free and open source. If someone asks you to pay for it, it's likely a scam.**

# 🥷 KatanaSaveDataResigner - What is it :interrobang:
This application can **decrypt and encrypt SaveData files** from various games running on Katana Engine. It can also **re-sign these SaveData files** with your own SteamID to **use anyone’s SaveData on your Steam Account**. For games that store data in JSON format, the tool allows **exporting and importing JSON files**.

## Supported game titles
|Game Title|Platform|
|---|---|
|Fatal Frame II Crimson Butterfly REMAKE|PC|
|NIOH|PC|
|NIOH 2|PC|
|NIOH 3|PC|
|Rise of the Ronin|PC|
|Stranger of Paradise: Final Fantasy Origin|PC|
|Wo Long: Fallen Dynasty|PC, PS4|

# 🤯 Why was it created :interrobang:
I wanted to share a SaveData file with a friend, but it isn't possible by default.

# :scream: Is it safe?
The short answer is: **No.** 
> [!CAUTION]
> If you unreasonably edit your SaveData files, you risk corrupting them or getting banned from playing online. In both cases, you will lose your progress.

> [!IMPORTANT]
> Always create a backup of any files before editing them.

> [!IMPORTANT]
> Disable the Steam Cloud before you replace any SaveData files.

You’ve been warned. Now that you fully understand the possible consequences, you may proceed to the next chapter.

# :scroll: How to use this tool
## [GUI] - 🪟 Windows 
> [!IMPORTANT]
> If you’re working on Linux or macOS, skip this chapter and move on to the next one.

On Windows, you can use either the CLI or the GUI version, but in this chapter I’ll describe the latter.

<img src="https://github.com/mi5hmash/KatanaSaveDataResigner/blob/main/.resources/images/MainWindow-v2.png" alt="MainWindow-v2"/>

#### 1. Selecting the Game Profile
Game Profile is a configuration that stores the settings for a specific game.
In plain terms, it tells my application how it should behave for that particular game.
This tool ships with these Game Profiles built in.
Use the ComboBox **(5)** to select the Game Profile you want.

#### 2. Setting the Input Directory
You can set the input folder in whichever way feels most convenient:
- **Drag & drop:** Drop SaveData file - or the folder - onto the TextBox **(1)**.
- **Pick a folder manually:** Click the button **(2)** to open a folder‑picker window and browse to the directory where SaveData file is.
- **Type it in:** If you already know the path, simply enter it directly into the TextBox **(1)**.

#### 3. Entering the User ID
Enter the User ID of the user who should become the new owner of the SaveData file into TextBox **(3)**.
In the case of Steam, your User ID is 64-bit SteamID.  
One way to find it is by using the SteamDB calculator at [steamdb.info](https://steamdb.info/calculator/).
If you don’t know the owner of a given file, you can try to find it using the Button **(4)**.

#### 4. Re-signing SaveData files
If you want to re‑sign your SaveData file/s so it works on another Steam account, select the Game Profile **(5)** corresponding to the game from which the save file comes. Once you have it selected, enter the User ID of the account that should become a new owner, into the TextBox **(3)**. Finally, press the **"Re-sign All"** button **(9)**.

> [!NOTE]
> The re‑signed files will be placed in a newly created folder within the ***"KatanaSaveDataResigner/_OUTPUT/"*** folder.

#### 5. Accessing modified files
Modified files are being placed in a newly created folder within the ***"KatanaSaveDataResigner/_OUTPUT/"*** folder. You may open this directory in a new File Explorer window by pressing the button **(10)**.

> [!NOTE]
> After you locate the modified files, you can copy them into your save‑game folder.

### ADVANCED OPERATIONS

#### Enabling SuperUser Mode

> [!WARNING]
> This mode is for advanced users only.

If you really need it, you can enable SuperUser mode by triple-clicking the version number label **(11)**.

#### Decrypting SaveData files

> [!IMPORTANT]
> This button is visible only when the SuperUser Mode is Enabled. 

If you want to decrypt SaveData file\s to read its content, select the Game Profile **(5)** corresponding to the game from which the SaveData file comes, and press the **"Decrypt All"** button **(6)**.

#### Encrypting SaveData files

> [!IMPORTANT]
> This button is visible only when the SuperUser Mode is Enabled. 

If you want to encrypt the decrypted SaveData file\s, select the Game Profile **(5)** corresponding to the game from which the SaveData file comes, enter the User ID of the account that should be allowed to use that SaveData file/s into the TextBox **(3)**, and press the **"Encrypt All"** button **(7)**.

#### Exporting JSON data from SaveData files

> [!IMPORTANT]
> This button is visible only when the SuperUser Mode is Enabled and the selected Game Title supports JSON format.

If you want to export the JSON data from SaveData file\s, select the Game Profile **(5)** corresponding to the game from which the SaveData file comes, and press the **"Export JSON(s)"** button **(12)**.

> [!NOTE]
> JSON files will be exported to the ***"KatanaSaveDataResigner/_JsonWorkspace/"*** folder.

#### Importing JSON data into SaveData files

> [!IMPORTANT]
> This button is visible only when the SuperUser Mode is Enabled and the selected Game Title supports JSON format.

If you want to import the JSON data into SaveData file\s, select the Game Profile **(5)** corresponding to the game from which the SaveData file comes, enter the User ID of the account that should be allowed to use that SaveData file/s into the TextBox **(3)**, and press the **"Import JSON(s)"** button **(14)**.

> [!IMPORTANT]
> This option will merge JSON data from ***"KatanaSaveDataResigner/_JsonWorkspace/"*** folder into the save files located in **InputDirectory**, provided that both directories share the same folder structure.

> [!NOTE]
> The files with imported JSON data will be placed in a newly created folder within the ***"KatanaSaveDataResigner/_OUTPUT/"*** folder.

### OTHER BUTTONS
Button **(8)** cancels the currently running operation.
Button **(13)** opens ***"KatanaSaveDataResigner/_JsonWorkspace/"*** folder in a new File Explorer window.

## [CLI] - 🪟 Windows | 🐧 Linux | 🍎 macOS

```plaintext
Usage: .\katana-savedata-resigner-cli.exe -m <mode> [options]

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
  wolong    <- [PC] Wo Long: Fallen Dynasty
  wolongps4 <- [PS4] Wo Long: Fallen Dynasty
  ff2cbr    <- FATAL FRAME II - Crimson Butterfly REMAKE
  nioh      <- NIOH
  nioh2     <- NIOH 2
  nioh3     <- NIOH 3
  ronin     <- Rise of the Ronin
  sopffo    <- STRANGER OF PARADISE FINAL FANTASY ORIGIN
```

### Examples
#### Decrypt
```bash
.\katana-savedata-resigner-cli.exe -m d -g nioh -p ".\InputDirectory"
```
#### Encrypt
```bash
.\katana-savedata-resigner-cli.exe -m e -g nioh -p ".\InputDirectory"
```
#### Re-sign
```bash
.\katana-savedata-resigner-cli.exe -m r -g nioh -p ".\InputDirectory" -u 76561197960265729
```
#### Find User ID
```bash
.\katana-savedata-resigner-cli.exe -m f -g nioh -p ".\InputDirectory"
```
#### Export JSON
```bash
.\katana-savedata-resigner-cli.exe -m ej -g nioh -p ".\InputDirectory"
```

> [!NOTE]
> JSON files will be exported to the ***"KatanaSaveDataResigner/_JsonWorkspace/"*** folder.

#### Import JSON
```bash
.\katana-savedata-resigner-cli.exe -m ij -g nioh -p ".\InputDirectory"
```

> [!IMPORTANT]
> This option will merge JSON data from ***"KatanaSaveDataResigner/_JsonWorkspace/"*** folder into the save files located in **InputDirectory**, provided that both directories share the same folder structure.

> [!NOTE]
> Modified files are being placed in a newly created folder within the ***"KatanaSaveDataResigner/_OUTPUT/"*** folder.

# :fire: Issues
All the problems I've encountered during my tests have been fixed on the go. If you find any other issues (which I hope you won't) feel free to report them [there](https://github.com/mi5hmash/KatanaSaveDataResigner/issues).

> [!TIP]
> This application creates a log file that may be helpful in troubleshooting.
It can be found in the same directory as the executable file.
Application stores up to two log files from the most recent sessions.

## [ISSUE] Not all controls are visible in the WPF application on Windows
You probably have your Windows system font size set higher than the default.
Set the font size back to the default value, or press **`CTRL + SHIFT + J`** to unlock window resizing in the application.