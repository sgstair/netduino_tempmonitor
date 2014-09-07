using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace DataReceiverService
{
    class WebServiceMgr
    {
        public WebServiceMgr()
        {


        }

        ServiceHost host;
        TemperatureDataReceiver serviceInstance;

        public void Start(bool debug = false)
        {
            if(serviceInstance != null)
            {
                throw new Exception("Don't support using Start as a Restart operation.");
            }

            serviceInstance = new TemperatureDataReceiver();

            Uri[] serviceUri = new Uri[1] { new Uri("http://localhost:2542/TemperatureData") };
            host = new ServiceHost(serviceInstance, serviceUri);

            // For setup and testing/debug, enable a Metadata exchange binding.
            if(debug)
            {

                // Check to see if the service host already has a ServiceMetadataBehavior
                ServiceMetadataBehavior smb = host.Description.Behaviors.Find<ServiceMetadataBehavior>();
                // If not, add one
                if (smb == null)
                {
                    smb = new ServiceMetadataBehavior();
                    host.Description.Behaviors.Add(smb);
                }
                smb.HttpGetEnabled = true;
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;

                // Add MEX endpoint
                host.AddServiceEndpoint(
                  ServiceMetadataBehavior.MexContractName,
                  MetadataExchangeBindings.CreateMexHttpBinding(),
                  "mex"
                );


                // Check to see if the service has a ServiceDebugBehavior
                ServiceDebugBehavior sdb = host.Description.Behaviors.Find<ServiceDebugBehavior>();
                // If not, add one
                if (sdb == null)
                {
                    sdb = new ServiceDebugBehavior();
                    host.Description.Behaviors.Add(sdb);
                }

                sdb.IncludeExceptionDetailInFaults = true;

            }



            // create primary binding.

            // WS-HTTP Binding (SOAP) - Could plausibly use this, but using the Web / http binding for now.
            //BasicHttpBinding binding = new BasicHttpBinding();
            //host.AddServiceEndpoint(typeof(ITemperatureDataReceiver), binding, "");

            // Web binding (REST)
            WebHttpBinding webBinding = new WebHttpBinding();
            
            ServiceEndpoint endpoint = host.AddServiceEndpoint(typeof(ITemperatureDataReceiver), webBinding, "");

            WebHttpBehavior whb = new WebHttpBehavior();

            if (debug)
            {
                whb.HelpEnabled = true;
                whb.FaultExceptionEnabled = true;
            }

            endpoint.EndpointBehaviors.Add(whb);

            host.Open();
        }

        public void Stop()
        {
            host.Close();
            host = null;
            serviceInstance = null;
        }

    }
}
