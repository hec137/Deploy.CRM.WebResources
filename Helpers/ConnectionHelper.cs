using DeployWeb.Models;
using Microsoft.PowerPlatform.Dataverse.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace DeployWeb.Helpers
{
    public class ConnectionHelper
    {
        private string connectionsPath;
        private List<Connection> connections;

        public ConnectionHelper(string basePath, string operationName) 
        {
            var connectionFilesPath = basePath + "\\connections.json";

            if (!File.Exists(connectionFilesPath))
            {
                File.Create(connectionFilesPath).Close();
            }

            connectionsPath = connectionFilesPath;
            GetConnections();

            if (connections.Count == 0 && operationName != "create") {
                Console.WriteLine("You don't have any connections created in this project pleas first create a connection");
                CreateConnection();
                GetConnections();
            }
        }

        public void CreateConnection() 
        {
            Console.WriteLine("Connection name: ");
            var name = Console.ReadLine();
            Console.WriteLine("Connection url (example: https://env.crm4.dynamics.com/): ");
            var url = Console.ReadLine();
            Console.WriteLine("Client Id: ");
            var clientId = Console.ReadLine();
            Console.WriteLine("Client Secret: ");
            var clientSecret = Console.ReadLine();

            var connectionString = $"AuthType=ClientSecret;Url={url};ClientId={clientId};ClientSecret={clientSecret}";
            try
            {
                var service = new ServiceClient(connectionString).Connect();
            }
            catch(Exception e) 
            { 
                Console.WriteLine(e.Message);
                CreateConnection();
                return;
            }

            var guid = Guid.NewGuid();
            var lastUse = DateTime.Now;

            var connection = new Connection();
            connection.guid = guid;
            connection.name = name;
            connection.clientId = clientId; 
            connection.clientSecret = clientSecret;
            connection.url = url;

            connections.Add(connection);

            var connectionsString = JsonConvert.SerializeObject(connections);
            var sw = new StreamWriter(this.connectionsPath);
            sw.Write(connectionsString);
            sw.Close();
        }

        public List<Connection> GetConnections() { 
            var sr = new StreamReader(this.connectionsPath);
            var json = sr.ReadToEnd();
            sr.Close();

            var unOrderedCon = JsonConvert.DeserializeObject<List<Connection>>(json);
            if (unOrderedCon == null)
            {
                this.connections = new List<Connection>();
            }
            else
            {
                this.connections = unOrderedCon.OrderBy(c => c.name).ToList();
            }
            return this.connections;
        }

        public void UpdateConnection(Connection connection)
        {
            var updateGuid = connection.guid;
            var connections = GetConnections();
            connections.RemoveAll(c => c.guid == updateGuid);

            CreateConnection();
        }

        public void DeleteConnection(Connection connection)
        {
            var updateGuid = connection.guid;
            var connections = GetConnections();
            connections.RemoveAll(c => c.guid == updateGuid);

            var connectionsString = JsonConvert.SerializeObject(connections);
            var sw = new StreamWriter(this.connectionsPath);
            sw.Write(connectionsString);
            sw.Close();
        }
    }
}
