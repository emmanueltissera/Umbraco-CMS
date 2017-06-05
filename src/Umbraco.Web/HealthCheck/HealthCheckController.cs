using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using Umbraco.Core.Configuration.HealthChecks;
using Umbraco.Web.Editors;

namespace Umbraco.Web.HealthCheck
{
    /// <summary>
    /// The API controller used to display the health check info and execute any actions
    /// </summary>
    public class HealthCheckController : UmbracoAuthorizedJsonController
    {
        private readonly IHealthCheckResolver _healthCheckResolver;
        private readonly IList<Guid> _disabledCheckIds;

        public HealthCheckController()
        {
            _healthCheckResolver = HealthCheckResolver.Current;

            var healthCheckConfig = (HealthChecksSection)ConfigurationManager.GetSection("umbracoConfiguration/HealthChecks");
            _disabledCheckIds = healthCheckConfig.DisabledChecks
                .Select(x => x.Id)
                .ToList();
        }

        public HealthCheckController(IHealthCheckResolver healthCheckResolver)
        {
            _healthCheckResolver = healthCheckResolver;
        }

        /// <summary>
        /// Gets a grouped list of health checks, but doesn't actively check the status of each health check.
        /// </summary>
        /// <returns>Returns a collection of anonymous objects representing each group.</returns>
        public object GetAllHealthChecks()
        {
            var groups = _healthCheckResolver.HealthChecks
                .Where(x => _disabledCheckIds.Contains(x.Id) == false)
                .GroupBy(x => x.Group)
                .OrderBy(x => x.Key);
            var healthCheckGroups = new List<HealthCheckGroup>();
            foreach (var healthCheckGroup in groups)
            {
                var hcGroup = new HealthCheckGroup
                {
                    Name = healthCheckGroup.Key,
                    Checks = healthCheckGroup
                        .OrderBy(x => x.Name)
                        .ToList()
                };
                healthCheckGroups.Add(hcGroup);
            }

            return healthCheckGroups;
        }

        [HttpGet]
        public object GetStatus(Guid id)
        {
            var check = GetCheckById(id);
            
            try
            {
                //Core.Logging.LogHelper.Debug<HealthCheckController>("Running health check: " + check.Name);
                return check.GetStatus();
            }
            catch (Exception e)
            {
                Core.Logging.LogHelper.Error<HealthCheckController>("Exception in health check: " + check.Name, e);
                throw;
            }
        }

        [HttpPost]
        public HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {
            var check = GetCheckById(action.HealthCheckId);
            return check.ExecuteAction(action);
        }

        private HealthCheck GetCheckById(Guid id)
        {
            var check = _healthCheckResolver.HealthChecks
                .Where(x => _disabledCheckIds.Contains(x.Id) == false)
                .FirstOrDefault(x => x.Id == id);

            if (check == null) throw new InvalidOperationException(string.Format("No health check found with id {0}", id));

            return check;
        }
    }
}