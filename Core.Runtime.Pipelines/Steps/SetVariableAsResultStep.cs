using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Logging;

namespace Core.Runtime.Pipelines
{
    [Activity("Core.Runtime.Pipelines", "SetVariableAsResult",
       Category = "rDex.steps",
       Description = "Set a variable and also return as result.",
       DisplayName = "rPipe.SetVariableAsResult",
       Kind = ActivityKind.Task,
       Version = 1)]
    [FlowNode("Pass", "Fail")]
    public class SetVariableAsResultStep : Activity, IActivityWithResult
    {
        /// <summary>
        /// The name of the variable to set
        /// </summary>
        [Input(
            UIHint = InputUIHints.SingleLine,
            Name = "VariableName",
            DisplayName = "Variable Name",
            Description = "The name of the variable to set.",
            Category = "Props",
            DefaultSyntax = "Literal",
            Order = 1
        )]
        public Input<string> VariableName { get; set; } = default!;

        /// <summary>
        /// The value to set
        /// </summary>
        [Input(
            UIHint = InputUIHints.MultiLine,
            Name = "Value",
            DisplayName = "Value",
            Description = "The value to set.",
            Category = "Props",
            DefaultSyntax = "CSharp",
            Order = 2
        )]
        public Input<object> Value { get; set; } = default!;

        /// <summary>
        /// The output result
        /// </summary>
        [Output(
             DisplayName = "Result",
             Name = "Result",
             IsBrowsable = true,
             Description = "The set value as a result.",
             IsSerializable = true)]
        public Output? Result { get; set; }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var logger = context.GetRequiredService<ILogger<SetVariableAsResultStep>>();

            try
            {
                var variableName = context.Get(VariableName);
                var value = context.Get(Value);

                // Log variable and value
                logger.LogInformation("@@@@ Setting variable {VariableName} with value type {ValueType}",
                    variableName, value?.GetType().Name ?? "null");

                // Set variable in current scope
                context.SetVariable(variableName, value);

                // Set as result for the activity
                context.SetResult(value);

                // Complete with success
                await context.CompleteActivityWithOutcomesAsync("Pass");
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "@@@@ Error setting variable in {StepName}", context.Activity.Name);
                await context.CompleteActivityWithOutcomesAsync("Fail");
            }
        }
    }
}
