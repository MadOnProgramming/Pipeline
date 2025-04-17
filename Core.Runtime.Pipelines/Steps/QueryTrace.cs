using Microsoft.Extensions.Logging;
using RepoDb;
using RepoDb.Interfaces;
using System.Text;

namespace Core.Runtime.Pipelines.Steps.Sql
{
    /// <summary>
    /// RepoDb trace object for the database operations.
    /// </summary>
    public class QueryTrace : IQueryTrace
    {
        private readonly ILogger<QueryTrace> _logger;

        public QueryTrace(ILogger<QueryTrace> logger)
        {
            _logger = logger;
        }
        void ITrace.AfterExecution<TResult>(ResultTraceLog<TResult> log)
        {
            _logger.LogInformation("@@@@ After execution qry trace session id: {SessionId},total execution time (in Seconds): {TotalSeconds}", log.SessionId, log.ExecutionTime.TotalSeconds);

        }

        Task ITrace.AfterExecutionAsync<TResult>(ResultTraceLog<TResult> log, CancellationToken cancellationToken)
        {
            _logger.LogInformation("@@@@ After executionasync qry trace session id: {SessionId},total execution time (in Seconds): {TotalSeconds}", log.SessionId, log.ExecutionTime.TotalSeconds);

            return Task.CompletedTask;
        }

        void ITrace.BeforeExecution(CancellableTraceLog log)
        {
            
            StringBuilder cmdParams = new StringBuilder();

            log.Parameters.ToList().ForEach(parameter =>
            {
                cmdParams = cmdParams.AppendLine(parameter.ParameterName + " : " + parameter.Value + " , ");
            });
            
            _logger.LogInformation("@@@@ Before execution qry trace session id: {SessionId}, statement:{Statement} \n{Params}", log.SessionId, log.Statement, cmdParams.ToString());

        }

        Task ITrace.BeforeExecutionAsync(CancellableTraceLog log, CancellationToken cancellationToken)
        {
            StringBuilder cmdParams = new StringBuilder();

            log.Parameters.ToList().ForEach(parameter =>
            {
                cmdParams = cmdParams.AppendLine(parameter.ParameterName + " : " + parameter.Value + " , ");
            });

            _logger.LogInformation("@@@@ Before executionasync qry trace session id: {SessionId}, statement:{Statement} \n{Params}", log.SessionId, log.Statement, cmdParams.ToString());

            return Task.CompletedTask;
        }
    }

}
