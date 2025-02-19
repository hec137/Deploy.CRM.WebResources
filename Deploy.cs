using DeployWeb.Helpers;
using DeployWeb.Models;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Sharprompt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;

namespace DeployWeb.Properties
{
    public static class Deploy
    {
        public static void DeployWebRes(string path, List<string> filters, ConnectionHelper connectionHelper) {
            var filter = String.Join("", filters.Select(f => $"<condition attribute='name' operator='like' value='%{f}%' />"));
            var connections = connectionHelper.GetConnections();

            var options = new SelectOptions<Connection>();
            options.Items = connections;
            options.Message = "Select Connection";
            options.DefaultValue = connections.FirstOrDefault();

            var connection = Prompt.Select(options);

            var service = connection.GetService();

            var fileNames = getFileNames(path, filters);
            if (fileNames == null) { Console.WriteLine($"No files found in folder: {path}"); return; }
            var files = fileNames.Select(f => { return pathToInfo(path, f); }).ToList();

            UpdateFiles(service, files, filter);
        }

        static private List<string> getFileNames(string path, List<string> filters)
        {
            var names = new List<string>();

            if (!Directory.Exists(path))
            {
                return null;
            }

            var subNmaes = Directory.GetFiles(path);

            names.AddRange(subNmaes.Where(n => filters.Any(f => n.Replace("\\","/").Contains(f))));
            var directories = Directory.GetDirectories(path);
            if (directories.Any())
            {
                foreach (var dir in directories)
                {
                    names.AddRange(getFileNames(dir, filters));
                }
            }

            return names;
        }

        private static WebFileInfo pathToInfo(string path, string name)
        {
            var file = new WebFileInfo();
            file.path = name;
            file.name = name.Replace("\\", "/").Substring(path.Length + 1);

            var type = name.Split('.').Last();
            var finalType = new OptionSetValue();

            switch (type)
            {
                case "html":
                    finalType = new OptionSetValue(1);
                    break;
                case "css":
                    finalType = new OptionSetValue(2);
                    break;
                case "js":
                    finalType = new OptionSetValue(3);
                    break;
                case "xml":
                    finalType = new OptionSetValue(4);
                    break;
                case "png":
                    finalType = new OptionSetValue(5);
                    break;
                case "jpg":
                    finalType = new OptionSetValue(6);
                    break;
                case "gif":
                    finalType = new OptionSetValue(7);
                    break;
                case "xap":
                    finalType = new OptionSetValue(8);
                    break;
                case "xsl":
                    finalType = new OptionSetValue(9);
                    break;
                case "svg":
                    finalType = new OptionSetValue(11);
                    break;
                default:
                    Console.WriteLine($"Unknown Type: {type}");
                    return null;
            }

            file.type = finalType;
            return file;
        }

        private static void UpdateFiles(ServiceClient service, List<WebFileInfo> files, string filter)
        {
            var webResources = GetWebResources(service, filter);

            var filesToUpdate = new List<WebFileInfo>();
            var filesToCreate = new List<WebFileInfo>();
            var skiped = 0;

            var publishGuids = new List<Guid>();

            foreach (var file in files)
            {
                Console.Write(file.name);
                if (webResources.Any(web => web["displayname"].ToString().Equals(file.name)))
                {
                    var webVersion = webResources?.First(web => web["displayname"].ToString().Equals(file.name));
                    if (compareFiles(webVersion, file))
                    {
                        file.id = webVersion.Id;
                        filesToUpdate.Add(file);
                        publishGuids.Add(webVersion.Id);
                        Console.WriteLine(" will be updated...");
                    }
                    else
                    {
                        Console.WriteLine(" will be skiped...");
                        publishGuids.Add(webVersion.Id);
                        skiped++;
                    }
                }
                else
                {
                    filesToCreate.Add(file);
                    Console.WriteLine(" will be created...");
                }
            }

            Console.WriteLine($"{filesToCreate.Count} filse will be created");
            Console.WriteLine($"{filesToUpdate.Count} files wiil be updated");
            Console.WriteLine($"{skiped} files will be skiped");

            Console.WriteLine("Confirm to publish [Y/n]: ");
            var response = Console.ReadLine();
            if (response.ToLower() == "n") { return; }

            foreach (var file in filesToCreate)
            {
                var guid = create(service, file);
                publishGuids.Add(guid);
            }

            foreach (var file in filesToUpdate)
            {
                update(service, file);
            }

            Console.WriteLine("publishing...");
            publish(service, publishGuids);
            Console.WriteLine("published");
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

        private static bool compareFiles(Entity web, WebFileInfo local)
        {
            byte[] fileArray = System.IO.File.ReadAllBytes(local.path);
            string base64FileRepresentation = Convert.ToBase64String(fileArray);

            if (!web.Contains("content"))
            {
                return true;
            }

            if (web["content"].ToString().Length != base64FileRepresentation.Length)
            {
                return true;
            }

            return web["content"].ToString() != base64FileRepresentation;
        }

        private static Guid create(IOrganizationService service, WebFileInfo local)
        {
            byte[] fileArray = System.IO.File.ReadAllBytes(local.path);
            string base64FileRepresentation = Convert.ToBase64String(fileArray);

            var webResource = new Entity("webresource");
            webResource["webresourcetype"] = local.type;
            webResource["name"] = local.name;
            webResource["displayname"] = local.name;
            webResource["content"] = base64FileRepresentation;

            return service.Create(webResource);
        }

        private static void update(IOrganizationService service, WebFileInfo local)
        {
            var guid = local.id;
            byte[] fileArray = System.IO.File.ReadAllBytes(local.path);
            string base64FileRepresentation = Convert.ToBase64String(fileArray);

            var webResource = new Entity("webresource", guid);
            webResource["content"] = base64FileRepresentation;

            service.Update(webResource);
        }

        private static void publish(ServiceClient service, List<Guid> guids)
        {
            if (guids.Count == 0) { return; }
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {service.CurrentAccessToken}");

            var url = $"https://{service.ConnectedOrgUriActual.Host}/api/data/v9.2/PublishXml";

            var list = "";
            foreach (var guid in guids)
            {
                list += $"<webresource>{{{guid}}}</webresource>";
            }
            var xml = $"<importexportxml><webresources>{list}</webresources></importexportxml>";

            var body = new Dictionary<string, string>();
            body.Add("ParameterXml", xml);

            var res = client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")).Result;
            var v = res.Content.ReadAsStringAsync().Result;
            if (res.StatusCode != System.Net.HttpStatusCode.OK && res.StatusCode != System.Net.HttpStatusCode.NoContent) { Console.WriteLine($"Error: {res.StatusCode.ToString()}-{v}"); }
        }
    }
}
