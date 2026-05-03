# Logicwright Standard Input Package

This folder contains a copyable standard input package. Engineers can duplicate the folder into a real project and then edit the YAML and CSV files.

File summary:

- `01-project.yaml`: project identity, TIA version, target PLC/HMI, and generation scope.
- `02-rules.yaml`: naming, forbidden items, default generation strategy, and validation rules.
- `03-equipment-list.csv`: Unit, EM, and CM hierarchy and module list.
- `04-io-list.csv`: I/O point list.
- `05-module-cylinders.csv`: cylinder module instance table.
- `06-sequences.csv`: sequence step table.
- `07-interlocks.csv`: interlock condition table.
- `08-alarms.csv`: alarm and diagnostic table.
- `09-hmi-requirements.csv`: HMI template and variable requirements.
- `10-acceptance-tests.csv`: acceptance test cases.

Filling rules are described in:

- `docs/04-logicwright-input-package-template.md`
- `docs/02-tia-plc-development-baseline.md`

Note: the sample values in this folder are for illustration only and must not be used directly for real equipment.
