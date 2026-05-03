# Logicwright

Logicwright is an open-source AI engineering workbench for industrial automation engineers.

The project helps engineers generate PLC programs, HMI configuration assets, and electrical engineering deliverables for tools such as TIA Portal, EPLAN, and SOLIDWORKS Electrical in a structured, verifiable, and auditable way.

## Current Status

The repository is in the project initialization phase. The first functional scope, engineering baseline, and standard input package have been drafted.

Core documents:

- `docs/01-functional-definition.md`
- `docs/02-tia-plc-development-baseline.md`
- `docs/03-plc-library-hmi-asset-strategy.md`
- `docs/04-logicwright-input-package-template.md`

Standard input package template:

- `templates/input-package/`

## v0.1 Target

The first release focuses on:

- TIA Portal V21 integration
- PLC standard module generation
- WinCC Unified template-driven HMI generation
- Compile validation and audit loop

## Suggested Repository Structure

- `docs/`
- `specs/`
- `connectors/`
- `orchestrator/`
- `generators/`
- `validators/`
- `templates/`
- `examples/`

## Recommended Next Steps

1. Define the unified `spec` structure.
2. Design the TIA Connector MVP.
3. Implement the first sample module, such as a cylinder control module.
4. Create the first naming, library, and template rules.
