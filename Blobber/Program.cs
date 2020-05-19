using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Blobber
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var files = GetFiles(args);
            var blobServiceClient = GetBlobService();
            var containerName = GetContainerName();

            BlobContainerClient containerClient;
            if (!blobServiceClient.GetBlobContainers().Any(c => c.Name == containerName))
            {
                containerClient = blobServiceClient.CreateBlobContainer(containerName, PublicAccessType.Blob).Value;
            }
            else
            {
                containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            }

            var links = new Dictionary<string, string>();
            foreach (var file in files)
            {
                // Get a reference to a blob
                var blobClient = containerClient.GetBlobClient(Path.GetFileName(file));

                Console.WriteLine($"Uploading '{file}' to Blob storage");
                links.Add(file, blobClient.Uri.ToString());

                // Open the file and upload its data
                using (FileStream uploadFileStream = File.OpenRead(file))
                {
                    var result = blobClient.Upload(uploadFileStream, true).Value;
                    Console.WriteLine("Done!");
                }
            }

            Console.WriteLine("All uploads complete:\n");
            foreach (var kvp in links)
            {
                Console.WriteLine($"{Path.GetFileName(kvp.Key)}: {kvp.Value}");
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        private const string ConVar = "AZURE_STORAGE_CONNECTION_STRING";

        private static BlobServiceClient GetBlobService()
        {
            var conn = Environment.GetEnvironmentVariable(ConVar);

            if (string.IsNullOrWhiteSpace(conn))
            {
                Console.Write("Enter a azure storage account connection string (NOTE: This will get sved as an environment variable!):");
                var input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    var testClient = new BlobServiceClient(input);
                    Console.WriteLine($"Test: {testClient.AccountName}");
                    Console.WriteLine("Connection valid!");
                    conn = input;
                    Environment.SetEnvironmentVariable(ConVar, conn);
                }
                else
                {
                    throw new Exception("No connection string given!");
                }
            }
            var blobServiceClient = new BlobServiceClient(conn);
            return blobServiceClient;
        }

        private static string GetContainerName()
        {
            var containerName = "blobber-" + Environment.UserName.ToLower();
            Console.Write($"Enter container name (default: '{containerName}'): ");
            var inputName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(inputName))
            {
                containerName = inputName;
            }

            return containerName;
        }

        private static List<string> GetFiles(string[] args)
        {
            if (args.Length > 0)
            {
                return args.ToList();
            }

            var fileDialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = "All files (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            return fileDialog.FileNames.ToList();
        }
    }
}