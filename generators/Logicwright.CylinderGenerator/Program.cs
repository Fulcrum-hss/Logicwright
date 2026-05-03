using System.Globalization;
using System.Text;

namespace Logicwright.CylinderGenerator;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = GeneratorOptions.Parse(args);
            var input = InputPackage.Load(options.InputPath);
            var cylinders = input.Cylinders
                .Where(cylinder => string.Equals(cylinder.TemplateId, CylinderGeneratorConstants.TemplateId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cylinders.Count == 0)
            {
                Console.Error.WriteLine("No CM_Cylinder_V0_1 rows were found in 05-module-cylinders.csv.");
                return 2;
            }

            Directory.CreateDirectory(options.OutputPath);
            var runId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            var generator = new CylinderArtifactGenerator(input, options.OutputPath, runId);
            generator.Generate(cylinders);

            Console.WriteLine("Generated CM_Cylinder artifacts.");
            Console.WriteLine("Input package: " + Path.GetFullPath(options.InputPath));
            Console.WriteLine("Output path: " + Path.GetFullPath(options.OutputPath));
            Console.WriteLine("Cylinder count: " + cylinders.Count);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            return 1;
        }
    }
}

internal sealed class GeneratorOptions
{
    public string InputPath { get; private init; } = string.Empty;
    public string OutputPath { get; private init; } = string.Empty;

    public static GeneratorOptions Parse(string[] args)
    {
        var input = "templates/input-package";
        var output = "artifacts/generated/cm-cylinder";

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--input":
                case "-i":
                    input = ReadValue(args, ref index, arg);
                    break;
                case "--output":
                case "-o":
                    output = ReadValue(args, ref index, arg);
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException("Unknown option: " + arg);
            }
        }

        return new GeneratorOptions
        {
            InputPath = input,
            OutputPath = output
        };
    }

    private static string ReadValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException(optionName + " requires a value.");
        }

        return args[++index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Logicwright CM_Cylinder Generator");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project generators/Logicwright.CylinderGenerator -- --input templates/input-package --output artifacts/generated/cm-cylinder");
    }
}

internal sealed class InputPackage
{
    public IReadOnlyList<CylinderRow> Cylinders { get; private init; } = Array.Empty<CylinderRow>();
    public IReadOnlyList<IoRow> IoRows { get; private init; } = Array.Empty<IoRow>();
    public IReadOnlyList<AlarmRow> AlarmRows { get; private init; } = Array.Empty<AlarmRow>();
    public IReadOnlyList<HmiRow> HmiRows { get; private init; } = Array.Empty<HmiRow>();

    public static InputPackage Load(string inputPath)
    {
        if (!Directory.Exists(inputPath))
        {
            throw new DirectoryNotFoundException("Input package folder not found: " + inputPath);
        }

        return new InputPackage
        {
            Cylinders = Csv.Read(Path.Combine(inputPath, "05-module-cylinders.csv"), CylinderRow.From),
            IoRows = Csv.Read(Path.Combine(inputPath, "04-io-list.csv"), IoRow.From),
            AlarmRows = Csv.Read(Path.Combine(inputPath, "08-alarms.csv"), AlarmRow.From),
            HmiRows = Csv.Read(Path.Combine(inputPath, "09-hmi-requirements.csv"), HmiRow.From)
        };
    }
}

internal sealed class CylinderArtifactGenerator
{
    private readonly InputPackage input;
    private readonly string outputPath;
    private readonly string runId;

    public CylinderArtifactGenerator(InputPackage input, string outputPath, string runId)
    {
        this.input = input;
        this.outputPath = outputPath;
        this.runId = runId;
    }

    public void Generate(IReadOnlyList<CylinderRow> cylinders)
    {
        var plcSources = Path.Combine(outputPath, "plc", "sources");
        var plcTags = Path.Combine(outputPath, "plc", "tags");
        var hmi = Path.Combine(outputPath, "hmi");
        var alarms = Path.Combine(outputPath, "alarms");
        var audit = Path.Combine(outputPath, "audit");

        Directory.CreateDirectory(plcSources);
        Directory.CreateDirectory(plcTags);
        Directory.CreateDirectory(hmi);
        Directory.CreateDirectory(alarms);
        Directory.CreateDirectory(audit);

        File.WriteAllText(Path.Combine(plcSources, "dbCylinderInterfaces.scl"), GenerateInterfaceDb(cylinders), Encoding.UTF8);
        File.WriteAllText(Path.Combine(plcSources, "FC_CallCylinders.scl"), GenerateCallFc(cylinders), Encoding.UTF8);
        File.WriteAllText(Path.Combine(plcTags, "IoTags.generated.csv"), GenerateIoTags(cylinders), Encoding.UTF8);
        File.WriteAllText(Path.Combine(hmi, "CylinderFaceplateBindings.generated.csv"), GenerateHmiBindings(cylinders), Encoding.UTF8);
        File.WriteAllText(Path.Combine(alarms, "CylinderAlarmBindings.generated.csv"), GenerateAlarmBindings(cylinders), Encoding.UTF8);
        File.WriteAllText(Path.Combine(audit, "manifest.json"), GenerateManifest(cylinders), Encoding.UTF8);
    }

    private string GenerateInterfaceDb(IEnumerable<CylinderRow> cylinders)
    {
        var writer = new SourceWriter();
        writer.Line("// Generated by Logicwright.CylinderGenerator.");
        writer.Line("// Do not edit generated files directly; update the input package and regenerate.");
        writer.Line("DATA_BLOCK \"dbCylinderInterfaces\"");
        writer.Line("{ S7_Optimized_Access := 'TRUE' }");
        writer.Line("VERSION : 0.1");
        writer.Line("   VAR");
        writer.Indent();

        foreach (var cylinder in cylinders)
        {
            writer.Line($"{cylinder.InstanceName} : \"typeCylinderIf\" := (");
            writer.Indent();
            writer.Line("Par := (");
            writer.Indent();
            writer.Line($"tExtendTimeout := T#{cylinder.ExtendTimeoutMs}ms,");
            writer.Line($"tRetractTimeout := T#{cylinder.RetractTimeoutMs}ms,");
            writer.Line($"xDoubleSolenoid := {BoolLiteral(cylinder.IsDoubleSolenoid)},");
            writer.Line($"xHomeExtended := {BoolLiteral(string.Equals(cylinder.HomePosition, "extended", StringComparison.OrdinalIgnoreCase))},");
            writer.Line($"xHmiManualAllowed := {BoolLiteral(cylinder.HmiManualAllowed)}");
            writer.Unindent();
            writer.Line(")");
            writer.Unindent();
            writer.Line(");");
        }

        writer.Unindent();
        writer.Line("   END_VAR");
        writer.Line("BEGIN");
        writer.Line("END_DATA_BLOCK");
        return writer.ToString();
    }

    private string GenerateCallFc(IEnumerable<CylinderRow> cylinders)
    {
        var writer = new SourceWriter();
        writer.Line("// Generated by Logicwright.CylinderGenerator.");
        writer.Line("// This FC calls standard CM_Cylinder instances and binds project I/O tags.");
        writer.Line("FUNCTION \"FC_CallCylinders\" : Void");
        writer.Line("{ S7_Optimized_Access := 'TRUE' }");
        writer.Line("VERSION : 0.1");
        writer.Line("BEGIN");
        writer.Indent();

        foreach (var cylinder in cylinders)
        {
            var enableInput = string.IsNullOrWhiteSpace(cylinder.EnableInput) ? "TRUE" : QuoteTag(cylinder.EnableInput);
            writer.Line($"REGION {cylinder.InstanceName} - {EscapeComment(cylinder.Description)}");
            writer.Indent();
            writer.Line($"\"CM_Cylinder\"(");
            writer.Indent();
            writer.Line($"xEnable := {enableInput},");
            writer.Line($"xReset := \"dbCylinderInterfaces\".{cylinder.InstanceName}.Cmd.xReset,");
            writer.Line("xAutoMode := TRUE,");
            writer.Line($"xManualMode := \"dbCylinderInterfaces\".{cylinder.InstanceName}.Cmd.xManualEnable,");
            writer.Line($"xFbExtended := {QuoteTag(cylinder.ExtendedInput)},");
            writer.Line($"xFbRetracted := {QuoteTag(cylinder.RetractedInput)},");
            writer.Line("xPermitExtend := TRUE,");
            writer.Line("xPermitRetract := TRUE,");
            writer.Line($"ioCmd := \"dbCylinderInterfaces\".{cylinder.InstanceName}.Cmd,");
            writer.Line($"ioPar := \"dbCylinderInterfaces\".{cylinder.InstanceName}.Par,");
            writer.Line($"ioSts := \"dbCylinderInterfaces\".{cylinder.InstanceName}.Sts,");
            writer.Line($"ioAlm := \"dbCylinderInterfaces\".{cylinder.InstanceName}.Alm,");
            writer.Line($"xValveExtend => {QuoteTag(cylinder.ExtendOutput)},");
            writer.Line($"xValveRetract => {QuoteTagOrFalse(cylinder.RetractOutput)},");
            writer.Line($"xReady => \"dbCylinderInterfaces\".{cylinder.InstanceName}.Sts.xReady,");
            writer.Line($"xBusy => \"dbCylinderInterfaces\".{cylinder.InstanceName}.Sts.xBusy,");
            writer.Line($"xDone => \"dbCylinderInterfaces\".{cylinder.InstanceName}.Sts.xDone,");
            writer.Line($"xError => \"dbCylinderInterfaces\".{cylinder.InstanceName}.Sts.xError,");
            writer.Line($"iState => \"dbCylinderInterfaces\".{cylinder.InstanceName}.Sts.iState,");
            writer.Line($"iErrorId => \"dbCylinderInterfaces\".{cylinder.InstanceName}.Sts.iErrorId");
            writer.Unindent();
            writer.Line(");");
            writer.Unindent();
            writer.Line("END_REGION");
            writer.Line();
        }

        writer.Unindent();
        writer.Line("END_FUNCTION");
        return writer.ToString();
    }

    private string GenerateIoTags(IEnumerable<CylinderRow> cylinders)
    {
        var rows = new List<string> { "name,address,data_type,comment,equipment_id,direction,source_io_id" };
        var cylinderIds = cylinders.Select(cylinder => cylinder.CylinderId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var io in input.IoRows.Where(row => cylinderIds.Contains(row.EquipmentId)).OrderBy(row => row.IoId, StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(Csv.Join(io.SignalName, io.Address, io.DataType, io.Comment, io.EquipmentId, io.Direction, io.IoId));
        }

        return string.Join(Environment.NewLine, rows) + Environment.NewLine;
    }

    private string GenerateHmiBindings(IEnumerable<CylinderRow> cylinders)
    {
        var rows = new List<string>
        {
            "hmi_id,equipment_id,instance_name,faceplate_template,tag_prefix,interface_db,commands,parameters,status_fields,alarms_visible,roles_allowed"
        };
        var cylinderById = cylinders.ToDictionary(cylinder => cylinder.CylinderId, StringComparer.OrdinalIgnoreCase);

        foreach (var hmi in input.HmiRows.Where(row => cylinderById.ContainsKey(row.EquipmentId)).OrderBy(row => row.HmiId, StringComparer.OrdinalIgnoreCase))
        {
            var cylinder = cylinderById[hmi.EquipmentId];
            rows.Add(Csv.Join(
                hmi.HmiId,
                hmi.EquipmentId,
                cylinder.InstanceName,
                hmi.FaceplateTemplate,
                hmi.TagPrefix,
                $"dbCylinderInterfaces.{cylinder.InstanceName}",
                hmi.Commands,
                hmi.Parameters,
                hmi.StatusFields,
                hmi.AlarmsVisible,
                hmi.RolesAllowed));
        }

        return string.Join(Environment.NewLine, rows) + Environment.NewLine;
    }

    private string GenerateAlarmBindings(IEnumerable<CylinderRow> cylinders)
    {
        var rows = new List<string>
        {
            "alarm_id,equipment_id,instance_name,alarm_code,alarm_class,severity,plc_alarm_bit,reset_condition,en_us,zh_cn,hmi_text_list_id,stop_required,first_fault_latch"
        };
        var cylinderById = cylinders.ToDictionary(cylinder => cylinder.CylinderId, StringComparer.OrdinalIgnoreCase);

        foreach (var alarm in input.AlarmRows.Where(row => cylinderById.ContainsKey(row.EquipmentId)).OrderBy(row => row.AlarmCode, StringComparer.OrdinalIgnoreCase))
        {
            var cylinder = cylinderById[alarm.EquipmentId];
            rows.Add(Csv.Join(
                alarm.AlarmId,
                alarm.EquipmentId,
                cylinder.InstanceName,
                alarm.AlarmCode,
                alarm.AlarmClass,
                alarm.Severity,
                MapAlarmBit(cylinder.InstanceName, alarm.AlarmId),
                alarm.ResetCondition,
                alarm.EnUs,
                alarm.ZhCn,
                alarm.HmiTextListId,
                alarm.StopRequired,
                alarm.FirstFaultLatch));
        }

        return string.Join(Environment.NewLine, rows) + Environment.NewLine;
    }

    private string GenerateManifest(IReadOnlyCollection<CylinderRow> cylinders)
    {
        var writer = new JsonWriter();
        writer.BeginObject();
        writer.Property("schemaVersion", "0.1");
        writer.Property("runId", runId);
        writer.Property("generatedAtUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        writer.Property("generator", "Logicwright.CylinderGenerator");
        writer.Property("standardTemplate", CylinderGeneratorConstants.TemplateId);
        writer.Property("standardLibraryVersion", "0.1.0");
        writer.PropertyName("outputs");
        writer.BeginArray();
        writer.Value("plc/sources/dbCylinderInterfaces.scl");
        writer.Value("plc/sources/FC_CallCylinders.scl");
        writer.Value("plc/tags/IoTags.generated.csv");
        writer.Value("hmi/CylinderFaceplateBindings.generated.csv");
        writer.Value("alarms/CylinderAlarmBindings.generated.csv");
        writer.EndArray();
        writer.PropertyName("cylinders");
        writer.BeginArray();

        foreach (var cylinder in cylinders)
        {
            writer.BeginObject();
            writer.Property("cylinderId", cylinder.CylinderId);
            writer.Property("instanceName", cylinder.InstanceName);
            writer.Property("templateId", cylinder.TemplateId);
            writer.EndObject();
        }

        writer.EndArray();
        writer.EndObject();
        return writer.ToString();
    }

    private static string MapAlarmBit(string instanceName, string alarmId)
    {
        var suffix = alarmId.ToUpperInvariant();
        if (suffix.Contains("EXT_TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            return $"dbCylinderInterfaces.{instanceName}.Alm.xExtendTimeout";
        }
        if (suffix.Contains("RET_TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            return $"dbCylinderInterfaces.{instanceName}.Alm.xRetractTimeout";
        }
        if (suffix.Contains("AIR_LOW", StringComparison.OrdinalIgnoreCase) || suffix.Contains("INTERLOCK", StringComparison.OrdinalIgnoreCase))
        {
            return $"dbCylinderInterfaces.{instanceName}.Alm.xInterlockLost";
        }
        if (suffix.Contains("CMD_CONFLICT", StringComparison.OrdinalIgnoreCase))
        {
            return $"dbCylinderInterfaces.{instanceName}.Alm.xCommandConflict";
        }
        if (suffix.Contains("FEEDBACK", StringComparison.OrdinalIgnoreCase))
        {
            return $"dbCylinderInterfaces.{instanceName}.Alm.xFeedbackConflict";
        }

        return $"dbCylinderInterfaces.{instanceName}.Alm.iFirstFaultId";
    }

    private static string QuoteTag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Required tag name is missing.");
        }

        return $"\"{name}\"";
    }

    private static string QuoteTagOrFalse(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "FALSE" : QuoteTag(name);
    }

    private static string BoolLiteral(bool value)
    {
        return value ? "TRUE" : "FALSE";
    }

    private static string EscapeComment(string value)
    {
        return value.Replace(Environment.NewLine, " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }
}

internal static class CylinderGeneratorConstants
{
    public const string TemplateId = "CM_Cylinder_V0_1";
}

internal sealed record CylinderRow(
    string CylinderId,
    string ParentEquipmentId,
    string InstanceName,
    string Description,
    string ValveType,
    string ExtendOutput,
    string RetractOutput,
    string ExtendedInput,
    string RetractedInput,
    string EnableInput,
    string InterlockGroup,
    int ExtendTimeoutMs,
    int RetractTimeoutMs,
    string HomePosition,
    bool HmiManualAllowed,
    string TemplateId)
{
    public bool IsDoubleSolenoid => string.Equals(ValveType, "double_solenoid", StringComparison.OrdinalIgnoreCase);

    public static CylinderRow From(Dictionary<string, string> row)
    {
        return new CylinderRow(
            CsvField.Required(row, "cylinder_id"),
            CsvField.Required(row, "parent_equipment_id"),
            CsvField.Required(row, "instance_name"),
            CsvField.Value(row, "description"),
            CsvField.Required(row, "valve_type"),
            CsvField.Required(row, "extend_output"),
            CsvField.Value(row, "retract_output"),
            CsvField.Required(row, "extended_input"),
            CsvField.Required(row, "retracted_input"),
            CsvField.Value(row, "enable_input"),
            CsvField.Value(row, "interlock_group"),
            CsvField.IntValue(row, "extend_timeout_ms"),
            CsvField.IntValue(row, "retract_timeout_ms"),
            CsvField.Required(row, "home_position"),
            CsvField.BoolValue(row, "hmi_manual_allowed"),
            CsvField.Required(row, "template_id"));
    }
}

internal sealed record IoRow(
    string IoId,
    string EquipmentId,
    string SignalName,
    string Direction,
    string Address,
    string DataType,
    string Comment)
{
    public static IoRow From(Dictionary<string, string> row)
    {
        return new IoRow(
            CsvField.Required(row, "io_id"),
            CsvField.Required(row, "equipment_id"),
            CsvField.Required(row, "signal_name"),
            CsvField.Required(row, "direction"),
            CsvField.Required(row, "address"),
            CsvField.Required(row, "data_type"),
            CsvField.Value(row, "comment"));
    }
}

internal sealed record AlarmRow(
    string AlarmId,
    string EquipmentId,
    string AlarmCode,
    string AlarmClass,
    string Severity,
    string ResetCondition,
    string ZhCn,
    string EnUs,
    string HmiTextListId,
    string StopRequired,
    string FirstFaultLatch)
{
    public static AlarmRow From(Dictionary<string, string> row)
    {
        return new AlarmRow(
            CsvField.Required(row, "alarm_id"),
            CsvField.Required(row, "equipment_id"),
            CsvField.Required(row, "alarm_code"),
            CsvField.Required(row, "alarm_class"),
            CsvField.Required(row, "severity"),
            CsvField.Value(row, "reset_condition"),
            CsvField.Value(row, "zh_CN"),
            CsvField.Value(row, "en_US"),
            CsvField.Value(row, "hmi_text_list_id"),
            CsvField.Value(row, "stop_required"),
            CsvField.Value(row, "first_fault_latch"));
    }
}

internal sealed record HmiRow(
    string HmiId,
    string EquipmentId,
    string FaceplateTemplate,
    string TagPrefix,
    string Commands,
    string Parameters,
    string StatusFields,
    string AlarmsVisible,
    string RolesAllowed)
{
    public static HmiRow From(Dictionary<string, string> row)
    {
        return new HmiRow(
            CsvField.Required(row, "hmi_id"),
            CsvField.Required(row, "equipment_id"),
            CsvField.Required(row, "faceplate_template"),
            CsvField.Required(row, "tag_prefix"),
            CsvField.Value(row, "commands"),
            CsvField.Value(row, "parameters"),
            CsvField.Value(row, "status_fields"),
            CsvField.Value(row, "alarms_visible"),
            CsvField.Value(row, "roles_allowed"));
    }
}

internal static class Csv
{
    public static IReadOnlyList<T> Read<T>(string path, Func<Dictionary<string, string>, T> create)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Required CSV file was not found.", path);
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
        if (lines.Count == 0)
        {
            return Array.Empty<T>();
        }

        var headers = Split(lines[0]);
        var result = new List<T>();
        for (var index = 1; index < lines.Count; index++)
        {
            var values = Split(lines[index]);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var headerIndex = 0; headerIndex < headers.Count; headerIndex++)
            {
                row[headers[headerIndex]] = headerIndex < values.Count ? values[headerIndex] : string.Empty;
            }
            result.Add(create(row));
        }

        return result;
    }

    public static string Join(params string[] values)
    {
        return string.Join(",", values.Select(Escape));
    }

    private static string Escape(string value)
    {
        value ??= string.Empty;
        if (!value.Contains(',', StringComparison.Ordinal) && !value.Contains('"', StringComparison.Ordinal) && !value.Contains('\n', StringComparison.Ordinal))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static IReadOnlyList<string> Split(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (ch == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(ch);
            }
        }

        values.Add(value.ToString());
        return values;
    }
}

internal sealed class SourceWriter
{
    private readonly StringBuilder builder = new();
    private int indent;

    public void Indent() => indent++;
    public void Unindent() => indent = Math.Max(0, indent - 1);

    public void Line(string value = "")
    {
        if (value.Length > 0)
        {
            builder.Append(new string(' ', indent * 3));
        }
        builder.AppendLine(value);
    }

    public override string ToString() => builder.ToString();
}

internal sealed class JsonWriter
{
    private readonly StringBuilder builder = new();
    private readonly Stack<bool> firstStack = new();
    private int indent;
    private bool awaitingPropertyValue;

    public void BeginObject()
    {
        BeforeValue();
        builder.Append("{");
        firstStack.Push(true);
        indent++;
    }

    public void EndObject()
    {
        indent--;
        if (firstStack.Count > 0 && !firstStack.Peek())
        {
            NewLine();
        }
        builder.Append("}");
        firstStack.Pop();
        awaitingPropertyValue = false;
    }

    public void BeginArray()
    {
        BeforeValue();
        builder.Append("[");
        firstStack.Push(true);
        indent++;
    }

    public void EndArray()
    {
        indent--;
        if (firstStack.Count > 0 && !firstStack.Peek())
        {
            NewLine();
        }
        builder.Append("]");
        firstStack.Pop();
        awaitingPropertyValue = false;
    }

    public void Property(string name, string value)
    {
        PropertyName(name);
        Value(value);
    }

    public void PropertyName(string name)
    {
        BeforeElement();
        WriteString(name);
        builder.Append(": ");
        awaitingPropertyValue = true;
    }

    public void Value(string value)
    {
        BeforeValue();
        WriteString(value);
        awaitingPropertyValue = false;
    }

    public override string ToString() => builder + Environment.NewLine;

    private void BeforeValue()
    {
        if (awaitingPropertyValue)
        {
            awaitingPropertyValue = false;
            return;
        }

        BeforeElement();
    }

    private void BeforeElement()
    {
        if (firstStack.Count == 0)
        {
            return;
        }

        var first = firstStack.Pop();
        if (!first)
        {
            builder.Append(",");
        }
        firstStack.Push(false);
        NewLine();
    }

    private void NewLine()
    {
        builder.AppendLine();
        builder.Append(new string(' ', indent * 2));
    }

    private void WriteString(string value)
    {
        builder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }
        builder.Append('"');
    }
}

internal static class CsvField
{
    public static string Required(Dictionary<string, string> row, string name)
    {
        var value = Value(row, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException("Missing required field: " + name);
        }

        return value;
    }

    public static string Value(Dictionary<string, string> row, string name)
    {
        return row.TryGetValue(name, out var value) ? value.Trim() : string.Empty;
    }

    public static int IntValue(Dictionary<string, string> row, string name)
    {
        var value = Required(row, name);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidDataException("Field " + name + " must be an integer.");
        }

        return result;
    }

    public static bool BoolValue(Dictionary<string, string> row, string name)
    {
        var value = Required(row, name);
        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        throw new InvalidDataException("Field " + name + " must be true or false.");
    }
}
