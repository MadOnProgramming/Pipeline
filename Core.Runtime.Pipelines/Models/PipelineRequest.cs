using Core.Runtime.Abstractions;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Runtime.Pipelines
{
    public class PipelineRequest
    {
        public string FlowId { get; set; }
        public IDictionary<string, object> Data { get; set; }
        public PipeContext Context { get; set; }
    }


    public class PipeData
    {
        private IDictionary<string, object> _data { get; set; }
        public PipeData(IDictionary<string, object> data)
        {
            _data = data;
        }
        public IList<object> GetList()
        {
            //convert a dictionary to a list
            return _data.Values.ToList();
        }
        public TValue Get<TValue>(string key)
        {
            if (_data == null)
                throw new InvalidOperationException("Data dictionary is not initialized.");

            if (!_data.ContainsKey(key))
                throw new KeyNotFoundException($"The key '{key}' was not found in the data dictionary.");

            var value = _data[key];

            if (value is TValue typedValue)
            {
                return typedValue;
            }

            try
            {
                return (TValue)Convert.ChangeType(value, typeof(TValue));
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"The value associated with the key '{key}' cannot be cast to type '{typeof(TValue).Name}'.");
            }
        }
    }

    /// <summary>
    /// Pipe line context
    /// </summary>
    public class PipeContext
    {
        /// <summary>
        /// User Id-Principal User
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Context Role
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// Ou Instance
        /// </summary>
        public int OUId { get; set; }

        /// <summary>
        /// Tenant Identifier
        /// </summary>
        public String? TenantId { get; set; }

        /// <summary>
        /// Segment Identifier
        /// </summary>
        public String? SegmentId { get; set; }

        /// <summary>
        /// Request / CorrelationId passed from the calling client context when enqueued
        /// </summary>
        public String? CorrelationId { get; set; }

        /// <summary>
        /// Request / CorrelationId passed from the calling client context when enqueued
        /// </summary>
        public String? FlowInstanceId { get; set; }

        /// <summary>
        /// Staging Data Identifier
        /// </summary>
        public String StagingDataIdentifier { get; set; }
    }

    public class PipeResolver : IPipeResolver
    {
        private readonly IApplicationConnectionSettings _applicationConnectionSettings;
        private readonly ILogger<PipeResolver> _logger;

        public PipeResolver(IApplicationConnectionSettings applicationConnectionSettings, ILogger<PipeResolver> logger)
        {
            _applicationConnectionSettings = applicationConnectionSettings;
            _logger = logger;
        }

        public string GetServiceConnectionString(string componentName, int ouId)
        {
            var res = _applicationConnectionSettings.ServiceDataConnectionAsync(componentName, ouId);
            var connectString =  res.Result.ConnectionString;
            //append Trusted_Connection=False; to the connection string
            connectString = connectString + ";TrustServerCertificate=True;";
           _logger.LogInformation("@@@@ Connection String for {0} Ou {1} is {2}", componentName, ouId, connectString);
            return connectString;
        }
    }
}

