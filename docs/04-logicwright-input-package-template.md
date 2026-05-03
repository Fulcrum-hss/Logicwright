# Logicwright Standard Input Package

This document defines the standard input package that engineers deliver to Logicwright. The goal is not to replace engineering design documents, but to organize the PLC/HMI generation inputs into a structured, parseable, and auditable package.

## 1. Input Package Principles

1. Engineers provide requirements, I/O, actions, interlocks, alarms, HMI needs, and project rules.
2. Logicwright converts the input package into `spec -> design -> artifacts` and then imports the result into TIA Portal through a Connector.
3. Table fields must remain stable and must not change arbitrarily between projects.
4. Free-text notes are allowed, but they do not replace structured fields.
5. All files must use UTF-8 encoding.
6. Identifier fields must use English ASCII only, without spaces, Chinese characters, or special symbols.
7. Names intended for HMI / SiVArc must not contain `% @ ? " / \ < > . :`

## 2. Recommended Folder Structure

```text
input-package/
  01-project.yaml
  02-rules.yaml
  03-equipment-list.csv
  04-io-list.csv
  05-module-cylinders.csv
  06-sequences.csv
  07-interlocks.csv
  08-alarms.csv
  09-hmi-requirements.csv
  10-acceptance-tests.csv
```

Example templates are provided in:

- `templates/input-package/01-project.yaml`
- `templates/input-package/02-rules.yaml`
- `templates/input-package/*.csv`

## 3. File Responsibilities

| File | Filled by | Purpose |
| --- | --- | --- |
| `01-project.yaml` | Project lead / PLC lead | Project identity, TIA version, target PLC, HMI, languages, and generation scope. |
| `02-rules.yaml` | Standardization lead | Naming rules, library versions, forbidden items, default state model, and validation strategy. |
| `03-equipment-list.csv` | Mechanical / electrical / PLC team | Unit, EM, and CM hierarchy and the module list. |
| `04-io-list.csv` | Electrical engineer | I/O addresses, symbolic names, comments, ownership, and HMI visibility. |
| `05-module-cylinders.csv` | PLC engineer | Cylinder instances, valve type, inputs/outputs, timeouts, home position, and HMI manual permission. |
| `06-sequences.csv` | PLC / process engineer | Sequence steps, transitions, and timeout handling. |
| `07-interlocks.csv` | PLC engineer / safety owner | Command permission, blocking conditions, failure actions, and prompts. |
| `08-alarms.csv` | PLC/HMI engineer | Alarm codes, severity, trigger condition, reset condition, and multilingual text. |
| `09-hmi-requirements.csv` | HMI engineer | Screen areas, faceplates, commands, parameters, states, and permissions. |
| `10-acceptance-tests.csv` | Commissioning / test lead | Compile, simulation, motion, alarm, and HMI binding checks. |

## 4. Field Rules

### 4.1 `01-project.yaml`

Key fields:

| Field | Required | Meaning |
| --- | --- | --- |
| `schema_version` | Yes | Input package template version. |
| `project.id` | Yes | Unique project ID. |
| `project.name` | Yes | TIA project or engineering project name. |
| `engineering.tia_portal_version` | Yes | Target TIA Portal version, for example `V21`. |
| `engineering.target_plc_family` | Yes | `S7-1500` or `S7-1200`. |
| `engineering.hmi_platform` | No | For example `WinCC Unified`. |
| `generation_scope` | Yes | Whether to generate PLC, HMI, Safety, or PackML assets. |
| `libraries` | Yes | Standard library, template library, and version information. |

### 4.2 `02-rules.yaml`

Key fields:

| Field | Meaning |
| --- | --- |
| `naming.object_case` | TIA object naming style, default `PascalCase`. |
| `naming.variable_prefixes` | Variable semantic prefix table. |
| `forbidden.absolute_addressing_in_logic` | Whether absolute addressing is forbidden in logic. |
| `forbidden.bit_memory_for_module_state` | Whether M area is forbidden for module state. |
| `defaults.optimized_blocks` | Whether new blocks default to optimized access. |
| `defaults.symbolic_addressing` | Whether symbolic addressing is mandatory. |
| `validation.required_checks` | Validation checks Logicwright must run. |

### 4.3 `03-equipment-list.csv`

| Field | Required | Meaning |
| --- | --- | --- |
| `equipment_id` | Yes | Unique equipment object ID. |
| `parent_id` | No | Parent object ID. Unit may be empty. |
| `level` | Yes | `Unit`, `EquipmentModule`, or `ControlModule`. |
| `name` | Yes | Generated object name. |
| `module_type` | Yes | `Cylinder`, `Motor`, `Valve`, `Station`, etc. |
| `template_id` | Yes | Standard template to use. |
| `generate_plc` | Yes | Whether PLC artifacts are generated. |
| `generate_hmi` | Yes | Whether HMI interfaces or screens are generated. |

### 4.4 `04-io-list.csv`

| Field | Required | Meaning |
| --- | --- | --- |
| `io_id` | Yes | Unique I/O row ID. |
| `equipment_id` | Yes | Owning equipment object. |
| `signal_name` | Yes | PLC symbolic name. |
| `direction` | Yes | `Input`, `Output`, or `InOut`. |
| `address` | Yes | TIA address such as `%I0.0` or `%Q0.0`. |
| `data_type` | Yes | `BOOL`, `INT`, `REAL`, etc. |
| `comment` | Yes | Engineering comment. |
| `safety_related` | Yes | Whether the signal is safety-related. If `true`, no automatic Safety logic may be generated. |
| `hmi_visible` | Yes | Whether the signal needs to appear on HMI. |
| `required` | Yes | Whether the signal is required by the module. |

### 4.5 `05-module-cylinders.csv`

| Field | Required | Meaning |
| --- | --- | --- |
| `cylinder_id` | Yes | Cylinder module ID. |
| `parent_equipment_id` | Yes | Owning EM or Unit. |
| `instance_name` | Yes | Instance name. |
| `valve_type` | Yes | `single_solenoid` or `double_solenoid`. |
| `extend_output` | Yes | Extend output signal name. |
| `retract_output` | Conditional | Retract output signal name. |
| `extended_input` | Yes | Extend-end feedback input. |
| `retracted_input` | Yes | Retract-end feedback input. |
| `enable_input` | No | External enable or air-pressure/interlock input. |
| `extend_timeout_ms` | Yes | Extend timeout in milliseconds. |
| `retract_timeout_ms` | Yes | Retract timeout in milliseconds. |
| `home_position` | Yes | `retracted` or `extended`. |
| `hmi_manual_allowed` | Yes | Whether HMI manual motion is allowed. |

### 4.6 `06-sequences.csv`

| Field | Meaning |
| --- | --- |
| `sequence_id` | Sequence ID. |
| `parent_equipment_id` | Owning EM or Unit. |
| `step_no` | Step number. |
| `step_name` | Step name. |
| `action` | Action such as `extend`, `retract`, or `wait`. |
| `command_target` | Target CM. |
| `transition_condition` | Transition condition expression. |
| `timeout_ms` | Step timeout. |
| `on_timeout_alarm` | Timeout alarm ID. |
| `next_step` | Next step number. |

### 4.7 `07-interlocks.csv`

| Field | Meaning |
| --- | --- |
| `interlock_id` | Interlock ID. |
| `equipment_id` | Owning equipment. |
| `target_command` | Command being constrained. |
| `condition_expression` | Allow-condition expression. |
| `severity` | `Alarm`, `Warning`, or `Info`. |
| `on_false_action` | Action when condition fails, such as `block_command` or `stop_module`. |
| `hmi_message_id` | HMI prompt or alarm ID. |

### 4.8 `08-alarms.csv`

| Field | Meaning |
| --- | --- |
| `alarm_id` | Alarm ID. |
| `equipment_id` | Owning equipment. |
| `alarm_code` | Project-unique alarm code. |
| `alarm_class` | `Alarm`, `Warning`, or `Status`. |
| `severity` | `High`, `Medium`, or `Low`. |
| `trigger_expression` | Trigger condition. |
| `reset_condition` | Reset condition. |
| `zh_CN` / `en_US` | Multilingual alarm text. |
| `stop_required` | Whether the module must stop or fault. |
| `first_fault_latch` | Whether the alarm contributes to first-fault latch. |

### 4.9 `09-hmi-requirements.csv`

| Field | Meaning |
| --- | --- |
| `hmi_id` | HMI requirement ID. |
| `equipment_id` | Bound equipment. |
| `screen_area` | Screen area or navigation node. |
| `faceplate_template` | Faceplate template. |
| `tag_prefix` | HMI tag prefix. |
| `commands` | Allowed commands, separated by semicolons. |
| `parameters` | Configurable parameters, separated by semicolons. |
| `status_fields` | Status fields to display. |
| `alarms_visible` | Whether alarms are visible. |
| `roles_allowed` | Allowed roles, separated by semicolons. |

### 4.10 `10-acceptance-tests.csv`

| Field | Meaning |
| --- | --- |
| `test_id` | Test ID. |
| `scope` | `PLC`, `HMI`, `Integration`, or `Simulation`. |
| `equipment_id` | Target equipment. |
| `precondition` | Precondition. |
| `steps` | Test steps. |
| `expected_result` | Expected result. |
| `test_type` | `compile`, `simulation`, `manual`, or `static_check`. |
| `required` | Whether the test is mandatory for v0.1. |

## 5. Recommended Editing Tools

| Tool | Recommended use | Notes |
| --- | --- | --- |
| Microsoft Excel | Main editing tool for engineers | Good for I/O lists, equipment lists, alarms, and HMI tables. |
| Google Sheets | Online multi-user collaboration | Good for early cross-functional review and shared editing. |
| LibreOffice Calc | Free offline editing | Suitable for teams that do not use commercial office suites. |
| Visual Studio Code + Red Hat YAML | Editing `01-project.yaml` and `02-rules.yaml` | Good for validation, completion, and schema-based editing. |
| TIA Portal / STEP 7 / WinCC Unified | Final import, compile, and validation | Siemens engineering environment. |
| Git / GitHub | Version control and review | Input packages should be versioned together with the project. |

Recommended workflow:

1. PLC/HMI engineers fill the CSV tables in Excel or Google Sheets.
2. The standardization lead edits `01-project.yaml` and `02-rules.yaml` in VS Code.
3. Export CSV files as UTF-8 before each submission.
4. Pass the package through Logicwright validation.
5. Validate the result in TIA Portal compilation and HMI binding checks.
6. Archive the input package, generated artifacts, and logs together.

## 6. Delivery Checklist

Before submitting the package, engineers must confirm:

- `equipment_id`, `io_id`, `alarm_id`, and `test_id` are globally unique.
- Every `equipment_id` exists in `03-equipment-list.csv`.
- Every module-referenced I/O signal exists in `04-io-list.csv`.
- `alarm_code` is unique within the project.
- HMI writable fields are marked with permissions and interlocks.
- Signals marked `safety_related=true` are not used for automatic Safety logic generation.
- Every `required=true` test case has an expected result.
- All tables are exported as UTF-8 CSV.
