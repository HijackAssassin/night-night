# Night-Night

A small Windows app that shuts your PC down at a time you pick, or after a countdown - handy when you fall asleep to
a movie or a long download. The shutdown is run by the app's own timer, so closing the app always cancels it, and
it keeps the PC awake until then.

**Download:** https://teamwolfgamers.online/night-night

The app checks that page for a newer version each time it starts (pattywatty.duckdns.org is the backup address).

## Building

Windows only, nothing to install: it is compiled with the C# compiler that comes with Windows (.NET Framework 4,
`csc.exe`). `build-app.bat <version> <project folder>` builds `<project folder>\Build\Night-Night.exe` with the
version stamped in; by hand:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:appicon.ico /out:Night-Night.exe NightNight.cs UpdateCheck.cs
```

## More

Made by Patrick - more apps and tools at https://teamwolfgamers.online
