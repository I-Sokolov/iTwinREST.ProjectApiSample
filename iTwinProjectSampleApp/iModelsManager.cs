using ItwinProjectSampleApp.Models;
using ItwinProjectSampleApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTwinProjectSampleApp.Models;
using System.Net;

namespace iTwinProjectSampleApp
    {
    internal class iModelsManager
        {
        private EndpointManager _endpointMgr;

        private const string _projectId = "f42bfd6f-4514-4923-9908-9cad44b4ffba"; //IfcBridge

        internal iModelsManager()
            {
            _endpointMgr = new EndpointManager();
            }

        public async Task<bool> Login()
            {
            return await _endpointMgr.Login();
            }

        public async Task<List<iModel>> GetiModels()
            {
            var header = new Dictionary<string, string>
                {
                    { "Prefer", "return=minimal" }
                };

            var responseMsg = await _endpointMgr.MakeGetCall<iModel>($"/imodels/?iTwinId={_projectId}", header);
            if (responseMsg.Status != HttpStatusCode.OK)
                throw new Exception($"{responseMsg.Status}: {responseMsg.ErrorDetails?.Code} - {responseMsg.ErrorDetails?.Message}");

            Console.WriteLine($" [Retrieved {responseMsg.Instances?.Count} iModels] (SUCCESS)");

            return responseMsg.Instances;

            }
        
        }
    }
