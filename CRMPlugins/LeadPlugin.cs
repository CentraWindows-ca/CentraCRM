using System;
using Microsoft.Xrm.Sdk;

namespace CentraCRM.CRMPlugins
{
    public class LeadPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)

        {
            //Extract the tracing service for use in debugging sandboxed plug-ins,
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            
            try
            {
                tracingService.Trace("LeadPlugin: i'm here.");
            }
            catch (Exception ex)
            {
                tracingService.Trace("LeadPlugin: Exception.");
                throw;
            }

            tracingService.Trace("LeadPlugin: TEST TEST i'm here.");
            // Obtain the execution context from the service provider,
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                Entity targetEntity = (Entity)context.InputParameters["Target"];

                if (targetEntity.LogicalName != "lead")
                    return;

                //
                Entity preImage = null;
                Entity postImage = null;

                if (context.PreEntityImages.Contains("PreImage"))
                {
                    preImage = context.PreEntityImages["PreImage"];
                    // Use preImage as needed
                }
                if (context.PostEntityImages.Contains("PostImage"))
                {
                    postImage = context.PostEntityImages["PostImage"];
                    // Use postImage as needed
                }

                // check Description field
                if (targetEntity.Contains("description"))
                {
                    tracingService.Trace("LeadPlugin: Target has description.");

                    if ((string)targetEntity["description"] == "1234")  // use it to trigger the action we'ere going to take
                    {
                        tracingService.Trace("LeadPlugin: description says 1234.");
                    }
                }
            }
        }
    }
}
