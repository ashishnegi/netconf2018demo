// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace VotingWeb.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Fabric.Query;
    using System.Linq;
    using System.Threading.Tasks;

    public class HomeController : Controller
    {
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;
        private readonly IStatelessServicePartition partition;

        public HomeController(StatelessServiceContext serviceContext, FabricClient fabricClient, IStatelessServicePartition partition)
        {
            this.fabricClient = fabricClient;
            this.serviceContext = serviceContext;
            this.partition = partition;
        }

        public async Task<IActionResult> Index(string poll)
        {
            Uri serviceName = VotingWeb.GetVotingDataServiceName(this.serviceContext, poll);

            ServiceList serviceList =
                await this.fabricClient.QueryManager.GetServiceListAsync(
                    new Uri(this.serviceContext.CodePackageActivationContext.ApplicationName),
                    serviceName);

            if (!serviceList.Any() && HttpContext.Request.Method.ToLower() == "post")
            {
                await this.fabricClient.ServiceManager.CreateServiceAsync(
                    new StatefulServiceDescription()
                    {
                        ApplicationName = new Uri(this.serviceContext.CodePackageActivationContext.ApplicationName),
                        HasPersistedState = true,
                        MinReplicaSetSize = 3,
                        TargetReplicaSetSize = 3,
                        ServiceName = serviceName,
                        ServiceTypeName = "VotingDataType",
                        ServicePackageActivationMode = ServicePackageActivationMode.ExclusiveProcess,
                        PartitionSchemeDescription = new UniformInt64RangePartitionSchemeDescription(3, 0, 25)
                    });

                // Get current number of service
                ServiceList serviceListforLoad =
                await this.fabricClient.QueryManager.GetServiceListAsync(
                    new Uri(this.serviceContext.CodePackageActivationContext.ApplicationName));

                int numberOfDataServices =
                    serviceListforLoad.Where<Service>(s => s.ServiceTypeName == "VotingDataType").Count();

                // Get number of frontend instances
                var numberOfReplicas =
                    await this.fabricClient.QueryManager.GetReplicaListAsync(partition.PartitionInfo.Id);

                // Calculate average number of backends per frontend
                int newAverageLoad = numberOfDataServices / numberOfReplicas.Count;

                // Report new load with additional service
                partition.ReportLoad(new List<LoadMetric> { new LoadMetric("polls", newAverageLoad) });

            }

            this.ViewData["Poll"] = serviceName;

            return this.View();
        }

        public IActionResult Error()
        {
            return this.View();
        }
    }
}