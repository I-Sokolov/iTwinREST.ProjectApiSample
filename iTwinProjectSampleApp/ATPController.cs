using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace iTwinProjectSampleApp
    {
    internal class ATPController
        {
        class Test
            {
            public Task<string> TaskDownload;

            public Models.iModel iModel;

            public int Num;

            public Test(Models.iModel imodel, int num) 
                {
                iModel = imodel;
                Num = num;
                }
            };

        private iModelsManager _iModelsMan = new iModelsManager();

        private List<Test> _tests = new List<Test>();

        private string _bimFolder = "E:\\Downloads";

        public async Task Run()
            {
            //
            await _iModelsMan.Login();

            // get imodels list
            var iModels = await _iModelsMan.GetiModels();
            int num = 1;
            foreach (var imodel in iModels)
                {
                _tests.Add(new Test(imodel, num++));
                }

            //start downloading task for 1st test, than next downoading will be started upon previous downloading finish
            StartNextDownloadingTask();
             
            // process tests cycle
            foreach (var test in _tests)
                {
                await ExecuteTest (test);
                }
            }

        private async Task<string> DownLoadAsync(Test test)
            {
            Console.WriteLine($"Downloading #{test.Num} start");

            //download bim file from the latest named version
            var namedVers = await _iModelsMan.GetLatestNamedVersion(test.iModel.id);
            if (namedVers == null)
                {
                StartNextDownloadingTask();
                return "";
                //throw new ApplicationException($"iModel {test.iModel.displayName} does not have named version.(Test is executing on the latest named version)");
                }

            var chkpt = await _iModelsMan.GetNamedVersionCheckpoint(test.iModel.id, namedVers.id);
            
            var href = chkpt?._links?.download?.href;
            if (href == null)
                {
                throw new ApplicationException("No download href for checkpoint for named version {namedVers.displayName} (changeset {namedVers.changesetIndex})");
                }

            var bimFilePath = System.IO.Path.Combine(_bimFolder, $"{test.iModel.displayName}.{namedVers.displayName}.bim");

            if (test.iModel.displayName == "IFC_ATP.IFC2x3_CV2_certification.Space_01A")
                {
                _iModelsMan.DownloadFile(href, bimFilePath);
                }

            StartNextDownloadingTask();

            Console.WriteLine($"Downloading #{test.Num} end");

            return bimFilePath;
            }

        private void StartNextDownloadingTask()
            {
            foreach (var t in _tests)
                {
                if (t.TaskDownload == null)
                    {
                    t.TaskDownload = DownLoadAsync(t);
                    break;
                    }
                }
            }

        private async Task ExecuteTest(Test test)
            {
            Console.WriteLine($"Execute #{test.Num} start");

            Trace.Assert(test.TaskDownload != null);

            var bimFilePath = await test.TaskDownload;
            Console.WriteLine($"Execute #{test.Num} wait for download finished");

            //do test
            if (test.Num == 5 || test.Num == 15)
                {
                await Task.Delay(TimeSpan.FromSeconds(20));
                }

            //clean
            if (System.IO.File.Exists (bimFilePath))
                {
                System.IO.File.Delete (bimFilePath);
                }

            Console.WriteLine($"Execute #{test.Num} finish");
            }
        }
    }
