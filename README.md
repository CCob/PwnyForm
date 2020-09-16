# PwnMstGenerator

## Overview

PwnMstGenerator will take an MSI installer as input and generate an MSI transform (mst) that can be used to inject arbitrary command execution by adding a custom action that will execute during the UI or Install sequence of an MSI file.

The generated MST produces a JScript custom action that will by default launch cmd.exe, the executed command can be overriden using the CMD MSI property

## Why

Generating an MST can be used as a method for adding custom behavior to signed MSI files without modifying the MSI itself, under the radar persistence or possibly breakout of restricted desktops environments if msiexec is allowed to execute.

## Usage

```cmd
PwnMstGenerator by @_EthicalChaos_
  Generates MST transform to inject arbitrary commands/cutom actions when installing MSI files

  -m, --msi=VALUE            MSI file to base transform on (required)
  -t, --mst=VALUE            MST to generate that includes new custom action (
                               required)
  -s, --sequence=VALUE       Which sequence table should inject the custom
                               action into (UI (default) | Execute)
  -o, --order=VALUE          Which sequence number to use (defaults 1)
  -h, --help                 Display this help
```

Example usage

```cmd
PwnMstGenerator -m Setup.msi -t Pwnd.mst
msiexec -i Setup.msi CMD=cmd.exe TRANSFORM=Pwnd.mst
```
