# Logicwright Engineer Workflow Guide

This guide describes the end-to-end workflow that an engineer follows when delivering a TIA project with Logicwright.

## 1. Prepare the Input Package

Start with the numbered input package in English.

Required files:

- `01-project.yaml`
- `02-rules.yaml`
- `03-equipment-list.csv`
- `04-io-list.csv`
- `05-module-cylinders.csv`
- `06-sequences.csv`
- `07-interlocks.csv`
- `08-alarms.csv`
- `09-hmi-requirements.csv`
- `10-acceptance-tests.csv`

Engineer action:

1. Fill the tables in Excel or another spreadsheet editor.
2. Keep identifiers in English ASCII only.
3. Confirm the target TIA version, PLC family, and HMI platform.
4. Review naming, alarm text, faceplate intent, and permissions before submission.
5. Export the CSV files as UTF-8.

Output:

- A complete input package ready for validation.

## 2. Validate the Input Package

Engineer action:

1. Open the package in the agreed editing tool.
2. Check that every equipment row has a parent, if required.
3. Check that every I/O row maps to a real signal.
4. Check that every alarm and test case has a unique ID.
5. Confirm that HMI write access is explicitly controlled.

Output:

- A clean package that can be consumed by Logicwright.

## 3. Start TIA Portal

Engineer action:

1. Launch TIA Portal V21.
2. Open the target project.
3. Keep the project open while the connector runs.

If TIA Portal is already running, Logicwright can attach to it.

Expected result:

- TIA Portal is open.
- The target project is loaded.
- The Windows user belongs to the `Siemens TIA Openness` group.

## 4. Export the Project Context

Use the connector to capture the live project structure.

Example:

```powershell
& 'D:\app\Logicwright\connectors\tia\src\Logicwright.TiaConnector\bin\Release\Logicwright.TiaConnector.exe' context --attach --output artifacts\context\project-context.json
```

Engineer action:

1. Run the context export after the project is open.
2. Confirm the TIA Openness authorization dialog if it appears.
3. Check the generated JSON for project name, devices, PLC software, tag tables, and HMI target structure.
4. Save the export together with the input package snapshot.

If `msbuild` is not available in the normal PowerShell path, use the Build Tools path:

```powershell
& 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe' Logicwright.sln /p:Configuration=Release
```

Output:

- A machine-readable project context file.

## 5. Generate Logicwright Artifacts

Logicwright uses the input package and exported context to create:

- spec
- design
- PLC types and blocks
- HMI faceplate contracts
- alarm and archive definitions

Engineer action:

1. Review the generated spec.
2. Confirm block names, library versions, and HMI bindings.
3. Approve the first generation pass or request a revision.
4. Confirm whether the project uses Logicwright standard faceplates, Siemens library faceplates, or project-specific faceplates.

Output:

- Generated artifacts ready for TIA import.

## 6. Import Into TIA Portal

Engineer action:

1. Import the generated PLC source or library content.
2. Import HMI objects, tags, faceplates, and alarm definitions if included.
3. Verify that the import lands in the expected folders.

Output:

- Project content updated inside TIA Portal.

## 7. Compile and Review

Engineer action:

1. Run a TIA compile.
2. Review compile messages.
3. Fix naming, missing references, or type mismatches.
4. Re-run compile until the project is consistent.

Output:

- A compiled project with a traceable compile log.

## 8. Review the Result

Engineer action:

1. Inspect generated blocks and UDTs.
2. Check HMI bindings, faceplates, alarms, and archive setup.
3. Confirm the cylinder or module behavior matches the input package.
4. Mark any exceptions that need manual engineering follow-up.

Output:

- A reviewed engineering result.

## 9. Archive the Delivery

Engineer action:

1. Archive the input package.
2. Archive the exported context.
3. Archive the generated artifacts.
4. Archive compile logs and review notes.

Output:

- A complete delivery package for traceability.
