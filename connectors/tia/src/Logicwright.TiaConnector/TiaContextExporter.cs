using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;

namespace Logicwright.TiaConnector
{
    internal sealed class TiaContextExporter
    {
        public int Run(TiaSessionOptions options)
        {
            using (var portal = TiaPortalSession.Open(options))
            {
                var project = portal.Projects.FirstOrDefault();
                if (project == null)
                {
                    throw new InvalidOperationException("No project is currently open.");
                }

                var json = BuildProjectContextJson(project);
                var outputPath = Path.GetFullPath(options.OutputPath);
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                File.WriteAllText(outputPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                Console.WriteLine("Project context exported to: " + outputPath);
            }

            return 0;
        }

        private static string BuildProjectContextJson(Project project)
        {
            var writer = new JsonTextWriter();
            writer.BeginObject();
            writer.Property("schemaVersion", "0.2");
            writer.Property("exportedAtUtc", DateTime.UtcNow.ToString("o"));

            writer.PropertyName("project");
            writer.BeginObject();
            writer.Property("name", project.Name);
            writer.Property("path", project.Path == null ? null : project.Path.ToString());
            writer.EndObject();

            writer.PropertyName("devices");
            writer.BeginArray();
            foreach (var device in project.Devices)
            {
                WriteDevice(writer, device);
            }
            writer.EndArray();

            writer.EndObject();
            return writer.ToString();
        }

        private static void WriteDevice(JsonTextWriter writer, Device device)
        {
            writer.BeginObject();
            writer.Property("name", device.Name);
            writer.Property("typeIdentifier", Safe(() => device.TypeIdentifier));

            writer.PropertyName("deviceItems");
            writer.BeginArray();
            foreach (var item in device.DeviceItems)
            {
                WriteDeviceItem(writer, item, depth: 0);
            }
            writer.EndArray();

            writer.EndObject();
        }

        private static void WriteDeviceItem(JsonTextWriter writer, DeviceItem item, int depth)
        {
            writer.BeginObject();
            writer.Property("name", item.Name);
            writer.Property("typeIdentifier", Safe(() => item.TypeIdentifier));
            writer.Property("classification", Safe(() => item.Classification.ToString()));

            WriteServiceInfos(writer, item);

            var software = GetSoftware(item);
            if (software != null)
            {
                writer.PropertyName("software");
                WriteSoftware(writer, software);
            }

            if (depth < 8 && item.DeviceItems.Any())
            {
                writer.PropertyName("deviceItems");
                writer.BeginArray();
                foreach (var child in item.DeviceItems)
                {
                    WriteDeviceItem(writer, child, depth + 1);
                }
                writer.EndArray();
            }

            writer.EndObject();
        }

        private static void WriteServiceInfos(JsonTextWriter writer, DeviceItem item)
        {
            writer.PropertyName("services");
            writer.BeginArray();

            var serviceProvider = item as IEngineeringServiceProvider;
            if (serviceProvider != null)
            {
                var serviceTypeNames = SafeEnumerable(() => serviceProvider.GetServiceInfos())
                    .Select(serviceInfo => Safe(() => serviceInfo.Type))
                    .Where(serviceType => serviceType != null)
                    .Select(serviceType => serviceType.FullName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

                foreach (var serviceTypeName in serviceTypeNames)
                {
                    writer.Value(serviceTypeName);
                }
            }

            writer.EndArray();
        }

        private static object GetSoftware(DeviceItem item)
        {
            var softwareContainer = Safe(() => item.GetService<SoftwareContainer>());
            if (softwareContainer != null)
            {
                return Safe(() => softwareContainer.Software);
            }

            return null;
        }

        private static void WriteSoftware(JsonTextWriter writer, object software)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(software, "Name")));
            writer.Property("type", software.GetType().FullName);

            if (software.GetType().FullName == "Siemens.Engineering.SW.PlcSoftware")
            {
                WritePlcSoftware(writer, software);
            }
            else if (software.GetType().FullName == "Siemens.Engineering.Hmi.HmiTarget")
            {
                WriteHmiTarget(writer, software);
            }
            else
            {
                WriteNamedObjectProperties(writer, software, new[] { "Author" });
            }

            writer.EndObject();
        }

        private static void WritePlcSoftware(JsonTextWriter writer, object software)
        {
            writer.PropertyName("blockGroups");
            WriteTree(writer, GetPropertyValue(software, "BlockGroup"), "blocks", "Blocks", "groups", "Groups", WritePlcBlock);

            writer.PropertyName("typeGroups");
            WriteTree(writer, GetPropertyValue(software, "TypeGroup"), "types", "Types", "groups", "Groups", WritePlcType);

            writer.PropertyName("tagTableGroups");
            WriteTree(writer, GetPropertyValue(software, "TagTableGroup"), "tagTables", "TagTables", "groups", "Groups", WritePlcTagTable);

            writer.PropertyName("externalSourceGroups");
            WriteTree(writer, GetPropertyValue(software, "ExternalSourceGroup"), "externalSources", "ExternalSources", "groups", "Groups", WriteNamedObject);
        }

        private static void WriteHmiTarget(JsonTextWriter writer, object hmiTarget)
        {
            WriteNamedObjectProperties(writer, hmiTarget, new[] { "Author" });

            writer.PropertyName("connections");
            WriteObjectCollection(writer, GetPropertyValue(hmiTarget, "Connections"), WriteNamedObject);

            writer.PropertyName("screens");
            WriteTree(writer, GetPropertyValue(hmiTarget, "ScreenFolder"), "screens", "Screens", "folders", "Folders", WriteNamedObject);

            writer.PropertyName("screenTemplates");
            WriteTree(writer, GetPropertyValue(hmiTarget, "ScreenTemplateFolder"), "screenTemplates", "ScreenTemplates", "folders", "Folders", WriteNamedObject);

            writer.PropertyName("tagTables");
            WriteTree(writer, GetPropertyValue(hmiTarget, "TagFolder"), "tagTables", "TagTables", "folders", "Folders", WriteHmiTagTable);

            writer.PropertyName("textLists");
            WriteObjectCollection(writer, GetPropertyValue(hmiTarget, "TextLists"), WriteNamedObject);

            writer.PropertyName("graphicLists");
            WriteObjectCollection(writer, GetPropertyValue(hmiTarget, "GraphicLists"), WriteNamedObject);
        }

        private static void WriteTree(
            JsonTextWriter writer,
            object group,
            string itemJsonName,
            string itemPropertyName,
            string groupJsonName,
            string groupPropertyName,
            Action<JsonTextWriter, object> writeItem)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(group, "Name")));
            writer.Property("type", group == null ? null : group.GetType().FullName);

            writer.PropertyName(itemJsonName);
            WriteObjectCollection(writer, GetPropertyValue(group, itemPropertyName), writeItem);

            writer.PropertyName(groupJsonName);
            writer.BeginArray();
            foreach (var childGroup in ToEnumerable(GetPropertyValue(group, groupPropertyName)))
            {
                WriteTree(writer, childGroup, itemJsonName, itemPropertyName, groupJsonName, groupPropertyName, writeItem);
            }
            writer.EndArray();

            writer.EndObject();
        }

        private static void WriteObjectCollection(JsonTextWriter writer, object collection, Action<JsonTextWriter, object> writeItem)
        {
            writer.BeginArray();
            foreach (var item in ToEnumerable(collection))
            {
                writeItem(writer, item);
            }
            writer.EndArray();
        }

        private static void WritePlcBlock(JsonTextWriter writer, object block)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(block, "Name")));
            writer.Property("type", block.GetType().FullName);
            writer.Property("number", SafeString(() => GetPropertyValue(block, "Number")));
            writer.Property("programmingLanguage", SafeString(() => GetPropertyValue(block, "ProgrammingLanguage")));
            writer.Property("memoryLayout", SafeString(() => GetPropertyValue(block, "MemoryLayout")));
            writer.Property("isConsistent", SafeString(() => GetPropertyValue(block, "IsConsistent")));
            writer.Property("isKnowHowProtected", SafeString(() => GetPropertyValue(block, "IsKnowHowProtected")));
            writer.Property("headerVersion", SafeString(() => GetPropertyValue(block, "HeaderVersion")));
            writer.Property("modifiedDate", SafeString(() => GetPropertyValue(block, "ModifiedDate")));
            writer.EndObject();
        }

        private static void WritePlcType(JsonTextWriter writer, object plcType)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(plcType, "Name")));
            writer.Property("type", plcType.GetType().FullName);
            writer.Property("isConsistent", SafeString(() => GetPropertyValue(plcType, "IsConsistent")));
            writer.Property("isKnowHowProtected", SafeString(() => GetPropertyValue(plcType, "IsKnowHowProtected")));
            writer.Property("modifiedDate", SafeString(() => GetPropertyValue(plcType, "ModifiedDate")));
            writer.EndObject();
        }

        private static void WritePlcTagTable(JsonTextWriter writer, object table)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(table, "Name")));
            writer.Property("type", table.GetType().FullName);
            writer.Property("isDefault", SafeString(() => GetPropertyValue(table, "IsDefault")));

            writer.PropertyName("tags");
            WriteObjectCollection(writer, GetPropertyValue(table, "Tags"), WritePlcTag);

            writer.EndObject();
        }

        private static void WritePlcTag(JsonTextWriter writer, object tag)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(tag, "Name")));
            writer.Property("logicalAddress", SafeString(() => GetPropertyValue(tag, "LogicalAddress")));
            writer.Property("dataTypeName", SafeString(() => GetPropertyValue(tag, "DataTypeName")));
            writer.Property("comment", SafeString(() => GetPropertyValue(tag, "Comment")));
            writer.Property("isSafety", SafeString(() => GetPropertyValue(tag, "IsSafety")));
            writer.Property("externalVisible", SafeString(() => GetPropertyValue(tag, "ExternalVisible")));
            writer.Property("externalAccessible", SafeString(() => GetPropertyValue(tag, "ExternalAccessible")));
            writer.Property("externalWritable", SafeString(() => GetPropertyValue(tag, "ExternalWritable")));
            writer.EndObject();
        }

        private static void WriteHmiTagTable(JsonTextWriter writer, object table)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(table, "Name")));
            writer.Property("type", table.GetType().FullName);
            writer.Property("isSystemObject", SafeString(() => GetPropertyValue(table, "IsSystemObject")));

            writer.PropertyName("tags");
            WriteObjectCollection(writer, GetPropertyValue(table, "Tags"), WriteNamedObject);

            writer.EndObject();
        }

        private static void WriteNamedObject(JsonTextWriter writer, object value)
        {
            writer.BeginObject();
            writer.Property("name", SafeString(() => GetPropertyValue(value, "Name")));
            writer.Property("type", value == null ? null : value.GetType().FullName);
            writer.EndObject();
        }

        private static void WriteNamedObjectProperties(JsonTextWriter writer, object value, IEnumerable<string> propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                writer.Property(ToCamelCase(propertyName), SafeString(() => GetPropertyValue(value, propertyName)));
            }
        }

        private static object GetPropertyValue(object source, string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            var property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                return null;
            }

            return property.GetValue(source, null);
        }

        private static IEnumerable<object> ToEnumerable(object collection)
        {
            var enumerable = collection as IEnumerable;
            if (enumerable == null || collection is string)
            {
                yield break;
            }

            foreach (var item in enumerable)
            {
                yield return item;
            }
        }

        private static IEnumerable<T> SafeEnumerable<T>(Func<IEnumerable<T>> getter)
        {
            try
            {
                return getter() ?? Enumerable.Empty<T>();
            }
            catch
            {
                return Enumerable.Empty<T>();
            }
        }

        private static T Safe<T>(Func<T> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return default(T);
            }
        }

        private static string SafeString(Func<object> getter)
        {
            var value = Safe(getter);
            return value == null ? null : value.ToString();
        }

        private static string ToCamelCase(string value)
        {
            return string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
    }

    internal sealed class JsonTextWriter
    {
        private readonly StringBuilder builder = new StringBuilder();
        private readonly Stack<ContainerState> containers = new Stack<ContainerState>();
        private int indent;
        private bool propertyValuePending;

        public void BeginObject()
        {
            BeforeValue();
            builder.Append("{");
            containers.Push(new ContainerState(true));
            indent++;
        }

        public void EndObject()
        {
            indent--;
            if (containers.Count > 0 && !containers.Peek().First)
            {
                NewLine();
            }
            builder.Append("}");
            containers.Pop();
            propertyValuePending = false;
        }

        public void BeginArray()
        {
            BeforeValue();
            builder.Append("[");
            containers.Push(new ContainerState(true));
            indent++;
        }

        public void EndArray()
        {
            indent--;
            if (containers.Count > 0 && !containers.Peek().First)
            {
                NewLine();
            }
            builder.Append("]");
            containers.Pop();
            propertyValuePending = false;
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
            propertyValuePending = true;
        }

        public void Value(string value)
        {
            BeforeValue();
            if (value == null)
            {
                builder.Append("null");
            }
            else
            {
                WriteString(value);
            }
            propertyValuePending = false;
        }

        public override string ToString()
        {
            return builder.ToString() + Environment.NewLine;
        }

        private void BeforeValue()
        {
            if (propertyValuePending)
            {
                propertyValuePending = false;
                return;
            }

            BeforeElement();
        }

        private void BeforeElement()
        {
            if (containers.Count == 0)
            {
                return;
            }

            var state = containers.Pop();
            if (state.First)
            {
                state.First = false;
            }
            else
            {
                builder.Append(",");
            }
            containers.Push(state);

            NewLine();
        }

        private void NewLine()
        {
            builder.AppendLine();
            builder.Append(new string(' ', indent * 2));
        }

        private void WriteString(string value)
        {
            builder.Append("\"");
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
                        if (char.IsControl(ch))
                        {
                            builder.Append("\\u");
                            builder.Append(((int)ch).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(ch);
                        }
                        break;
                }
            }
            builder.Append("\"");
        }

        private struct ContainerState
        {
            public bool First;

            public ContainerState(bool first = true)
            {
                First = first;
            }
        }
    }
}
