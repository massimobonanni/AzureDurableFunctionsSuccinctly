using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DurableFunctions.FanOutFanIn
{
    public class FanOutFanInFunctions
    {
        [FunctionName("Backup_Client")]
        public async Task<HttpResponseMessage> Client(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "funoutfanin/backup")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogInformation($"[CLIENT Backup_Client] --> Backup started!");

            string jsonContent = await req.Content.ReadAsStringAsync();
            try
            {
                var backupRequest = JsonConvert.DeserializeObject<BackupRequest>(jsonContent);
                if (backupRequest.IsValid())
                {
                    var instanceId = await starter.StartNewAsync("Backup_Orchestrator", backupRequest);

                    log.LogInformation($"Backup started - started orchestration with ID = '{instanceId}'.");

                    return starter.CreateCheckStatusResponse(req, instanceId);
                }
            }
            catch (Exception ex)
            {
                log.LogError("Error during backup operation", ex);
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }

        [FunctionName("Backup_Orchestrator")]
        public async Task<BackupReport> Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log.LogInformation($"[ORCHESTRATOR Backup_Orchestrator] --> id : {context.InstanceId}");

            var backupRequest = context.GetInput<BackupRequest>();
            var report = new BackupReport();

            var files = await context.CallActivityAsync<string[]>("Backup_GetFileList", backupRequest.path);
            report.NumberOfFiles = files.Length;

            var tasks = new Task<long>[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                tasks[i] = context.CallActivityAsync<long>("Backup_CopyFileToBlob", files[i]);
            }

            await Task.WhenAll(tasks);

            report.TotalBytes = tasks.Sum(t => t.Result);

            return report;
        }

        [FunctionName("Backup_GetFileList")]
        public string[] GetFileList([ActivityTrigger] string rootPath,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY Backup_GetFileList] --> rootPath : {rootPath}");
            string[] files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
            return files;
        }

        [FunctionName("Backup_CopyFileToBlob")]
        [StorageAccount("StorageAccount")]
        public async Task<long> CopyFileToBlob([ActivityTrigger] string filePath,
            IBinder binder,
            ILogger log)
        {
            log.LogInformation($"[ACTIVITY Backup_CopyFileToBlob] --> filePath : {filePath}");
            long byteCount = new FileInfo(filePath).Length;

            string blobPath = filePath
                .Substring(Path.GetPathRoot(filePath).Length)
                .Replace('\\', '/');
            string outputLocation = $"backups/{blobPath}";

            using (Stream source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Stream destination = await binder.BindAsync<CloudBlobStream>(
                new BlobAttribute(outputLocation, FileAccess.Write)))
            {
                await source.CopyToAsync(destination);
            }

            return byteCount;
        }

    }


}