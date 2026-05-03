# Logicwright Standard Input Package

This folder contains a copyable standard input package. Engineers can duplicate the folder into a real project and then edit the YAML and CSV files.

File summary:

- `project.yaml`: project identity, TIA version, target PLC/HMI, and generation scope.
- `rules.yaml`: naming, forbidden items, default generation strategy, and validation rules.
- `equipment-list.csv`: Unit, EM, and CM hierarchy and module list.
- `io-list.csv`: I/O point list.
- `module-cylinders.csv`: cylinder module instance table.
- `sequences.csv`: sequence step table.
- `interlocks.csv`: interlock condition table.
- `alarms.csv`: alarm and diagnostic table.
- `hmi-requirements.csv`: HMI template and variable requirements.
- `acceptance-tests.csv`: acceptance test cases.

Filling rules are described in:

- `docs/04-logicwright-input-package-template.md`
- `docs/02-tia-plc-development-baseline.md`

Note: the sample values in this folder are for illustration only and must not be used directly for real equipment.
