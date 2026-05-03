# PLC Library and HMI Asset Strategy

This document defines how Logicwright should plan PLC block libraries, HMI faceplates, alarms, archives, and Siemens library usage.

## 1. Strategy Summary

Logicwright should not invent a separate engineering world. It should build on Siemens’ own concepts and package them into a governed layer:

- Siemens reference libraries and engineering guidelines remain the technical baseline.
- Logicwright provides a project-specific standard layer on top.
- Project-owned types, templates, and rules are versioned and auditable.
- Generated assets must be reusable, importable, and compatible with TIA Portal workflows.

## 2. PLC Library Strategy

### 2.1 Library Layers

Use three layers:

1. Siemens reference layer
2. Logicwright standard layer
3. Project-specific layer

#### Siemens reference layer

Use Siemens-delivered or Siemens-recommended libraries where they already solve a common engineering problem:

- Programming Guideline for S7-1200/1500 as the style and block-usage baseline
- Automation Framework libraries as an architecture reference
- LBP as a process-block reference
- CPG / PackML libraries as a machine-state and PackTags reference
- PLCopen-aligned motion or safety libraries where applicable

Do not edit Siemens-delivered types directly.

#### Logicwright standard layer

Logicwright should define its own reusable standard blocks for the first release:

- `CM_Cylinder`
- `CM_Motor`
- `CM_Valve`
- `CM_AnalogLoop`
- `CM_SingleStation`

These types are not replacements for Siemens libraries. They are project-owned wrappers and patterns that enforce naming, interfaces, diagnostics, and HMI coupling rules.

#### Project-specific layer

Project-specific blocks handle machine behavior that is unique to a customer or line.

Rules:

- May call Logicwright standard blocks.
- May extend standard interfaces.
- Must not break the standard interface contract unless explicitly versioned.

### 2.2 Delivery Format

For PLC assets, Logicwright should generate:

- UDTs / PLC data types
- FBs
- FCs
- DBs
- optional OB wrappers
- library export packages
- source-code artifacts for review and versioning

### 2.3 Versioning Rule

Each standard block must carry:

- library name
- type name
- semantic version
- source reference
- compatibility notes

Recommended version format: `major.minor.patch`.

### 2.4 Siemens Library Usage Policy

Use Siemens libraries when:

- the logic is a well-defined standard utility
- the library is already validated for the intended function
- the integration cost is lower than reimplementation

Examples:

- standard motion utilities
- diagnostics frameworks
- PackML or process template logic
- safety-related certified blocks

Avoid using Siemens libraries as opaque black boxes when you need:

- strict project naming control
- custom HMI binding
- custom audit logging
- custom data contracts for Logicwright

In that case, wrap the Siemens library with Logicwright-owned types and interfaces.

## 3. HMI Faceplate Strategy

### 3.1 Faceplate Ownership

Faceplates should follow the same three-layer model:

1. Siemens reference screen patterns
2. Logicwright standard faceplates
3. Project-specific faceplates

Logicwright should generate standard faceplates for reusable module types such as cylinder, motor, and valve.

### 3.2 Faceplate Contract

Each faceplate must expose:

- command interface
- parameter interface
- state/feedback interface
- alarm interface
- permission or role information when required

The faceplate must bind to a stable UDT or tag structure. It must not bind to internal temporary variables or ad hoc block internals.

### 3.3 Faceplate Reuse Policy

Use faceplates when:

- the module repeats across the project
- the screen behavior is identical or nearly identical
- the HMI control set is standardized

Do not use faceplates just to mimic design styling. Siemens HMI guidance already recommends using proper screen objects and styles where appropriate.

## 4. Alarm Strategy

### 4.1 Alarm Ownership

Alarm handling should be split into:

- module alarms
- EM or Unit aggregated alarms
- HMI alarm classes and texts
- archive or history storage

### 4.2 Alarm Generation Policy

Logicwright should generate:

- alarm IDs
- alarm codes
- alarm classes
- trigger conditions
- reset conditions
- multilingual alarm texts
- optional first-fault tags

Alarm logic must remain in PLC-owned data structures. The HMI only visualizes and filters it.

### 4.3 Siemens Alignment

Use Siemens and Siemens-adjacent concepts where already established:

- Standard alarm classes and diagnostic views in WinCC Unified
- PackML/CPG-style event aggregation where relevant
- ProDiag where the project wants harmonized process diagnostics

## 5. Archive Strategy

### 5.1 Archive Scope

Logicwright should treat archives as first-class engineering outputs:

- input package snapshot
- generated spec
- generated design
- generated PLC/HMI artifacts
- compile log
- import log
- repair log
- final acceptance record

### 5.2 HMI and Alarm Archives

For HMI projects, preserve:

- alarm text list version
- text list version
- faceplate version
- screen template version
- tag interface version

For PLC projects, preserve:

- UDT version
- FB version
- DB version
- library version
- rule version

### 5.3 Audit Model

Each run should produce a traceable package:

```text
run-id/
  input/
  spec/
  design/
  artifacts/
  import/
  compile/
  repair/
  acceptance/
```

## 6. Recommended Implementation Order

1. Define project-owned PLC standard library types.
2. Define one faceplate per standard module.
3. Define alarm and text list generation.
4. Define archive naming and versioning rules.
5. Add Siemens reference imports only where they give direct value.

## 7. Practical Decision

For v0.1, the recommended plan is:

- Use Siemens guidelines and reference libraries as the technical basis.
- Build Logicwright-owned standard FBs, UDTs, faceplates, and alarm schemas on top.
- Do not depend on Siemens library internals as the primary contract.
- Prefer wrapping and versioning over direct modification.

This keeps the system portable, auditable, and easier to generate automatically.
