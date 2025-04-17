using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RepoDb;
using RepoDb.Enumerations;

namespace Core.Runtime.Pipelines
{
    [Activity("Core.Runtime.Pipelines", "ExecuteQueryStep",
        Category = "rDex.steps",
        Description = "Execute the given SQL queries or Stored procedures and return the resulting data.",
        DisplayName = "rPipe.ExecuteQuery",
        Kind = ActivityKind.Task,
        Version = 1)]

    [FlowNode("Pass", "Fail")]
    public class ExecuteQueryStep : Activity, IActivityWithResult
    {
        #region Inputs
        [Input(
           UIHint = InputUIHints.SingleLine,
           Name = "ConnectionString",
           DisplayName = "Connection String",
           Description = "The connection string details.",
           CanContainSecrets = true,
           Category = "Props",
           DefaultSyntax = "Literal",
           DefaultValue = "Data Source=technetcnv01.pearl.com;Max Pool Size =150;Database=Elsa;User Id=rvwuser;Password=rvw;TrustServerCertificate=True;",
           SupportedSyntaxes = new[] { "Literal", "CSharp" },
           Order = 1)]
        public Input<string> ConnectionString { get; set; }

        /// <summary>
        /// Query to run against the database.
        /// </summary>
        [Input(
            UIHint = InputUIHints.MultiLine,
            DisplayName = "Command Text",
            Name = "CommandText",
            Description = "The SQL query or SP command to execute.\r\n(eg. \"DECLARE @WorkitemId NVARCHAR(510); EXEC Rdel_generate_workitem_sp @SetName = @SetName_p,@CtxtUser = @CtxtUser_p,@WorkitemId = @WorkitemId OUTPUT; SELECT @WorkitemId AS GeneratedWorkitemId;\")",
            DefaultValue = "DECLARE @WorkitemId NVARCHAR(510); \r\nEXEC Rdel_generate_workitem_sp @SetName = @SetName_p,@CtxtUser = @CtxtUser_p,@WorkitemId = @WorkitemId OUTPUT; \r\nSELECT @WorkitemId AS GeneratedWorkitemId;",
            Category = "Props",
            DefaultSyntax = "CSharp",
            SupportedSyntaxes = new[] { "Literal", "CSharp" },
            Order = 2)]
        public Input<string> CommandText { get; set; }

        [Input(
            UIHint = InputUIHints.MultiLine,
            DisplayName = "Command Parameters",
            Name = "CommandParameters",
            Description = "The SQL query or SP command Parameters.\r\n(eg. \"return new { SetName_p = Input.Get<PipeData>(\\\"pd\\\").Get<string>(\\\"SetName_p\\\") , CtxtUser_p = Input.Get<PipeContext>(\\\"pc\\\").UserId};)",
            DefaultValue = "return new { SetName_p = Input.Get<PipeData>(\"pd\").Get<string>(\"SetName_p\") , CtxtUser_p = Input.Get<PipeContext>(\"pc\").UserId};",
            Category = "Props",
            DefaultSyntax = "CSharp",
            SupportedSyntaxes = new[] { "Literal", "CSharp" },
            Order = 3)]
        public Input<object> CommandParameters { get; set; }
        #endregion

        #region Outputs
        [Output(
             DisplayName = "Result",
             Name = "Result",
             IsBrowsable = true,
             Description = "Result of the commandtext.",
             IsSerializable = false)]
        public Output? Result { get; set; }
        #endregion

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var logger = context.GetRequiredService<ILogger<ExecuteQueryStep>>();
            var qryTracer = context.GetRequiredService<IQueryTrace>();

            try
            {
                logger.LogInformation("@@@@ Executing executeQueryStep {StepName}", context.Activity.Name);

                var connectionString = context.Get(ConnectionString);
                var commandText = context.Get(CommandText);
                var commandParams = context.Get(CommandParameters);
            
                using var connection = new SqlConnection(connectionString);
                var result = await connection.ExecuteQueryAsync(commandText, commandParams,trace:qryTracer);
                
                context.SetResult(result.ToList());

                /*context.Set(QueryResult, result.ToList());
                context.SetVariable("result1", result.ToList());*/

                Console.WriteLine("executed sql with rows affected {0}", result.Count());
                
                logger.LogInformation("@@@@ Executed executeQueryStep {StepName} rows affected {Count}", context.Activity.Name,result.Count());
                
                await context.CompleteActivityWithOutcomesAsync("Pass");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "@@@@ Error executing executeQueryStep {StepName}", context.Activity.Name);

                /*context.AddExecutionLogEntry("StepError", ex.Message, payload: new
                {
                    StackTrace = ex.StackTrace
                });
                context.JournalData.Add("StepError", ex.Message);
                await context.CompleteActivityWithOutcomesAsync(ActivityStatus.Faulted.ToString());*/

                throw new StepException(ex.Message, ex);
            }
        }
    }
}
