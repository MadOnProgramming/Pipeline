using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Runtime.Pipelines
{
   [Activity("Core.Runtime.Pipelines", "LoggerStep",
   Category = "rDex.steps",
   Description = "Write a line of text to the log.",
   DisplayName = "rPipe.Logger",
   Kind = ActivityKind.Action,
   Version = 1)]

    [FlowNode("Pass", "Fail")]
    public class LoggerStep : Activity
    {
        /// <summary>
        /// The text to write.
        /// </summary>
        [Input(
         UIHint = InputUIHints.SingleLine,
         Name = "Text",
         DisplayName = "Text",
         Description = "The text to write.",
         Category = "Props",
         DefaultSyntax = "CSharp",
         Order = 1
        )]
        public Input<string> Text { get; set; } = default!;
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var logger = context.GetRequiredService<ILogger<LoggerStep>>();

            if (!context.WorkflowInput.TryGetValue<PipeContext>("pc", out var pipeContext))
            {
                logger.LogError("@@@@ PipeContext is null");
                throw new InvalidDataException("PipeContext is null");
            }

            var text = context.Get(Text);

            var currentIndex = context.GetVariable<int?>("CurrentIndex");
            var currentValue = context.GetVariable<object?>("CurrentValue");

            logger.LogInformation("@@@@ {Text}", text);

            //logger.LogInformation("@@@@ Executing Longrunning LoggerStep \nText :{Text}\nactivity :{StepName} role :{RoleId}\n currentIndex :{CurrentIndex}\n currentValue :{CurrentValue}",
            //      text, context.Activity.Name, pipeContext.RoleId, currentIndex, currentValue);

            await context.CompleteActivityWithOutcomesAsync("Pass");
        }
    }
}
