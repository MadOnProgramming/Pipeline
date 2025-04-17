using Core.Runtime.Job.Abstractions;
using Core.Runtime.Pipelines;
using Elsa.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Core.Pipeline.Job.v1
{
    public class PipelineHandler : IJob
    {
        #region private variables
        
        private readonly ILogger<PipelineHandler> _logger;
        private readonly IJobExecutionContext _jobExecutionContext;
        private readonly IPipelineExecutor _pipelineExecutor;

        #endregion
        public PipelineHandler(ILogger<PipelineHandler> logger,
                            IJobExecutionContext jobExecutionContext,
                            IPipelineExecutor pipelineExecutor)
        {
            _logger = logger;
            _jobExecutionContext = jobExecutionContext;
            _pipelineExecutor = pipelineExecutor;
        }

        public async Task ExecuteAsync(JobData jobData)
        {
            try
            {
                _logger.LogInformation("@@@@ Begin Invoke Pipeline Job Handler ExecuteAsync");

                #region Extract service id and data from job data

                if (!jobData.Data.TryGetValue("flowid", out var flowId))
                {
                    _logger.LogError("@@@@ flow configuration id is null");
                    throw new InvalidDataException("flow configuration id is null");
                }

                if (!jobData.Data.TryGetValue("flowjobdata", out var flowjobdata))
                {
                    _logger.LogWarning("@@@@ flowjobdata is null");
                }

                var pipeRequest = new PipelineRequest
                {
                    FlowId = flowId.ToString(),
                    //HACK: This is a hack to convert the object to dictionary and then back to object to avoid serialization issues
                    Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(flowjobdata)),
                    Context = new PipeContext
                    {
                        UserId = _jobExecutionContext.Context.UserId,
                        CorrelationId = _jobExecutionContext.Context.CorrelationId,
                        OUId = _jobExecutionContext.Context.OUId,
                        RoleId = _jobExecutionContext.Context.RoleId,
                        SegmentId = _jobExecutionContext.Context.SegmentId, 
                        TenantId = _jobExecutionContext.Context.TenantId,
                        FlowInstanceId = jobData.Data.TryGetValue("trigerringflowinstanceid", out var id)  ? (id == null ? _jobExecutionContext.ExecutionId : id.ToString()) : _jobExecutionContext.ExecutionId
                    }
                };

                _logger.LogInformation("@@@@ PipelineRequest : {0}", JsonConvert.SerializeObject(pipeRequest, Newtonsoft.Json.Formatting.Indented));


                #endregion

                 var res = await _pipelineExecutor.ExecuteAsync(pipeRequest);

                 Thread.Sleep(10000);

                _logger.LogInformation("@@@@ End Invoke Pipeline Job Handler ExecuteAsync staus :{0} flowInstanceId : {1}", res.Status, res.FlowInstanceId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "@@@@ Exception in Pipeline Job Handler ExecuteAsync");
            }
        }
    }
}
