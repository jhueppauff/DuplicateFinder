using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DuplicateFinder
{
    public static class Program
    {
        private static IConfigurationRoot configuration;

        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the File Converter");

            // Create service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            MainProcess();
        }

        private static void MainProcess()
        {
            List<Models.File> SourceFiles = new List<Models.File>();

            DirectoryInfo directoryInfoSource = new DirectoryInfo(configuration.GetSection("SourcePath").Value);

            foreach (var item in directoryInfoSource.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    Models.File file = new Models.File
                    {
                        CreateDate = item.CreationTime,
                        ModfiedDate = item.LastWriteTime,
                        Name = item.Name,
                        Path = item.FullName,
                        Size = item.Length,
                        Hash = GetFileHash(item.FullName)
                    };

                    SourceFiles.Add(file);
                }

                catch (Exception ex)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                }   
            }

            string jsonSource = JsonConvert.SerializeObject(SourceFiles);
            System.IO.File.WriteAllText(System.IO.Directory.GetParent(AppContext.BaseDirectory).FullName + @"\source.json", jsonSource);

            List<Models.File> DestinationFiles = new List<Models.File>();
            DirectoryInfo directoryInfoTarget = new DirectoryInfo(configuration.GetSection("DestinationPath").Value);

            foreach (var item in directoryInfoTarget.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    Models.File file = new Models.File
                    {
                        CreateDate = item.CreationTime,
                        ModfiedDate = item.LastWriteTime,
                        Name = item.Name,
                        Path = item.FullName,
                        Size = item.Length,
                        Hash = GetFileHash(item.FullName)
                    };

                    DestinationFiles.Add(file);
                }

                catch (Exception ex)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                }
            }

            string jsonDestination = JsonConvert.SerializeObject(DestinationFiles);
            System.IO.File.WriteAllText(System.IO.Directory.GetParent(AppContext.BaseDirectory).FullName + @"\destination.json", jsonDestination);

            Compare(SourceFiles, DestinationFiles);

            Console.ReadLine();
        }

        private static void Compare(List<Models.File> Source, List<Models.File> Destination)
        {
            List<Models.FileMatch> fileMatches = new List<Models.FileMatch>();

            foreach (var item in Destination)
            {
                foreach (var itemDest in Source)
                {
                    Models.FileMatch fileMatch = new Models.FileMatch();
                    fileMatch.TypesMatched = new List<string>();

                    if (item.CreateDate == itemDest.CreateDate)
                    {
                        fileMatch.Match = true;
                        fileMatch.TypesMatched.Add("CreateDate");
                    }

                    if (item.Hash == itemDest.Hash)
                    {
                        fileMatch.Match = true;
                        fileMatch.TypesMatched.Add("Hash");
                    }

                    if (item.Name == itemDest.Name)
                    {
                        fileMatch.Match = true;
                        fileMatch.TypesMatched.Add("Name");
                    }

                    if (item.ModfiedDate == itemDest.ModfiedDate)
                    {
                        fileMatch.Match = true;
                        fileMatch.TypesMatched.Add("ModfiedDate");
                    }

                    if (item.Size == itemDest.Size)
                    {
                        fileMatch.Match = true;
                        fileMatch.TypesMatched.Add("Size");
                    }

                    if (fileMatch.Match)
                    {
                        fileMatch.SourceFile = item;
                        fileMatch.DestinationFile = itemDest;
                        fileMatches.Add(fileMatch);
                    }
                }
            }

            string json = JsonConvert.SerializeObject(fileMatches);
            System.IO.File.WriteAllText(System.IO.Directory.GetParent(AppContext.BaseDirectory).FullName + @"\matches.json", json);
        }



        private static string GetFileHash(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(new LoggerFactory()
              .AddConsole());
            serviceCollection.AddLogging();

            configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false).Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
        }
    }
}
