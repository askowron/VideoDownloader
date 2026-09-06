# VideoDownloader

A Windows desktop app (.NET 8, WinForms) for downloading videos via [yt-dlp](https://github.com/yt-dlp/yt-dlp), with a stripped-down FFmpeg bundled for post-processing/remuxing.

## Features

- Paste a link (or let it auto-detect one from the clipboard) and pick a video/audio quality, or let it auto-select the best available.
- Batch downloads: drag-and-drop a list of URLs, or load them from a `.txt` file, to queue many downloads at once.
- Configurable number of simultaneous downloads.
- Per-job progress (percentage, speed, ETA) with cancel support.
- Automatically keeps the bundled `yt-dlp.exe` up to date (`yt-dlp -U`) on startup.
- UI available in English and Polish (auto-detected from the OS, or set manually).

## Solution structure

- **VideoDownloader** — the WinForms application (`net8.0-windows`). Uses [YoutubeDLSharp](https://www.nuget.org/packages/YoutubeDLSharp) to drive `yt-dlp.exe`, with a built-in updater (`Core/Tools/YtDlpUpdater.cs`) that runs `yt-dlp -U`.
- **FFmpegBuild** — native (vcxproj) project that produces the FFmpeg binaries bundled with the app. Not part of the default solution build; see `FFmpegBuild/README.md` for how to (re)build it.
- **VideoDownloader.Setup** — WiX installer project producing the MSI package.
- **ExternalLib** — third-party executables (`yt-dlp.exe`, `ffmpeg.exe`) copied into the build output on `PostBuild`.

## Requirements

- Windows
- .NET 8 SDK
- Visual Studio 2022 (17.14+) with the WiX Toolset extension for building the installer

## Building

Open `VideoDownloader.sln` in Visual Studio and build, or from the command line:

```
dotnet build VideoDownloader\VideoDownloader.csproj -c Release
```

The `PostBuild` target copies executables from `ExternalLib\` into the output directory automatically.

## Running

Run the built `VideoDownloader.exe`, or `dotnet run --project VideoDownloader` for development.

## License

MIT — see [LICENSE](LICENSE).
