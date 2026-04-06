# ChromiumBrowserFixed

Windows browser application built with .NET 8 WinForms and CefSharp.WinForms.NETCore.

## Build

```bash
dotnet restore ChromiumBrowserFixed/ChromiumBrowserFixed.csproj
dotnet publish ChromiumBrowserFixed/ChromiumBrowserFixed.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
```
