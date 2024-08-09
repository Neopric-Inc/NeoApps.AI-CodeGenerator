using System.Text.Json;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Collections.Generic;
using System;

namespace NoCodeAppGenerator
{
    class WorkflowGenerator
    {
        private static string DestinationFolderPath = "../Workflows";
        private static string SwaggerFilePath = Directory.GetCurrentDirectory()+$"/{Program.projectName}.API/swagger.json";
        private static string ComponentType = "task";
        private static string ComponentId = "1";
        private static string ToolType = "DBOperation";

        public static void generateWorkflowCalled()
        {
            // Make 3 folder for each type of Workflow
            createWorkflowFolders(DestinationFolderPath);

            // Read Swagger File in Proper Format
            SwaggerDocument document = readSwaggerFile(SwaggerFilePath);

            // Generate Workflow Files from Swagger Document
            generateWorkflowFiles(document);
        }

        static void createWorkflowFolders(string path)
        {
            try
            {
                Directory.CreateDirectory(path + "/POST");
                Directory.CreateDirectory(path + "/POST/FILTER");
                Directory.CreateDirectory(path + "/PUT");
                Directory.CreateDirectory(path + "/DELETE");
                Directory.CreateDirectory(path + "/DELETE/MULTIPLE");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in createWorkflowFolders:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in createWorkflowFolders: " + ex.Message);
            }
        }

        static SwaggerDocument readSwaggerFile(string path)
        {
            // // Create a new instance of the HttpClientHandler class.
            // var handler = new HttpClientHandler();

            // // Set the ServerCertificateCustomValidationCallback property of the handler to a lambda function that always returns true.
            // // This will bypass certificate validation for any server certificate, even if it's self-signed or issued by an untrusted CA.
            // handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            // // Create a new instance of the HttpClient class, passing in the handler as an argument.
            // var client = new HttpClient(handler);

            // // Make a GET request to the Swagger JSON file URL and wait for the response.
            // var response = client.GetAsync(path).Result;

            // // Read the content of the response as a string and wait for it to complete.
            // string swaggerJson = response.Content.ReadAsStringAsync().Result;
            try
            {
                string swaggerJson = File.ReadAllText(path);
                // Parse the Swagger JSON using System.Text.Json
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var document = JsonSerializer.Deserialize<SwaggerDocument>(swaggerJson, options);

                return document;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in readSwaggerFile:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in readSwaggerFile: " + ex.Message);
            }
            return null;
        }

        // Loop through the API paths and create the workflows.json files
        static void generateWorkflowFiles(SwaggerDocument document)
        {
            try
            {
                foreach (var path in document.Paths)
                {
                    var post = path.Value.Post;
                    var put = path.Value.Put;
                    var delete = path.Value.Delete;

                    if (post != null)
                    {
                        if (post.parameters == null)
                            post.parameters = new Parameters();

                        CreateWorkflowsJson(path.Key, "POST", "create", post.tags, path.Key, document.Components.Schemas, post.parameters, post.requestBody);
                    }

                    if (put != null)
                    {
                        CreateWorkflowsJson(path.Key, "PUT", "update", put.tags, path.Key, document.Components.Schemas, put.parameters, put.requestBody);
                    }

                    if (delete != null)
                    {
                        if (delete.requestBody == null)
                            delete.requestBody = new RequestBody();

                        CreateWorkflowsJson(path.Key, "DELETE", "delete", delete.tags, path.Key, document.Components.Schemas, delete.parameters, delete.requestBody);
                        // CreateWorkflowsJson(path.Key, "DELETE", delete.operationId, "delete", delete.tags, delete.parameters, path.Key, document.Components.Schemas);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in generateWorkflowFiles:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in generateWorkflowFiles: " + ex.Message);
            }
        }

        static void CreateWorkflowsJson(string path, string method, string type, TagItem tags, string queryPath, DefinitionDictionary definitions, Parameters parameters, RequestBody requestBody)
        {
            try
            {
                // Create the workflows.json filename
                // string filename = $"{DestinationFolderPath}/{method}/{tags[0]}_{method}.json";

            string sendContent = tags[0];
            sendContent += "_" + char.ToUpper(method[0]) + method.Substring(1).ToLower();

            if (method == "POST" || method == "PUT")
            {
                createParamsFromPost_Put(ref parameters, requestBody);
                // Create Property Field for workflow

                var propertyObject = createPropertiesObj(parameters, sendContent, queryPath, method, definitions);

                // Create the JSON object for workflows.json file contents
                Workflow workflow = new Workflow(ComponentType, ComponentId, propertyObject, sendContent, type);
                tags[0]=tags[0].ToLower();
                // Write Workflows to Destination File
                if (path.EndsWith("/filter"))
                {
                    string filename = $"{DestinationFolderPath}/{method}/FILTER/{tags[0]}_{method}_Filter.json";
                    writeWorkflowFile(filename, workflow);
                }
                else
                {
                    string filename = $"{DestinationFolderPath}/{method}/{tags[0]}_{method}.json";
                    writeWorkflowFile(filename, workflow);
                }
            }
            else
            {
                removeSchema(ref parameters);
                // Create Property Field for workflow
                var propertyObject = createPropertiesObj(parameters, sendContent, queryPath, method, definitions);

                    // Create the JSON object for workflows.json file contents
                    Workflow workflow = new Workflow(ComponentType, ComponentId, propertyObject, sendContent, type);
                    tags[0] = tags[0].ToLower();
                    // Write Workflows to Destination File
                    if (path.EndsWith("/Multiple"))
                    {
                        Console.WriteLine(tags[0]);
                        string filename = $"{DestinationFolderPath}/{method}/MULTIPLE/{tags[0]}_{method}_Multiple.json";
                        writeWorkflowFile(filename, workflow);
                    }
                    else
                    {
                        string filename = $"{DestinationFolderPath}/{method}/{tags[0]}_{method}.json";
                        writeWorkflowFile(filename, workflow);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in CreateWorkflowsJson:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in CreateWorkflowsJson: " + ex.Message);
            }
        }

        static Parameters removeSchema(ref Parameters parameters)
        {
            try
            {
                foreach (Parameter parameter in parameters)
                {
                    if (parameter.In == "path")
                    {
                        // if (parameter.Schema.Type != null)
                        //     parameter.Type = parameter.Schema.Type;
                        // if (parameter.Schema.Format != null)
                        //     parameter.Format = parameter.Schema.Format;
                        foreach (var KeyValuePair in parameter.Schema)
                        {
                            string key = KeyValuePair.Key;
                            if (key == "type")
                                parameter.Type = Convert.ToString(KeyValuePair.Value);
                            if (key == "format")
                                parameter.Format = Convert.ToString(KeyValuePair.Value);
                        }
                        parameter.Schema = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in removeSchema:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in removeSchema: " + ex.Message);
            }
            return parameters;
        }
        static Parameters createParamsFromPost_Put(ref Parameters parameters, RequestBody requestBody)
        {
            try
            {
                foreach (KeyValuePair<string, application_json> kvp in requestBody.content)
                {
                    if (kvp.Key == "application/json")
                    {
                        // Schema schema;
                        // if (kvp.Value.schema.Type == "array")
                        //     schema = kvp.Value.schema.Items;
                        // else
                        //     schema = kvp.Value.schema;

                        Parameters old_param = parameters;
                        Schema schema = kvp.Value.schema;
                        ParamSchema paramSchema = new ParamSchema();
                        if (schema.Type == "array" && schema.Items != null)
                        {
                            string componentRefString = schema.Items.Ref;
                            string defintionRefString = componentRefString.Replace("#/components/schemas/", "#/definitions/");
                            // schema.Ref = defintionRefString;
                            paramSchema["$ref"] = defintionRefString;
                        }
                        else
                        {
                            string componentRefString = schema.Ref;
                            // Replace "#/components/schemas/" with "#/definitions/"
                            string defintionRefString = componentRefString.Replace("#/components/schemas/", "#/definitions/");
                            // schema.Ref = defintionRefString;
                            paramSchema["$ref"] = defintionRefString;
                        }
                        // foreach (var KeyValuePair in schema)
                        // {
                        //     if (KeyValuePair.Key == "$ref")
                        //     {
                        //         string componentRefString = KeyValuePair.Value;
                        //         // Replace "#/components/schemas/" with "#/definitions/"
                        //         string defintionRefString = componentRefString.Replace("#/components/schemas/", "#/definitions/");
                        //         schema["$ref"] = defintionRefString;
                        //     }
                        // }
                        Parameter new_parameter = new Parameter("model", "body", true, paramSchema, false);
                        parameters.Add(new_parameter);
                    }
                    // foreach (KeyValuePair<string, string> schemaKvp in kvp.Value.schema)
                    // {
                    //     Console.WriteLine($"Schema key: {schemaKvp.Key}, value: {schemaKvp.Value}");
                    // }
                }
                foreach (Parameter parameter in parameters)
                {
                    if (parameter.In == "path")
                    {
                        foreach (var KeyValuePair in parameter.Schema)
                        {
                            string key = KeyValuePair.Key;
                            if (key == "type")
                            {
                                parameter.Type = Convert.ToString(KeyValuePair.Value);
                            }
                            if (key == "format")
                            {
                                parameter.Format = Convert.ToString(KeyValuePair.Value);
                            }
                        }
                        parameter.Schema = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in createParamsFromPost_Put:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in createParamsFromPost_Put: " + ex.Message);
            }
            return parameters;
        }
        // For Creating Dynamic Properties fields for Put , Post & Delete type
        static Properties createPropertiesObj(Parameters parameters, string sendContent, string queryPath, string reqType, DefinitionDictionary definitions)
        {
            Query queryObject = createQueryObj(parameters, definitions);

            // We need same field as a query inside "op" of "properties" but with different Key "value"
            foreach (var parameter in parameters)
                parameter.Value = queryObject;

            Properties propertiespObject = new Properties(parameters, queryObject, queryPath, reqType.ToLower(), sendContent, ToolType);
            return propertiespObject;
        }

        // Create Custom Query Object for all type of Requests. Post , put and Delete
        static Query createQueryObj(Parameters parameters, DefinitionDictionary definitions)
        {
            Query queryObject = new Query();

            foreach (var parameter in parameters)
            {
                // For Input Type body (send Data in Body Object) , mostly in Post Request
                if (parameter.In == "body" && parameter.Name == "model")
                {
                    // Extract modelName from Schema
                    string modelName = getModelNamefromRef(Convert.ToString(parameter.Schema["$ref"]));

                    // Outer Keys are the Field Name like backend_stack_id , backend_stack_name 
                    foreach (var outerKey in definitions[modelName].properties.Keys)
                    {
                        queryObject.Add(new Dictionary<string, object> { { outerKey, null }, { "body", true } });
                    }
                }

                // For Input Type path (send Data in Query Path, ) mostly in Put and Delete Request
                if (parameter.In == "path")
                    queryObject.Add(new Dictionary<string, object> { { parameter.Name, null }, { "path", true } });
            }

            return queryObject;
        }

        // Convert modelName from Schema $ref , i.e => "#/definitions/Frontend_stacksModel" to modelName = "Frontend_stacksModel"
        static string getModelNamefromRef(string Ref)
        {
            string[] splitedStr = Ref.Split('/');
            string modelName = splitedStr[splitedStr.Length - 1];
            return modelName;
        }

        static void writeWorkflowFile(string filename, Workflow workflow)
        {
            // Create the workflows.json file contents from JSON object
            var contents = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });

            // Write the contents to the workflows.json file
            File.WriteAllText(filename, contents);
        }

    }
}


// var contents = $"{{ \"path\": \"{path}\", \"method\": \"{method}\", \"operationId\": \"{operationId}\", \"tags\": \"{tags[0]}\" }}";
// var opObject = new { op = parameters };

// Create the JSON object for workflows.json file contents
// var contentsObject = new { componentType = "Task", id = 1, name = operationId, properties = opObject, type = type, path = path, method = method, operationId = operationId };

// public class KeyValue
// {
//     public string FieldName { get; set; }
//     public string Value { get; set; }
// }

// foreach (var definition in document.Definitions)
// {
//     if (definition.Key == "Backend_stacksModel")
//         Console.WriteLine(definition.Value.properties.Values);

// inside outer Keys
// Console.WriteLine($"Outer Key: {outerKey}");
// var innerDict = definitions[modelName].properties[outerKey];
// foreach (var innerKey in innerDict.Keys)
// {
//     if(innerKey == "type")
//     Console.WriteLine($"  Inner Key: {innerKey}, Value: {innerDict[innerKey]}");
// }