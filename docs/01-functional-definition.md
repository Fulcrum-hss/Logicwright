# Logicwright Functional Definition

## 1. Project Positioning

Logicwright is an open-source AI engineering workbench for industrial automation engineers.

Its core purpose is not to provide a general-purpose chatbot. The goal is to build a verifiable, auditable, and reversible engineering automation chain that helps engineers create PLC programs, HMI configurations, and electrical design assets in tools such as TIA Portal, EPLAN, and SOLIDWORKS Electrical.

The first target scenario is:

- PLC module generation for Siemens TIA Portal V21
- Template-driven HMI generation for WinCC Unified
- Future extension to EPLAN electrical schematics and SOLIDWORKS Electrical project generation and validation

## 2. Product Vision

Logicwright aims to become open-source infrastructure for industrial engineering. It should help engineers generate high-quality engineering assets from structured requirements, template libraries, and rule libraries, while using compilation, validation, and audit loops to ensure the results are usable.

Core principles:

- AI does not replace engineers; it amplifies engineering productivity.
- Generation must be constrained by templates, rules, and project context.
- Every generation run must produce validation results, logs, and traceable diffs.
- Engineering tools remain the primary work environment; Logicwright acts as an enhancement layer.

## 3. Target Users

Primary users:

- Electrical engineers for custom machinery
- PLC engineers
- HMI engineers
- Electrical design engineers
- Automation team leads

Secondary users:

- Senior engineers responsible for standard module libraries
- Technical managers responsible for machine standardization
- Software engineers building engineering digitalization platforms

## 4. Core Value

Logicwright must continuously provide the following value:

1. Reduce development time for standard engineering tasks.
2. Improve consistency and standardization across PLC, HMI, and electrical deliverables.
3. Reduce errors caused by manual copy-paste work.
4. Turn team experience into reusable templates and rules.
5. Provide compilation, validation, repair suggestions, and auditability for AI-generated results.

## 5. Product Boundary

The first release explicitly supports:

- Structured requirement input
- PLC standard module generation
- Template-driven HMI generation
- TIA Portal V21 integration
- Engineering rule validation
- Generated result diff review
- Compile result feedback and repair suggestions

The first release does not support:

- Automatic generation of Safety programs
- Automatic download to real equipment
- Free-form generation of complete complex machine HMIs
- Automatic programming of high-risk motion control loops
- Unconstrained free project creation without templates

## 6. Business Workflow

The standard Logicwright workflow is:

1. The user provides structured requirements.
2. The system loads project context, templates, and rules.
3. AI generates an intermediate `spec`.
4. Validators check naming, interfaces, and structural consistency.
5. AI/generators create PLC, HMI, or electrical artifacts.
6. A Connector imports artifacts into the target engineering tool.
7. The system triggers compilation or consistency checks.
8. Errors are parsed and limited repair iterations are executed.
9. The user reviews diffs and logs before accepting the result.

## 7. Functional Modules

### 7.1 Requirement Input Module

Goals:

- Support structured form input.
- Support limited natural-language clarification.
- Support import of I/O lists, device lists, and process steps.

v0.1 capabilities:

- Device type selection
- I/O point entry
- Action flow definition
- Alarm and interlock definition
- HMI requirement definition
- Input template saving and reuse

### 7.2 Project Context Module

Goals:

- Read existing project objects and naming conventions.
- Provide generation context.

v0.1 capabilities:

- Read basic project information.
- Scan existing PLC blocks and namespaces.
- Scan HMI screens and tag tables.
- Identify template versions.
- Load project rules.

### 7.3 AI Orchestration Module

Goals:

- Convert user requirements and engineering context into reliable outputs.
- Prevent unconstrained free-form generation.

v0.1 capabilities:

- Convert requirements to intermediate `spec`.
- Convert `spec` to `design`.
- Convert `design` to `artifacts`.
- Manage prompt templates.
- Record multi-step generation logs.

### 7.4 PLC Generation Module

Goals:

- Generate standardized PLC engineering assets.

v0.1 capabilities:

- Generate UDTs.
- Generate DBs.
- Generate FB/FC blocks.
- Generate SCL code skeletons.
- Generate variable comments and names.
- Instantiate standard modules.

Priority module types:

- Cylinder
- Motor
- Variable frequency drive
- Analog loop
- Single-station state machine

### 7.5 HMI Generation Module

Goals:

- Generate reusable HMI screens and data objects based on templates.

v0.1 capabilities:

- Generate tag tables.
- Generate text lists.
- Generate graphic lists.
- Generate standard popup pages.
- Generate device control views.
- Bind variables automatically.

The first release is template-driven and does not allow free-form layout generation.

### 7.6 Engineering Connector Module

Goals:

- Connect Logicwright with target engineering software.

Connector roadmap:

- `TIA Connector`
- `EPLAN Connector`
- `SOLIDWORKS Electrical Connector`

The first release only implements `TIA Connector`.

TIA Connector v0.1 capabilities:

- Connect to a running TIA Portal V21 instance.
- Open or identify the current project.
- Import PLC artifacts.
- Import HMI artifacts.
- Trigger compilation.
- Read compile diagnostics.

### 7.7 Validation and Repair Module

Goals:

- Validate generated results before they enter the formal engineering project.

v0.1 capabilities:

- Naming rule validation
- Interface consistency validation
- Template compatibility validation
- Compile error parsing
- Repair suggestion generation
- Limited automatic retry

### 7.8 Audit and Diff Module

Goals:

- Make every generated change explicit to the engineer.

v0.1 capabilities:

- Archive task input.
- Archive generated artifacts.
- Provide diff review.
- Archive compile logs.
- Archive repair records.
- Record final user confirmation.

## 8. Non-Functional Requirements

### 8.1 Traceability

Each generation task must record:

- Input parameters
- Model version
- Template versions
- Rule versions
- Output artifacts
- Import result
- Compile log

### 8.2 Controllability

- All automatic generation must be constrained by rules.
- High-risk objects must not be modified directly by default.
- Real equipment download is not allowed by default.
- Manual review is required before applying changes.

### 8.3 Extensibility

- Support multiple engineering tool Connectors.
- Support multiple model providers.
- Support community templates and rules.
- Support enterprise private knowledge bases.

### 8.4 Open-Source Friendliness

- Core capabilities should be organized as open source.
- Templates, rules, examples, and specifications should support community contributions.
- The architecture should be decoupled from commercial models and private deployments.

## 9. v0.1 Delivery Scope

Recommended v0.1 scope:

1. Basic project structure
2. Unified `spec` data structure
3. TIA Connector MVP
4. PLC module generation MVP
5. HMI template generation MVP
6. Logging and audit MVP
7. One runnable sample module

Recommended sample module:

- `Cylinder module`

Reasoning:

- The scenario is highly standardized.
- PLC and HMI requirements are clear.
- It is suitable for a complete demonstration loop.

## 10. Evolution Roadmap

Mid-term:

- EPLAN Connector
- More PLC module types
- Team template repositories
- Visual project rule configuration
- Enterprise knowledge base integration

Long-term:

- SOLIDWORKS Electrical Connector
- Cross-tool engineering object linkage
- Multi-role approval workflows
- Test-driven engineering generation
- Community module marketplace

## 11. Success Criteria

The first release is successful when:

1. Engineers can input standard module requirements and generate importable artifacts.
2. TIA Portal V21 can import and compile the generated result.
3. Generated naming and structure comply with predefined rules.
4. Logs, diffs, and repair records are traceable.
5. The sample module can be reused in a real project.

## 12. Naming

`Logicwright` combines `Logic` and `Wright`, meaning a builder of engineering logic.

The name emphasizes:

- Control logic and engineering logic
- Structured construction rather than pure chat-based generation
- Suitability as a long-term platform brand for PLC, HMI, and electrical engineering
