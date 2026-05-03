# TIA / PLC Development Baseline

This document defines the engineering baseline that Logicwright must enforce when generating TIA Portal PLC projects. The baseline is derived from the Siemens application guides, programming guidelines, standardization guidelines, Automation Framework, LBP, CPG Template, WinCC Unified, and HMI Template Suite documents in `referenceDoc/`.

> Scope: The baseline targets TIA Portal V21 and S7-1200 / S7-1500 standard module generation. The first release focuses on S7-1500 and discrete control modules such as cylinders, motors, and valves. Safety programs, real-device download, and advanced motion-control loops are not included in first-release automatic generation.

## 1. Standards and Source Documents

### 1.1 International and Industry Standards

| Standard | Baseline usage |
| --- | --- |
| IEC 61131-3 | Base standard for PLC languages, POUs, data types, and FB/FC modeling. In TIA Portal, SCL corresponds to ST, LAD to LD, and FBD to FBD. |
| IEC 81346-1 | Basis for functional, product, and location structuring and reference designation. Project structuring must start early. |
| IEC 61512-1 / ISA-88 | Reference model for modular equipment decomposition. Use Unit / Equipment Module / Control Module layering for structured projects. |
| OMAC PackML | Use when the machine requires unified modes, state management, and PackTags interfaces. It is optional for non-PackML projects. |
| PLCopen | Reference for motion, safety, XML exchange, and structured library usage. |

### 1.2 Siemens Reference Documents

| Document | Baseline role |
| --- | --- |
| `81318674_Programming_guideline_DOC_v16_en.pdf` | Core PLC engineering baseline: optimized blocks, symbolic addressing, block interfaces, libraries, reusable blocks, SCL style, and hardware-independent programming. |
| `109756737_Standardization_Guideline_DOC_V10_en (1).pdf` | Standardization workflow: requirements, analysis, design, implementation, testing, library use, and TIA Openness. |
| `109817223_AutomationFramework_DOC_V2_2_2_en.pdf` | Reference architecture: ISA-88 layering, Software Units, library versioning, HMI interfaces, diagnostics, alarms, and simulation. |
| `CPG_Template_SIMATIC_V1_0_en.pdf` | PackML / ISA-88 / Make2Pack reference, especially Unit, EM, CM, PackTags, and alarm/state interfaces. |
| `109749508_LBP_V2.8_Implementation_DOC_en.pdf` | Basic process library reference: standard block interfaces, `settingsPLC` / `settingsHMI` / `statusHMI` separation, and HMI communication patterns. |
| `109827603_WinCC_Unified_engineering_guideline_DOC_V4_en_withNavigation.pdf` | WinCC Unified engineering boundaries, tag dynamization, faceplates, performance, and screen structure. |
| `81318674_HMI_Styleguide_DOC_v10_en.pdf`, `91174767_HMITemplateSuite*.pdf` | HMI visual and template references. They constrain PLC-HMI interfaces but do not define PLC logic directly. |

`STEP7_WinCC_Engineering_V19_zhCN.pdf` could not be auto-extracted in this pass. The file header is valid, but `pdf-parse` reported `Invalid Root reference` and `pdf2json` timed out. This document must be re-checked later with Acrobat, TIA Portal help, or Siemens official search.

## 2. Project Baseline Goals

Logicwright-generated TIA projects must satisfy the following goals:

1. Structured: define equipment structure, functional modules, I/O, HMI, alarms, interlocks, and data interfaces before generating code.
2. Reusable: standard modules must communicate through block interfaces and PLC data types, not hidden global dependencies.
3. Verifiable: generated results must support rule validation, TIA compilation, audit logs, and diff review.
4. Maintainable: project folders, naming, comments, versions, library types, and instance relationships must remain clear and stable.
5. Extensible: the structure must support future integration with Automation Framework, PackML, HMI Template Suite, TIA Openness, and testing automation.

## 3. Project Structure Baseline

### 3.1 Engineering Layers

Default logical layering:

| Level | Source | Description |
| --- | --- | --- |
| Plant / Line | IEC 81346 / ISA-88 | System-level boundary. Optional in first release. |
| Unit | ISA-88 / Automation Framework / CPG | Independent operating unit with alarms, diagnostics, and HMI control. |
| Equipment Module (EM) | ISA-88 / AF / CPG | A functional machine section, such as loading, clamping, or conveying. |
| Control Module (CM) | ISA-88 / AF / CPG | The smallest reusable control object, such as a cylinder, motor, valve, or sensor group. |

The first cylinder sample can be modeled directly as a `CM`. If a station contains multiple cylinders, motors, and sensors, it should be modeled as an `EM` that aggregates multiple `CM`s.

### 3.2 TIA Portal Project Folder Structure

Recommended PLC folder structure:

```text
PLC
  Program blocks
    00_Main
    10_Units
    20_EquipmentModules
    30_ControlModules
    40_Shared
    50_Diagnostics
    60_HMI_Interface
    90_TestSimulation
  PLC data types
    Base
    Interfaces
    Modules
    HMI
    Diagnostics
  PLC tag tables
    IO
    Constants
    HMI
    Diagnostics
  External source files
```

The generator must preserve user extension areas and never overwrite user code. Project templates, library types, standard UDTs, and standard FBs must be updated through versioned releases.

### 3.3 Software Units

If the target CPU and TIA version support Software Units, use them as follows:

- `Global`: shared data types, shared functions, diagnostics, and HMI interfaces.
- `Unit_<Name>`: unit-level OB/FB/DB/HMI interface content.
- `EM_<Name>`: reusable equipment module scope.
- `Safety`: safety-only scope; first release does not auto-generate safety logic.

Data exchange between Software Units must be explicit. Do not rely on implicit global access.

## 4. Programming Language Baseline

### 4.1 Default Language Choice

| Scenario | Recommended language |
| --- | --- |
| State machines, sequences, complex conditions, data processing | SCL |
| Simple interlocks, Boolean logic, maintenance-friendly logic | LAD or FBD |
| Standard libraries, algorithms, arrays, and structured data handling | SCL |
| HMI generation control comments or visualization helper networks | LAD/FBD or SCL, depending on TIA/SiVArc support |

Do not use STL as the default language for new projects. In S7-1500, STL is mainly for legacy compatibility and is not a first-release output target.

### 4.2 SCL Coding Rules

SCL code must follow:

- Use `REGION` to organize code blocks.
- Use enums or named constants for state machines, not magic numbers.
- Use `CASE` for discrete states.
- Do not modify the loop variable inside a `FOR` loop.
- Use `FOR` when the loop variable is clearly defined; use `WHILE` or `REPEAT` when the loop condition must change dynamically.
- Prefer direct Boolean assignment, for example `xReady := xA AND xB;`.
- Prefer whole-array assignment or `MOVE_BLK` over element-by-element copy loops unless element-level processing is required.
- Avoid expensive Variant instructions inside loops.

## 5. Blocks and Data Baseline

### 5.1 Optimized Blocks

All newly created FB, FC, and DB blocks default to optimized block access. Exceptions must be declared in `spec`, for example:

- Compatibility with legacy S7-300/400, non-optimized blocks, or third-party absolute-address interfaces.
- Communication protocol that requires fixed address layout.
- Import of an existing non-optimized library.

Do not mix optimized and non-optimized access in the same module. Avoid frequent copying of structured data between optimized and non-optimized blocks.

### 5.2 Symbolic Addressing

Default to symbolic addressing only. Do not generate:

- Direct absolute addressing such as `DB10.DBX0.0`
- ANY pointer as a standard interface
- Module-internal dependence on M, T, or C areas

Use `ARRAY` for indexed access, `VARIANT` for generic interfaces, and PLC data types for structured I/O mapping.

### 5.3 POU Usage Rules

| Type | Rule |
| --- | --- |
| OB | Scheduling, startup initialization, and error/diagnostic entry only. Do not place reusable business logic here. |
| FB | Default implementation for stateful, instantiable, reusable modules. |
| FC | Stateless calculations, conversions, checks, and small reusable functions. |
| Global DB | Shared cross-module data, HMI interfaces, diagnostics, parameters, recipes, and log buffers. |
| Instance DB | Modified only by its owning FB. Do not write directly from outside. |
| UDT / PLC data type | Structure definition for module interfaces, HMI interfaces, I/O mapping, diagnostics, alarms, and parameters. |

### 5.4 Multi-Instances

When a standard module calls IEC timers, IEC counters, edge detection, or child FBs, prefer multi-instance. Create standalone instance DBs only when independent online monitoring, external binding, or library compatibility requires it.

### 5.5 Parameter Passing

All inter-block data exchange must happen through `Input`, `Output`, or `InOut` interfaces.

- Structured data such as `ARRAY`, `STRUCT`, `STRING`, and UDTs should default to `InOut` to avoid unnecessary copying.
- Reusable blocks must not directly access project-specific global DBs, PLC tags, global constants, or single-instance DBs.
- Global DBs are reserved for shared data, HMI/diagnostic interfaces, configuration, and task-level data, not for bypassing block interfaces.

### 5.6 Retentivity

All data defaults to non-retain. Only the following may be marked retain:

- Recipes, calibration values, counters, and run parameters that must survive power loss.
- HMI writable settings explicitly confirmed by requirements.

Retentivity must be defined in the FB interface or static variables, not by using “Set in IDB” as the default strategy. Retentive data must be recorded centrally in `spec.retain`.

### 5.7 Download Without Reinitialization

Optimized FBs and DBs expected to expand during commissioning must reserve memory so online downloads do not destroy existing actual values. Interface extensions must append fields only and must not reorder existing fields.

## 6. Naming Baseline

### 6.1 General Rules

Naming must satisfy:

- Use English ASCII identifiers only. Avoid spaces, Chinese characters, and special symbols.
- Do not use characters unsupported by WinCC / SiVArc: `% @ ? " / \ < > . :`
- Names must express function and layer, not temporary implementation details.
- Use a uniform prefix, casing, and abbreviation table per object class.
- All abbreviations must be registered; do not invent them ad hoc.

Use PascalCase for TIA objects and semantic prefixes for variables. Automation Framework allows consecutive uppercase abbreviations in library names such as `LAF` and `LBC`.

### 6.2 Object Naming

| Object | Pattern | Example |
| --- | --- | --- |
| Unit FB | `Unit_<Name>` | `Unit_Loading` |
| EM FB | `EM_<Name>` | `EM_ClampStation` |
| CM FB | `CM_<DeviceType>` | `CM_Cylinder` |
| Instance DB | `inst<Name>` or `<Name>_DB` | `instCylClamp` |
| UDT | `type<Name>` | `typeCylinderIf` |
| Global DB | `db<Name>` | `dbHmiCylinder` |
| Constants | `C_<NAME>` | `C_STATE_HOME` |
| PLC tag table | `<Area>Tags` | `IoTags` |
| HMI tag table | `Hmi<Name>Tags` | `HmiCylinderTags` |

The project may use a different naming format if that format is declared in the project rule library and enforced by validation.

### 6.3 Variable Semantic Prefixes

| Prefix | Meaning | Example |
| --- | --- | --- |
| `x` | BOOL | `xEnable` |
| `i` | INT / DINT | `iStep` |
| `r` | REAL / LREAL | `rSpeedSetpoint` |
| `t` | TIME | `tExtendTimeout` |
| `dtl` | DTL | `dtlLastFault` |
| `cmd` | Command | `cmdExtend` |
| `sts` | Status | `stsExtended` |
| `ind` | Feedback / indication | `indExtended` |
| `err` | Error | `errTimeout` |
| `wrn` | Warning | `wrnLowPressure` |
| `par` | Parameter | `parTimeout` |
| `cfg` | Configuration | `cfgLimits` |
| `hmi` | HMI interaction | `hmiCommand` |
| `stat` | FB internal static state | `statStep` |
| `temp` | Temporary variable | `tempElapsed` |

The LBP documents’ `cmd`, `ind`, `settingsPLC`, `settingsHMI`, and `statusHMI` split should be treated as the HMI interface naming reference.

## 7. Standard Module Interface Baseline

Every standard control module must define at least the following interface data:

```text
type<Module>Cmd      HMI / upstream commands
type<Module>Par      Configurable parameters
type<Module>Sts      Current state and feedback
type<Module>Alm      Alarm, warning, and diagnostic bits
type<Module>If       Aggregated interface as needed
```

Recommended interface structure:

```text
Input
  xEnable
  xReset
  hwIo / process feedback

InOut
  ioCmd     : type<Module>Cmd
  ioPar     : type<Module>Par
  ioSts     : type<Module>Sts
  ioAlm     : type<Module>Alm

Output
  xReady
  xBusy
  xDone
  xError
  hwOutput / actuator command
```

HMI-writable data must be separated from PLC input and computed data:

- `settingsPLC`: PLC-side parameters or upstream logic values
- `settingsHMI`: HMI-writable data requiring access control
- `statusHMI`: read-only values shown on the HMI

The same variable must not be written both by automatic PLC logic and by direct HMI interaction. If HMI override is required, an explicit override bit, permission model, interlock, and audit trail are required.

## 8. State Machine Baseline

### 8.1 Control Module States

The first-release non-PackML standard module uses a lightweight state machine:

| State | Meaning |
| --- | --- |
| `Disabled` | Not enabled; outputs are in safe default state. |
| `Idle` | Enabled and waiting for a command. |
| `Starting` | Pre-action check or startup phase. |
| `Moving` / `Running` | Action in progress. |
| `Completed` | Action completed; can return to Idle or wait for the next command. |
| `Stopping` | Stop handling. |
| `Faulted` | Latched fault, waiting for reset. |

All transitions must be explicitly triggered and must include timeout, interlock, and reset handling.

### 8.2 PackML Extension

If a project enables PackML, the Unit layer must use the PackML mode/state machine and PackTags interface, with Command, Status, and Admin tags separated. EM and CM layers report status, alarms, and availability to the Unit layer and do not implement a conflicting global state machine.

## 9. I/O and Hardware Independence

1. Module logic must not depend directly on hardware absolute addresses.
2. Map I/O points to symbolic PLC tags or UDTs first, then pass them into the module interface.
3. Prefer PLC data types for drive telegrams, remote I/O, and analog channels.
4. Do not use M areas as module internal storage; generate cycle bits in software, not via CPU clock memory.
5. Use IEC timers and IEC counters, preferably as multi-instances.

## 10. Alarm, Diagnostics, and Event Baseline

### 10.1 Alarm Classification

Every module must at least distinguish:

| Type | Meaning |
| --- | --- |
| Alarm | Affects production or safety and requires stopping or fault handling. |
| Warning | Does not stop immediately but requires operator attention. |
| Status | Runtime state, step, hint, or diagnostic message. |

CPG Template and Automation Framework both use a CM/EM-to-Unit event aggregation model. Logicwright-generated modules should emit module-level events, then let the upper level aggregate them into HMI or Unit diagnostics.

### 10.2 Diagnostic Fields

Standard module output must include:

- `xError`: fault total bit
- `iErrorId` or `dwErrorId`: fault code
- `xWarning`: warning total bit
- `iWarningId` or a bit array
- `dtlFirstFault` or first-fault record field when the project enables it
- optional `statusTextId`: HMI text list index

### 10.3 ProDiag / System Diagnostics

If ProDiag is enabled, use unified alarm classes, unified PLC/HMI alarm configuration, and standard diagnostic screens. The first release does not require ProDiag generation, but the generated assets must leave room for later integration.

## 11. HMI Interface Baseline

### 11.1 PLC-HMI Boundary

The PLC generator only creates HMI-related data objects. It does not freely generate complex HMI layouts. HMI generation must be driven by templates, faceplates, or HMI Template Suite assets.

The HMI interface must:

- Use stable UDTs.
- Separate commands, parameters, states, and alarms.
- Support ID-based binding for text lists, graphic lists, and alarm texts.
- Avoid exposing FB internal temporary variables.
- Use permissions, interlocks, edges, or handshake mechanisms for HMI commands.

### 11.2 WinCC Unified Performance Constraints

From the WinCC Unified engineering guidelines:

- Prefer tag dynamization over scripts when the logic is simple.
- Avoid stacking multiple objects solely to simulate visibility changes.
- Use faceplates only for genuine reuse, not as decorative wrappers.
- Respect screen object and HMI tag system limits.
- Avoid unsupported characters in PLC tag names so SiVArc does not rename them and create inconsistencies.

## 12. Library and Version Baseline

1. Standard modules, UDTs, HMI faceplates, and templates should be delivered as TIA library types rather than master copies whenever possible.
2. Library types must be versioned, and the default version must be the current recommended version.
3. When modifying Siemens/AF/LBP-delivered library types, do not edit the original type directly. Duplicate it into a project-owned type and rename it.
4. Every Logicwright generation task must record the library name, type name, version, and default version status used.
5. When releasing a new version, update the project instances and remove non-default legacy versions to prevent accidental misuse.

## 13. Comments, Documentation, and Multilanguage Baseline

1. FB, FC, UDT, and DB objects must have title, purpose, interface description, version, and author/generator information.
2. Key networks must include network titles and required comments.
3. Watch tables and debug variables should be commented.
4. HMI texts, alarm texts, and enum display texts must support multiple languages. If multilingual mode is enabled, every active language must have text.
5. TIA Portal user documentation folders or Code2Docu-generated block documentation may be used, and the documentation must be delivered with the project or library.

## 14. Safety and Permission Baseline

The first release does not auto-generate Safety programs. When safety is involved:

- Keep safety and standard program areas clearly separated.
- Exchange data between standard and safety programs only through controlled standard DBs, not through M areas.
- Protect the safety program with a password and require human confirmation.
- Do not generate automatic real-device download functionality.
- Do not generate HMI commands that bypass safety circuits.

HMI writable parameters, manual actions, reset actions, and mode changes must be protected by permissions and interlocks.

## 15. Generation and Validation Baseline

### 15.1 Generation Flow

Logicwright’s TIA generation flow must be:

1. Input structured requirements.
2. Parse project structure, naming rules, template versions, and existing objects.
3. Generate `spec`.
4. Validate `spec`.
5. Generate `design`.
6. Validate interfaces, naming, layering, and HMI boundaries.
7. Generate TIA artifacts.
8. Import into TIA Portal.
9. Compile and read diagnostics.
10. Run limited repair iterations.
11. Output diffs, logs, and human confirmation records.

### 15.2 Mandatory Validation Checks

| Check | Rule |
| --- | --- |
| Naming | Object, variable, and HMI tag names must not contain illegal characters and must follow project naming rules. |
| Optimized blocks | New blocks must default to optimized access unless an exception is declared. |
| Interfaces | Reusable blocks must not hide dependencies on project-specific global objects. |
| Data types | Structured interfaces must use UDTs / PLC data types. |
| Addressing | Module logic must not directly use absolute addressing. |
| M/T/C areas | Must not be used as module state or default timer/counter storage. |
| Retentivity | Retain data must be declared and minimized. |
| HMI writes | HMI-writable fields must be separated from PLC state and protected by permissions/interlocks/override strategy. |
| Alarms | Every module must have a fault total bit and fault code. |
| Multilanguage | HMI and alarm texts must not be missing when multilingual mode is enabled. |
| Library versions | Standard types must record version and source. |

### 15.3 Test Baseline

The first release must at least provide:

- Static rule validation
- SCL/XML artifact structure validation
- TIA compilation validation
- Module-level simulation test cases
- HMI tag interface consistency testing

Later integration can include TIA Portal Test Suite, S7 Unit Test, S7-PLCSIM, S7-PLCSIM Advanced, and SIMIT.

## 16. First Sample: Cylinder Module Baseline

The cylinder module, `CM_Cylinder`, must include:

- Inputs: enable, reset, extend command, retract command, extended feedback, retracted feedback, air-pressure or interlock availability, and emergency-stop or safety status as read-only inputs.
- Outputs: extend valve, retract valve, ready, busy, done, fault, current state, and fault code.
- Parameters: extend timeout, retract timeout, motion exclusivity strategy, single- or double-solenoid mode, and HMI manual permission.
- States: Disabled, Idle, Extending, Extended, Retracting, Retracted, Faulted.
- Alarms: extend timeout, retract timeout, contradictory position feedback, no position feedback, interlock lost, and command conflict.
- HMI interface: separate command, parameter, state, and alarm areas. HMI commands must use edge/handshake logic and must not directly write internal state.

## 17. Default Decisions

| Topic | Decision |
| --- | --- |
| Target PLC | Prefer S7-1500; explicitly declare compatibility with S7-1200 when needed. |
| TIA version | Target V21; apply V19/V20/V21 reference rules as general principles. |
| Block access | Default to optimized. |
| Addressing | Default to symbolic only. |
| Module layering | Default to CM; use EM/CM for more complex objects and Unit/EM/CM for whole machines. |
| State model | Lightweight first-release state machine; PackML is optional. |
| HMI | Template/faceplate driven, not free-form layout generation. |
| Safety | Do not auto-generate; only provide controlled interface recommendations. |
| Libraries | Logicwright standard modules must be versioned. |

## 18. Next Steps

1. Re-check `STEP7_WinCC_Engineering_V19_zhCN.pdf` with a dedicated PDF tool.
2. Create machine-readable rules: `rules/tia-plc-baseline.yaml`.
3. Create a `spec` JSON Schema for naming, interfaces, HMI, alarms, retain, and state machine structure.
4. Build `CM_Cylinder` UDTs, FBs, HMI interfaces, and test samples.
5. Build the TIA Openness import/compile MVP error-code mapping.
