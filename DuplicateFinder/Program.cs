using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace DuplicateFinder
{
    public static class Program
    {
        private static IConfigurationRoot configuration;

        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the File Duplicate Finder");

            // Create service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            MainProcess();
        }

        private static void MainProcess()
        {
            List<Models.File> SourceFiles = new List<Models.File>();

            Console.WriteLine("Started Source Directory");

            if (!string.IsNullOrEmpty(configuration.GetSection("SourceJson").Value))
            {
                Console.WriteLine("Found Source Json, skipping read of Directory");
                try
                {
                    string json = System.IO.File.ReadAllText(configuration.GetSection("SourceJson").Value);
                    SourceFiles = JsonConvert.DeserializeObject<List<Models.File>>(json);
                }
                catch (Exception ex)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error while reading Source JSON:");
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
            else
            {
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
            }

            if (string.IsNullOrEmpty(configuration.GetSection("SourceJson").Value))
            {
                string jsonSource = JsonConvert.SerializeObject(SourceFiles, Formatting.Indented);
                System.IO.File.WriteAllText(System.IO.Directory.GetParent(AppContext.BaseDirectory).FullName + @"\source.json", jsonSource);
            }

            Console.WriteLine("Finished Source Directory");
            Console.WriteLine("Started Target Directory");

            List<Models.File> DestinationFiles = new List<Models.File>();
            DirectoryInfo directoryInfoTarget = new DirectoryInfo(configuration.GetSection("DestinationPath").Value);

            foreach (var item in directoryInfoTarget.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    if (item.DirectoryName.ToLowerInvariant().Contains(configuration.GetSection("SourcePath").Value.ToLowerInvariant()))
                    {
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"Info: Skipping File, as it is in the SourceDirectory {item.FullName}");
                        continue;
                    }

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

            string jsonDestination = JsonConvert.SerializeObject(DestinationFiles, Formatting.Indented);
            System.IO.File.WriteAllText(System.IO.Directory.GetParent(AppContext.BaseDirectory).FullName + @"\destination.json", jsonDestination);

            Compare(SourceFiles, DestinationFiles);


            Console.WriteLine("Finished, press any key to exit!");
            Console.ReadLine();
        }

        private static void Compare(List<Models.File> Source, List<Models.File> Destination)
        {
            List<Models.FileMatch> fileMatches = new List<Models.FileMatch>();

            foreach (var item in Destination)
            {
                foreach (var itemDest in Source)
                {
                    if (item.Name.ToLowerInvariant() == "thumbs.db")
                    {
                        continue;
                    }

                    Models.FileMatch fileMatch = new Models.FileMatch
                    {
                        TypesMatched = new List<string>()
                    };

                    if (item.CreateDate == itemDest.CreateDate)
                    {
                        fileMatch.Match = true;
                        fileMatch.TypesMatched.Add("CreateDate");
                    }

                    if (item.Hash == itemDest.Hash)
                    {
                        if (item.Size != 0)
                        {
                            fileMatch.Match = true;
                        }
                        
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

            string json = JsonConvert.SerializeObject(fileMatches, Formatting.Indented);
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
