[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"

# ---- Konfiguracja ------------------------------------------------------
# Sprawdź aktualny tag wydania: https://github.com/FFmpeg/FFmpeg/tags
$FFmpegTag   = "n7.1"

$ProjectDir  = $PSScriptRoot
$SrcDir      = Join-Path $ProjectDir "src"
$SolutionDir = Split-Path $ProjectDir -Parent
$OutputDir   = Join-Path $SolutionDir "ExternalLib"
$OutputExe   = Join-Path $OutputDir "ffmpeg.exe"

$Msys2Root = $env:MSYS2_ROOT
if (-not $Msys2Root) { $Msys2Root = "C:\msys64" }
$Bash = Join-Path $Msys2Root "usr\bin\bash.exe"

if (-not (Test-Path $Bash)) {
    Write-Error (
        "Nie znaleziono MSYS2 w '$Msys2Root'. Zainstaluj MSYS2 (https://www.msys2.org/), a nastepnie w konsoli MSYS2 uruchom:`n" +
        "  pacman -S --needed base-devel mingw-w64-x86_64-toolchain mingw-w64-x86_64-nasm git diffutils make pkg-config`n" +
        "Jesli MSYS2 jest zainstalowane w innym miejscu, ustaw zmienna srodowiskowa MSYS2_ROOT."
    )
    exit 1
}

function ConvertTo-UnixPath([string]$WinPath) {
    $p = $WinPath -replace '\\', '/'
    if ($p -match '^([A-Za-z]):(.*)$') {
        return "/{0}{1}" -f $Matches[1].ToLower(), $Matches[2]
    }
    return $p
}

$SrcDirUnix = ConvertTo-UnixPath $SrcDir

# ---- Clean / Rebuild -----------------------------------------------------
if ($Clean -or $Rebuild) {
    if (Test-Path (Join-Path $SrcDir "Makefile")) {
        Write-Host "Czyszczenie katalogu zrodlowego ffmpeg (make distclean)..."
        & $Bash -lc "export MSYSTEM=MINGW64; source /etc/profile; cd '$SrcDirUnix' && make distclean"
    }
    if ($Clean) {
        Write-Host "Wyczyszczono."
        return
    }
}

# ---- Pobranie zrodel -------------------------------------------------
if (-not (Test-Path (Join-Path $SrcDir ".git"))) {
    Write-Host "Klonowanie zrodel FFmpeg ($FFmpegTag)..."
    git clone --branch $FFmpegTag --depth 1 https://github.com/FFmpeg/FFmpeg.git $SrcDir
}

# ---- Flagi configure ---------------------------------------------------
# Cel: sam remux (stream copy) kontenerow mp4/webm/mkv z kodekami spotykanymi
# w formatach YouTube. Brak sieci, urzadzen, ffplay/ffprobe, dokumentacji.
# avfilter zostaje wlaczony w minimalnym zakresie - wymaga go sam program ffmpeg.c.
$ConfigureFlags = @(
    "--disable-everything",
    "--disable-doc", "--disable-htmlpages", "--disable-manpages", "--disable-podpages", "--disable-txtpages",
    "--disable-avdevice", "--disable-postproc", "--disable-network",
    "--disable-hwaccels", "--disable-indevs", "--disable-outdevs",
    "--disable-ffplay", "--disable-ffprobe",
    "--enable-ffmpeg",
    "--enable-protocol=file",
    "--enable-demuxer=mov,matroska,webm",
    "--enable-muxer=mp4,webm,matroska",
    "--enable-parser=h264,hevc,vp8,vp9,av1,aac,opus,vorbis",
    "--enable-bsf=h264_mp4toannexb,hevc_mp4toannexb,aac_adtstoasc,extract_extradata",
    # ffmpeg.c wymaga avfiltra i tego konkretnego zestawu filtrow (ffmpeg_select w configure)
    "--enable-filter=aformat,anull,atrim,crop,format,hflip,null,rotate,transpose,trim,vflip",
    "--disable-debug",
    "--disable-shared",
    "--enable-static",
    "--extra-cflags=-static",
    "--extra-ldflags=-static"
) -join " "

$Jobs = $env:NUMBER_OF_PROCESSORS
if (-not $Jobs) { $Jobs = 4 }

$BuildCmd = "export MSYSTEM=MINGW64; source /etc/profile; cd '$SrcDirUnix' && ./configure $ConfigureFlags && make -j$Jobs"

Write-Host "Konfiguracja i kompilacja FFmpeg - to moze potrwac kilka do kilkunastu minut..."
& $Bash -lc $BuildCmd
if ($LASTEXITCODE -ne 0) { throw "Kompilacja FFmpeg nie powiodla sie (kod $LASTEXITCODE)." }

# ---- Strip i kopiowanie wyniku ----------------------------------------
Write-Host "Usuwanie symboli debug (strip)..."
& $Bash -lc "export MSYSTEM=MINGW64; source /etc/profile; cd '$SrcDirUnix' && strip ffmpeg.exe"

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Copy-Item -Path (Join-Path $SrcDir "ffmpeg.exe") -Destination $OutputExe -Force

$sizeMB = [math]::Round((Get-Item $OutputExe).Length / 1MB, 2)
Write-Host "Gotowe: $OutputExe ($sizeMB MB)"
