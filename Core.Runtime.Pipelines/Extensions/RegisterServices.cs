using Core.Runtime.Pipelines.Steps.Sql;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using RepoDb;

namespace Core.Runtime.Pipelines
{
    public static class RegisterServices
    {
        public static IServiceCollection AddPipelineServices(this IServiceCollection services)
        {
            GlobalConfiguration
                  .Setup()
                  .UseSqlServer();

            services.AddTransient<IPipelineExecutor, PipelineExecutor>();
            services.AddScoped<IPipeResolver, PipeResolver>();

            services.AddHttpClient();
            // Configure RepoDb trace
            services.AddSingleton<IQueryTrace, QueryTrace>();


            services.AddElsa(elsa =>
            {
                elsa
                   .UseLiquid()
                   .UseCSharp(opts =>
                   {
                       //opts.Assemblies.Add(typeof(JObject).Assembly);
                       opts.Assemblies.Add(typeof(PipeContext).Assembly);
                       opts.Namespaces.Add("Core.Runtime.Pipelines");
                   })
                   ;

                elsa
                .AddActivity<Elsa.Workflows.Activities.Sequence>()
                .AddActivity<Elsa.Workflows.Activities.Flowchart.Activities.Flowchart>()
                .AddActivity<Elsa.Workflows.Activities.WriteLine>()
                .AddActivity<Elsa.Workflows.Activities.Correlate>()
                .AddActivity<Elsa.Workflows.Activities.ForEach>()
                .AddActivity<Elsa.Workflows.Activities.ForEach<object>>()
                .AddActivity<ExecuteQueryStep>()
                .AddActivity<CreateBatchQueueStep>()
                .AddActivity<InvokeApiStep>()
                .AddActivity<EnqueueJobStep>()
                .AddActivity<LoggerStep>()
                .AddActivity<LongrunningLoggerStep>()
                .AddActivity<SetVariableAsResultStep>()
                 ;
            });
            return services;
        }
    }
}
