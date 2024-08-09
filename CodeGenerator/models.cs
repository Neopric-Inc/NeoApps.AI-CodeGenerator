using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;

namespace NoCodeAppGenerator
{
    // Define the SwaggerDocument class to match the structure of the Swagger JSON file
    class SwaggerDocument
    {
        public string Openapi { get; set; }
        public Info Info { get; set; }
        // public string Host { get; set; }
        public PathDictionary Paths { get; set; }
        public ComponentsDictionary Components { get; set; }
        // public DefinitionDictionary Definitions { get; set; }
    }

    class ComponentsDictionary
    {
        public DefinitionDictionary Schemas { get; set; }
    }
    class Info
    {
        public string Title { get; set; }
        public string Version { get; set; }
    }

    class PathDictionary : Dictionary<string, PathItem> { }

    class DefinitionDictionary : Dictionary<string, DefinitionItem> { }

    class DefinitionItem
    {
        public string type { get; set; }
        public RequiredItem required { get; set; }
        public DefinitionProperty properties { get; set; }
    }

    class RequiredItem : List<string> { };

    class DefinitionProperty : Dictionary<string, Dictionary<string, object>> { };

    class PathItem
    {
        [JsonPropertyName("post")]
        public OperationItem Post { get; set; }
        [JsonPropertyName("put")]
        public OperationItem Put { get; set; }
        // [JsonIgnore]
        [JsonPropertyName("delete")]
        public OperationItem Delete { get; set; }
    }

    class OperationItem
    {
        public TagItem tags { get; set; }
        // public string operationId { get; set; }
        public Parameters parameters { get; set; }
        [JsonPropertyName("requestBody")]
        public RequestBody requestBody { get; set; }
    }

    class RequestBody
    {
        public Content content { get; set; }
    }

    class Content : Dictionary<string, application_json> { }

    class application_json
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Schema schema { get; set; }
    }

    class TagItem : List<string> { };

    class Parameters : List<Parameter> { };

    class Parameter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("in")]
        public string In { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("schema")]
        public ParamSchema? Schema { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("value")]
        public Query Value { get; set; }

        [JsonPropertyName("x-nullable")]
        public bool XNullable { get; set; }

        // public Parameter(string Type, string Name, string In, bool Required, Schema schema, string Format, Query Value, bool XNullable)
        // {

        // }
        // public Parameter(string Type, string Name, string In, bool Required, Schema schema, string Format, Query Value, bool XNullable)
        // {

        // }
        public Parameter(string Name, string In, bool Required, ParamSchema schema, bool XNullable)
        {
            this.Name = Name;
            this.In = In;
            this.Required = Required;
            this.Schema = schema;
            this.XNullable = XNullable;
        }
    }

    // class ParamSchema : Dictionary<string, string>
    // {
    // }

    public class ParamSchema : Dictionary<string, object> { }

    // public class ParamSchema : Dictionary<string, object>
    // {
    //     public void DeserializeJson(string jsonString)
    //     {
    //         ParamSchema schema = JsonSerializer.Deserialize<ParamSchema>(jsonString);

    //         // Access the values in the schema dictionary
    //         foreach (KeyValuePair<string, object> pair in schema)
    //         {
    //             string key = pair.Key;
    //             object value = pair.Value;

    //             // Check the type of the value before casting it
    //             if (value is string)
    //             {
    //                 string stringValue = (string)value;
    //                 // Do something with the string value...
    //             }
    //             else if (value is int)
    //             {
    //                 int intValue = (int)value;
    //                 // Do something with the int value...
    //             }
    //             // Add additional checks for other value types as needed
    //         }
    //     }
    // }

    // class ParamSchema
    // {
    //     [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //     [JsonPropertyName("type")]
    //     public string? Type;
    //     [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //     [JsonPropertyName("format")]
    //     public string? Format;
    //     [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //     [JsonPropertyName("default")]
    //     public string? Default;
    // }

    // class Schema_with_item
    // {
    //     [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //     [JsonPropertyName("type")]
    //     public string? Type { get; set; }
    //     [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    //     [JsonPropertyName("item")]
    //     public Schema? Item { get; set; }

    // }

    // class Schema : Dictionary<string, string> { }

    public class Schema
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("items")]
        public Item Items { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("$ref")]
        public string Ref { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("$ref")]
        public string Ref { get; set; }
    }
    // public class Schema : IEnumerable<KeyValuePair<string, string>>
    // {
    //     private Dictionary<string, string>? _properties = new Dictionary<string, string>();
    //     public string? Type { get; set; }
    //     public Schema? Items { get; set; }

    //     public string this[string key]
    //     {
    //         get => _properties[key];
    //         set => _properties[key] = value;
    //     }

    //     public Dictionary<string, string> Properties
    //     {
    //         get => _properties;
    //         set => _properties = value;
    //     }


    //     public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    //     {
    //         return _properties.GetEnumerator();
    //     }

    //     IEnumerator IEnumerable.GetEnumerator()
    //     {
    //         return GetEnumerator();
    //     }
    // }



    class Workflow
    {
        [JsonPropertyName("componentType")]
        public string ComponentType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        public Workflow(string componentType, string id, Properties properties, string name, string type)
        {
            this.ComponentType = componentType;
            this.Id = id;
            this.Properties = properties;
            this.Name = name;
            this.Type = type;
        }
    }

    class Properties
    {
        [JsonPropertyName("op")]
        public Parameters Op { get; set; }

        [JsonPropertyName("query")]
        public Query Query { get; set; }

        [JsonPropertyName("queryPath")]
        public string QueryPath { get; set; }

        [JsonPropertyName("reqType")]
        public string ReqType { get; set; }

        [JsonPropertyName("sendContent")]
        public string SendContent { get; set; }

        [JsonPropertyName("toolType")]
        public string ToolType { get; set; }

        public Properties(Parameters op, Query queryObject, string queryPath, string reqType, string sendContent, string toolType)
        {
            this.Op = op;
            this.Query = queryObject;
            this.QueryPath = queryPath;
            this.ReqType = reqType;
            this.SendContent = sendContent;
            this.ToolType = toolType;
        }
    }

    class Query : List<Dictionary<string, object>> { };

    public class EverythingToStringJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {

            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? String.Empty;
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                var stringValue = reader.GetDouble();
                return stringValue.ToString();
            }
            else if (reader.TokenType == JsonTokenType.False ||
                reader.TokenType == JsonTokenType.True)
            {
                return reader.GetBoolean().ToString();
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Skip();
                return "(not supported)";
            }
            else
            {
                Console.WriteLine($"Unsupported token type: {reader.TokenType}");

                throw new System.Text.Json.JsonException();
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}