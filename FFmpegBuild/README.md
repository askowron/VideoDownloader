# FFmpegBuild

Projekt typu *Makefile* (nie kompiluje niczego przez MSBuild/C++), ktory
buduje wlasny, okrojony `ffmpeg.exe` ze zrodel FFmpeg i wrzuca go do
`ExternalLib\ffmpeg.exe`, skad `VideoDownloader.csproj` kopiuje go juz
automatycznie (`PostBuild` -> `xcopy ExternalLib\*.exe`).

Build jest ograniczony wylacznie do **remuxu** (stream copy) kontenerow
mp4/webm/mkv z kodekami spotykanymi w formatach YouTube (h264, hevc, vp8,
vp9, av1, aac, opus, vorbis) - bez transkodowania, filtrow, urzadzen,
sieci, ffplay/ffprobe i dokumentacji. Wynikowy plik powinien miec
kilka-kilkanascie MB zamiast ~200 MB peinego builda.

## Wymagania (jednorazowo)

1. Zainstaluj [MSYS2](https://www.msys2.org/).
2. W konsoli MSYS2 (UCRT64/MSYS) zainstaluj toolchain:
   ```
   pacman -S --needed base-devel mingw-w64-x86_64-toolchain mingw-w64-x86_64-nasm git diffutils make pkg-config
   ```
3. Jesli MSYS2 zainstalowane jest poza `C:\msys64`, ustaw zmienna
   srodowiskowa `MSYS2_ROOT` na wlasciwa sciezke.

## Uzycie

- **Build** (w Visual Studio: PPM na projekcie -> Build) - klonuje zrodla
  FFmpeg (jesli jeszcze nie pobrane), konfiguruje i kompiluje. Pierwsza
  kompilacja trwa kilka-kilkanascie minut, kolejne (bez `Rebuild`) sa
  przyrostowe dzieki `make`.
- **Rebuild** - `make distclean` + pelna kompilacja od zera.
- **Clean** - tylko `make distclean`, bez ponownej kompilacji.

Projekt **nie jest** dolaczony do standardowego "Build Solution" (patrz
`VideoDownloader.sln`), zeby nie wymagac MSYS2 na kazdej maszynie
deweloperskiej ani nie wydluzac zwyklego builda C#. Uruchamiaj go
recznie, gdy chcesz odswiezyc `ffmpeg.exe`.

## Zmiana wersji / zestawu formatow

- Tag zrodel FFmpeg: zmienna `$FFmpegTag` na gorze `build.ps1`
  (aktualne tagi: https://github.com/FFmpeg/FFmpeg/tags).
- Obslugiwane kontenery/kodeki: lista `--enable-demuxer` / `--enable-muxer`
  / `--enable-parser` / `--enable-bsf` w `$ConfigureFlags` w `build.ps1`.
  Jesli program zacznie wybierac formaty spoza tej listy, merge w yt-dlp
  zacznie sie wywalac - trzeba dopisac brakujacy demuxer/parser.

## Katalog `src`

Tworzony automatycznie przy pierwszym buildzie (`git clone` zrodel
FFmpeg). Nie jest czescia repozytorium projektu - mozna go bezpiecznie
usunac, zostanie sklonowany ponownie przy kolejnym buildzie.
