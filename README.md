# Roblox Farm

A harmless popup prank for **Windows** and **Mac**. It opens a bunch of little Notepad-style
windows all over the screen and keeps filling them with `677777` as if someone were pasting it
over and over.

**Press `Shift` + `1` to stop it.** It also closes if you quit it (⌘Q on Mac) or end it from Task
Manager / Force Quit.

## What it does and doesn't do

- It only draws its own windows. It does **not** type into other programs, touch the clipboard,
  read or change any files, or install anything.
- It does **not** start by itself — you launch it, and it keeps going until you press Shift+1.
- It caps out at 150 windows so it can't run the machine out of memory.

Run it on **your own computer**, or on a friend's **while you're there and they're in on the
joke** so they can stop it with Shift+1. Don't put it on someone's machine without them knowing.

## Download

From the [Releases page](../../releases/latest):

- **Windows 10/11:** `RobloxFarm.exe`. Just run it. (SmartScreen may warn — More info → Run anyway.)
- **Mac (macOS 13+):** `RobloxFarm-mac.zip`. Unzip it, then right-click `Roblox Farm` → **Open** → **Open**
  (it isn't notarized, so a plain double-click is blocked the first time).

## Build it yourself

- **Mac** (Xcode command-line tools): `mac/build-app.sh` → `mac/dist/Roblox Farm.app`. Quick run:
  `swiftc mac/main.swift -o /tmp/robloxfarm && /tmp/robloxfarm --seconds 10`.
- **Windows** ([.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)):
  `dotnet publish windows/RobloxFarm.csproj -c Release -o windows/publish` → `RobloxFarm.exe`.

Both take `--seconds N` to stop on their own after N seconds, which is handy while testing.
