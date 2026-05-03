# TIA Connector

The TIA Connector is the Logicwright integration layer for Siemens TIA Portal through TIA Openness.

Current scope:

- Build a minimal .NET Framework 4.8 console tool.
- Load the TIA Portal V21 Openness API.
- Verify that the current Windows user can execute Openness applications.
- Connect to an existing TIA Portal process or start a new one.
- Print basic project and device information.

Prerequisites:

- TIA Portal V21
- TIA Openness PublicAPI V21
- Windows user in the `Siemens TIA Openness` group
- Visual Studio 2022 Build Tools
- .NET Framework 4.8 SDK and targeting pack

Build:

```powershell
msbuild Logicwright.sln /p:Configuration=Release
```

Run:

```powershell
.\connectors\tia\src\Logicwright.TiaConnector\bin\Release\Logicwright.TiaConnector.exe probe --attach
```

If TIA Portal is not running, use:

```powershell
.\connectors\tia\src\Logicwright.TiaConnector\bin\Release\Logicwright.TiaConnector.exe probe --start
```

The connector does not download to real equipment.
