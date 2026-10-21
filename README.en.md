# 伪迁移助手 (Pseudo Migration Assistant)

[简体中文](README.md) | **English**

> Move a bulky folder off your C: drive while keeping the original path working.
> A directory junction is left behind, so programs keep reading and writing the same path —
> the data just lives on another disk now.

![Screenshot](assets/ui-home.png)

## The problem

Your C: drive keeps filling up, but a lot of folders **cannot be deleted**: app configs,
local caches, chat history, game assets. Delete them and the app breaks; keep them and
you are down tens of gigabytes.

This tool moves the whole folder to another disk, then creates a **junction** at the
original location:

```
before   C:\Users\me\AppData\Roaming\Tencent       <- a real folder, 4.2 GB
after    C:\Users\me\AppData\Roaming\Tencent       <- a link, 0 bytes
                              └──> D:\Apps\Tencent  <- the real data lives here
```

When a program opens `C:\Users\me\AppData\Roaming\Tencent`, Windows silently redirects it
to `D:\Apps\Tencent`. **The program never notices, no settings are lost, and the space on
C: is actually reclaimed.**

That is what the "伪" (pseudo) in the name means: nothing is really moved — the system is
just made to believe it is still there.

## Features

- **Single-file exe**, ~2.4 MB, no .NET runtime required, copy it anywhere and run
- **Five-step commit**: copy → verify → swap → link → probe; any failure restores the original state
- **Rebuilds junctions as junctions** instead of copying the linked content twice
- **Backup + pending list**: the original data is kept after migration, so rolling back takes milliseconds
- Handles the details that actually bite: read-only files, sparse files, long paths, locked files

## How it works

```
1. copy      copy into <target>.__incoming__ instead of the final name
2. verify    compare size + mtime for every file; abort on any mismatch
3. swap      rename incoming to the final name, rename the source to xxx.__bak_<timestamp>
4. link      create a junction at the original path pointing to the target
5. probe     write a file through the C: path, confirm it shows up on the target, then delete it
```

- If step 1 or 2 fails: the temporary copy is deleted, **the source is untouched byte for byte**
- If step 4 or 5 fails: the link is removed, `.bak` is renamed back — automatic rollback
- On success: the original data stays as `.__bak_*` and appears in the pending-release list

Rolling back **does not copy anything again** — it deletes the link and renames a folder back.

## Usage

1. Download `伪迁移助手.exe` from [Releases](../../releases)
2. Run it (SmartScreen may block it once — see FAQ)
3. **Source folder**: the folder you want to move
4. **Destination folder**: where to put it. With "auto-create same-name folder" checked,
   `D:\Apps` becomes `D:\Apps\Tencent`
5. Click **Start migration**
6. Use the program normally for a few days. When you are confident, select the row in the
   pending list and click **Release** — that deletes the backup and actually frees the space

If something looks wrong: **Roll back** restores everything.

## Build from source

Requires the **.NET 8 SDK** and **Visual Studio 2022 Build Tools** (NativeAOT needs the C++ linker).

```powershell
.\build.ps1
```

Output: `src\c2d\out\伪迁移助手.exe`, a single file.

## Known limitations

| Limitation | Details |
|---|---|
| Target must be a **fixed disk** | Unplug a USB drive or change a drive letter and junctions break |
| Cannot rename a folder in use | The tool walks down and tells you which item is locked |
| Target app may not support long paths | If the new path gets longer, some programs may fail to open it (warned only when it grows) |
| Do not rename `.__bak_*` manually | The pending list will lose track of it |
| Only user-readable folders | Folders requiring SYSTEM privileges fail the pre-check |

## FAQ

**"Cannot rename" during migration?**
A file inside is held open by another program. Windows lets you read such files but refuses
to rename the folder. Close the program shown in the message (for WeChat/QQ, quit from the
tray icon too), or reboot and migrate before starting anything else.

**Why do I have to click "Release" after migrating?**
The backup is your last safety net. Delete it once you are sure the program works —
after that, rolling back is no longer possible.

## License

[MIT](LICENSE)
