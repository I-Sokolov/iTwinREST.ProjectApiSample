using ItwinProjectSampleApp.Models;
using ItwinProjectSampleApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTwinProjectSampleApp.Models;
using System.Net;
using System.Reflection.PortableExecutable;

namespace iTwinProjectSampleApp
    {
    /// <summary>
    /// Implements iModels API https://developer.bentley.com/apis/imodels-v2/operations/
    /// </summary>
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
            var header = new Dictionary<string, string>();
            header.Add("Prefer", "return=minimal");

            var responseMsg = await _endpointMgr.MakeGetCall<iModel>($"/imodels/?iTwinId={_projectId}", header);
            if (responseMsg.Status != HttpStatusCode.OK)
                throw new Exception($"{responseMsg.Status}: {responseMsg.ErrorDetails?.Code} - {responseMsg.ErrorDetails?.Message}");

            Console.WriteLine($" [Retrieved {responseMsg.Instances?.Count} iModels] (SUCCESS)");

            return responseMsg.Instances;

            }
        
        public async Task<changeset>  GetLatestChangeset(string imodelId)
            {
            var header = new Dictionary<string, string>();
            header.Add("Prefer", "return=representation");

            var request = $"/imodels/{imodelId}/changesets?$orderBy=index desc&$top=1";

            var responseMsg = await _endpointMgr.MakeGetCall<changeset>(request, header);
            if (responseMsg.Status != HttpStatusCode.OK)
                throw new Exception($"{responseMsg.Status}: {responseMsg.ErrorDetails?.Code} - {responseMsg.ErrorDetails?.Message}");

            Console.WriteLine($" [Retrieved {responseMsg.Instances?.Count} iModels] (SUCCESS)");

            return responseMsg.Instances?.FirstOrDefault();
            }

        public async Task<checkpoint> GetCheckpoint(string hrefCheckpoint)
            {
            var responseMsg = await _endpointMgr.MakeGetSingleCall<checkpoint>(hrefCheckpoint);
            if (responseMsg.Status != HttpStatusCode.OK)
                throw new Exception($"{responseMsg.Status}: {responseMsg.ErrorDetails?.Code} - {responseMsg.ErrorDetails?.Message}");

            return responseMsg.Instance;
            }

        public async Task<changeset> GetChangeset(string imodelId, string changesetId)
            {
            var request = $"/imodels/{imodelId}/changesets/{changesetId}";

            var responseMsg = await _endpointMgr.MakeGetSingleCall<changeset>(request);
            if (responseMsg.Status != HttpStatusCode.OK)
                throw new Exception($"{responseMsg.Status}: {responseMsg.ErrorDetails?.Code} - {responseMsg.ErrorDetails?.Message}");

            return responseMsg.Instance;
            }

        public async Task<namedVersion> GetLatestNamedVersion(string imodelId)
            {
            var header = new Dictionary<string, string>();
            header.Add("Prefer", "return=minimal");

            var request = $"/imodels/{imodelId}/namedversions?$orderBy=changesetIndex desc&$top=1";

            var responseMsg = await _endpointMgr.MakeGetCall<namedVersion>(request, header);
            if (responseMsg.Status != HttpStatusCode.OK)
                throw new Exception($"{responseMsg.Status}: {responseMsg.ErrorDetails?.Code} - {responseMsg.ErrorDetails?.Message}");

            Console.WriteLine($" [Retrieved {responseMsg.Instances?.Count} iModels] (SUCCESS)");

            return responseMsg.Instances?.FirstOrDefault();
            }

        public async Task<checkpoint> GetNamedVersionCheckpoint (string imodelId, string namedVersionId)
            {
            var request = $"/imodels/{imodelId}/namedversions/{namedVersionId}/checkpoint";

            var responseMsg = await _endpointMgr.MakeGetSingleCall<checkpoint>(request);
            if (responseMsg.Status != HttpStatusCode.OK)
                throw new Exception($"{responseMsg.Status}: {responseMsg.ErrorDetails?.Code} - {responseMsg.ErrorDetails?.Message}");

            return responseMsg.Instance;
            }
        }
    }
