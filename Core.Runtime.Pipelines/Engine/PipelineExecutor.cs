using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RepoDb;
using System.Collections;
using System.Text.Json;
using System.Xml;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Core.Runtime.Pipelines
{
    public class PipelineExecutor : IPipelineExecutor
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public PipelineExecutor(
            ILogger<PipelineExecutor> logger,
            IServiceProvider serviceProvider
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        public async Task<PipelineResponse> ExecuteAsync(PipelineRequest pipelineRequest)
        {

            try
            {
                var pipelineidentifier = pipelineRequest.FlowId;

                _logger.LogInformation("@@@@ Begin Invoke PipelineExecutor configid : {0}", pipelineidentifier);

                /*
                 var workflowJson = await File.ReadAllTextAsync(String.Format("./flows/{0}.json", pipelineidentifier));
                // Get a serializer to deserialize the workflow.
                var serializer = _serviceProvider.GetRequiredService<IActivitySerializer>();
                var workflowDefinitionModel = serializer.Deserialize<Workflow>(workflowJson);
                var pipeId = String.Format("./flows/{0}.json", pipelineidentifier);
                */


                var registriesPopulator = _serviceProvider.GetService<IRegistriesPopulator>();
                await registriesPopulator!.PopulateAsync();

                var workflowDefinitionModel = await GetPipelineModel(pipelineidentifier);

                // Get a workflow runner to run the workflow.
                var workflowRunner = _serviceProvider.GetRequiredService<IWorkflowRunner>();
                var pipeResolver = _serviceProvider.GetRequiredService<IPipeResolver>();
                // Run the workflow.

                var wfInput = new Dictionary<string, object>()
                {
                    {"pc",pipelineRequest.Context },
                    {"pd",new  PipeData(pipelineRequest.Data) },
                    {"pr",pipeResolver }
                };
                _logger.LogInformation("@@@@ flow input : {0}", JsonConvert.SerializeObject(wfInput, Newtonsoft.Json.Formatting.Indented));

                var wfexecutionResult = await workflowRunner.RunAsync(workflowDefinitionModel
                    , new RunWorkflowOptions()
                    {
                        Input = wfInput,
                        WorkflowInstanceId = pipelineRequest.Context.FlowInstanceId,
                        CorrelationId = pipelineRequest.Context.CorrelationId
                        
                    });

                // LogExecutionResult
                switch (wfexecutionResult.WorkflowState.SubStatus)
                {
                    case WorkflowSubStatus.Finished:
                        _logger.LogInformation("@@@@ End Invoke PipelineExecutor executed successfully");
                        break;

                    case WorkflowSubStatus.Faulted:
                        wfexecutionResult.WorkflowState.Incidents.ToList().ForEach(incident =>
                        {
                            _logger.LogError("@@@@ Pipeline activityid id {0} execution failed with incident {1}", incident.ActivityId, incident.Message);
                        });
                        _logger.LogError("@@@@ End Invoke PipelineExecutor execution failed");
                        break;
                    case WorkflowSubStatus.Suspended:
                        _logger.LogInformation("@@@@ End Invoke PipelineExecutor execution Suspended");
                        break;

                    default:
                        _logger.LogInformation("@@@@ End Invoke PipelineExecutor execution status not known");
                        break;
                }

                return new PipelineResponse()
                {
                    FlowInstanceId = wfexecutionResult.WorkflowState.Id,
                    Status = wfexecutionResult.WorkflowState.SubStatus.ToString() ?? "nosubstatus"
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "@@@@ Error PipelineExecutor executing pipeline configid {0}", pipelineRequest.FlowId);
                return new PipelineResponse()
                {
                    FlowInstanceId = pipelineRequest.FlowId,
                    Status = "failed"
                };
            }
        }

        private async Task<Workflow> GetPipelineModel(string pipelineidentifier)
        {
            String pipelineJson;

            if (pipelineidentifier.EndsWith(".yml"))
            {
                var yamlContent = await File.ReadAllTextAsync(String.Format("./flows/{0}", pipelineidentifier));

                YamlStream yamlStream = new YamlStream();
                yamlStream.Load(new StringReader(yamlContent));

                var yamlDocument = yamlStream.Documents.FirstOrDefault();
                var jsonObject = ConvertNodeToJson(yamlDocument.RootNode);

                pipelineJson = JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);
            }
            else if (pipelineidentifier.EndsWith(".json"))
            {
                pipelineJson = await File.ReadAllTextAsync(String.Format("./flows/{0}", pipelineidentifier));
            }
            else
            {
                throw new InvalidDataException("Invalid pipeline file format");
            }

            // Get a serializer to deserialize the workflow.
            var serializer = _serviceProvider.GetRequiredService<IActivitySerializer>();
            var workflowDefinitionModel = serializer.Deserialize<Workflow>(pipelineJson);
            return workflowDefinitionModel;
        }


        private static object ConvertNodeToJson(YamlNode node)
        {
            if (node is YamlMappingNode mappingNode)
            {
                var dictionary = new Dictionary<string, object>();
                foreach (var entry in mappingNode.Children)
                {
                    var key = entry.Key.ToString();
                    dictionary[key] = ConvertNodeToJson(entry.Value); // Recursively process the value
                }
                return dictionary;
            }
            else if (node is YamlSequenceNode sequenceNode)
            {
                var list = new List<object>();
                foreach (var item in sequenceNode.Children)
                {
                    list.Add(ConvertNodeToJson(item)); // Recursively process each item in the sequence
                }
                return list;
            }
            else if (node is YamlScalarNode scalarNode)
            {
                // Try to convert to different types
                if (int.TryParse(scalarNode.Value, out int intValue))
                {
                    return intValue; // Convert to integer if possible
                }
                else if (bool.TryParse(scalarNode.Value, out bool boolValue))
                {
                    return boolValue; // Convert to boolean if possible
                }
                else if (float.TryParse(scalarNode.Value, out float floatValue))
                {
                    return floatValue; // Convert to float if possible
                }
                else if (decimal.TryParse(scalarNode.Value, out decimal decimalValue))
                {
                    return decimalValue; // Convert to decimal if possible
                }
                return scalarNode.Value; // Otherwise, return as string
            }

            return null;
        }
    }
}
