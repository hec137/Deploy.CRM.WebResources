using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerPlatform.Dataverse.Client;
using DeployWeb.Helpers;
using DeployWeb.Models;
using Sharprompt;
using System.IO;

namespace DeployWeb
{
    public static class Download
    {
        public static void DownloadWebRes(ConnectionHelper connectionHelper, string path, List<string> filters1, List<string> filters2) 
        {
            var filters = filters2.Count > 0 ? filters2 : filters1;
            var filter = String.Join("", filters.Select(f => $"<condition attribute='name' operator='like' value='%{f}%' />"));
            var connections = connectionHelper.GetConnections();

            var options = new SelectOptions<Connection>();
            options.Items = connections;
            options.Message = "Select Connection";
            options.DefaultValue = connections.FirstOrDefault();

            var connection = Prompt.Select(options);

            var service = connection.GetService();

            var webRes = GetWebResources(service, filter);
            CreateFiles(webRes.ToList(), path);
        }

        static private DataCollection<Entity> GetWebResources(ServiceClient service, string filter)
        {
            var fetchXml = $@"<fetch>
	            <entity name='webresource'>
		            <attribute name='webresourceid' />
		            <attribute name='content' />
		            <attribute name='displayname' />
		            <attribute name='name' />
		            <attribute name='webresourcetype' />
		            <attribute name='webresourcetypename' />
		            <filter type='and'>
                        <condition attribute='displayname' operator='not-null' />
                        <filter type='or'>
			                {filter}
                        </filter>
		            </filter>
	            </entity>
            </fetch>";

            var webResources = service.RetrieveMultiple(new FetchExpression(fetchXml)).Entities;
            return webResources;
        }

        static private void CreateFiles(List<Entity> webRes, string path) {
            foreach (var res in webRes) {
                var filePath = res["displayname"].ToString();
                Console.WriteLine(filePath);
            }

            Console.WriteLine("Confirm to download [Y/n]: ");
            var response = Console.ReadLine();
            if (response.ToLower() == "n") { return; }

            foreach (var res in webRes) { 
                var filePath = res["displayname"].ToString();
                var depth = filePath.Split('/').Length;
                var folderPath = path + "/" + string.Join("/", filePath.Split('/').Take(depth - 1));
                var fileName = filePath.Split('/').LastOrDefault();

                var fullPath = folderPath + "/" + fileName;
                var content = res["content"].ToString();
                var bytes = Convert.FromBase64String(content);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                File.WriteAllBytes(fullPath, bytes);
                Console.WriteLine($"Created {fileName}");
            }
            Console.WriteLine("Done");
        }
    }
}
