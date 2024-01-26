/*--------------------------------------------------------------------------------------+
|
| Copyright (c) Bentley Systems, Incorporated. All rights reserved.
| See LICENSE.md in the project root for license terms and full copyright notice.
|
+--------------------------------------------------------------------------------------*/

using iTwinProjectSampleApp;
using iTwinProjectSampleApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ItwinProjectSampleApp
    {
    class Program
        {
        static async Task Main (string[] args)          
            {
            DisplayMainIndex();

            // Retrieve the _token using the TryIt button. https://developer.bentley.com/api-groups/administration/apis/projects/operations/create-project/
            //Console.WriteLine("\n\nCopy and paste the Authorization header from the 'Try It' sample in the APIM front-end:  ");
            //string authorizationHeader = Console.ReadLine();
            //Console.Clear();
            //DisplayMainIndex();

            //await using var projectMgr = new ProjectManager();
            //await projectMgr.Login();
            var iModelsMan = new iModelsManager();
            var res = await iModelsMan.Login();
            Trace.Assert(res);

            var iModels = await iModelsMan.GetiModels();
            foreach (var imodel in iModels) 
                { 
                Console.WriteLine($"iModel: {imodel.displayName}\n");

                if (imodel.displayName.StartsWith("IFC_ATP"))
                    {
                    ///
                    var chset = await iModelsMan.GetLatestChangeset(imodel.id);
                    if (chset!=null)
                        {
                        Console.WriteLine($"Changeset #{chset.index} id {chset.id}");
                        Console.WriteLine($"    download: {chset._links?.download?.href}");
                        Console.WriteLine($"    checkpoint: {chset._links?.currentOrPrecedingCheckpoint?.href}");

                        var chkptLink = chset._links.currentOrPrecedingCheckpoint?.href;
                        if (chkptLink != null)
                            {
                            var chkpt = await iModelsMan.GetCheckpoint(chkptLink);
                            var namedChset = await iModelsMan.GetChangeset(imodel.id, chkpt.changesetId);
                            }
                        }

                    ///
                    namedVersion namedVers = await iModelsMan.GetLatestNamedVersion(imodel.id);
                    if (namedVers != null)
                        {
                        Console.WriteLine($"Named version {namedVers.id} : {namedVers.displayName} changeset {namedVers.changesetIndex}");
                        var chkpt = await iModelsMan.GetNamedVersionCheckpoint(imodel.id, namedVers.id);
                        var href = chkpt?._links?.download?.href;
                        iModelsMan.DownloadFile(href, "E:\\downloads\\a.bim");
                        Console.WriteLine(href);
                        }

                    break;
                    }
                }

            // Execute Project workflow. This will create/update/query an iTwin project
            //await projectMgr.ProjectManagementWorkflow();

            // Execute User Management workflow. This will create an iTwin project, create a project role and add a user to the project
            // with that role. The user must be a valid Bentley user so we we get it from the _token to be sure. You can change this to another user.
            //var projectUserEmail = RetrieveEmailFromAuthHeader(authorizationHeader); 
            //await projectMgr.ProjectUserManagementWorkflow(projectUserEmail);
            }

        #region Private Methods
        private static void DisplayMainIndex()
            {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
            Console.WriteLine("*****************************************************************************************");
            Console.WriteLine("*           iTwin Platform Sample App                                                   *");
            Console.WriteLine("*****************************************************************************************\n");
            }

        private static string RetrieveEmailFromAuthHeader(string authorizationHeader)
            {
            var jwt = authorizationHeader?.Split(" ")[1]?.Trim();
            if (string.IsNullOrWhiteSpace(jwt))
                throw new ApplicationException("The jwt _token is incorrect.  Ensure that 'Bearer ' precedes the _token in the header.");
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var email = token.Claims?.FirstOrDefault(x => x.Type.Equals("Email", StringComparison.OrdinalIgnoreCase))?.Value;
            return email;
            }
        #endregion
    }
    }
