using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net;

class Program
{
    private static readonly HttpClient client;
    private const string API_URL = "http://recruitment.warpdevelopment.co.za/api/authenticate"; // Changed back to HTTP
    private static int attemptCount = 0;
    private const int MAX_ATTEMPTS = 100;
    
    // Static constructor to configure HttpClient
    static Program()
    {
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
        };
        
        client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(30);
        
        // Print the .NET version for debugging
        Console.WriteLine($"Running on .NET version: {Environment.Version}");
    }
    
    static async Task Main()
    {
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            
            string username = "Prince";
            string dictionaryFile = "dict.txt";
            string zipFile = "submission.zip";

            Console.WriteLine("Starting password dictionary generation...");
            await GenerateAndShowSamplePasswords();

            Console.WriteLine("\nStarting authentication attempts...");
            string? uploadUrl = await BruteForcePasswordAsync(username, dictionaryFile);
            
            if (uploadUrl == null)
            {
                Console.WriteLine("Authentication failed after maximum attempts.");
                return;
            }

            Console.WriteLine($"Authentication successful! Upload URL: {uploadUrl}");
            await CreateZipAsync(zipFile);
            await SubmitZipAsync(uploadUrl, zipFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    static async Task GenerateAndShowSamplePasswords()
    {
        var variations = new List<string>
        {
            "password",
            "Password",
            "p@ssword",
            "P@ssword",
            "passw0rd",
            "Passw0rd",
            "p@ssw0rd",
            "P@ssw0rd",
            "admin",
            "Admin",
            "admin123",
            "Admin123",
            "test",
            "Test",
            "test123",
            "Test123"
        };
        
        Console.WriteLine("Generated passwords sample:");
        foreach (var password in variations.Take(5))
        {
            Console.WriteLine(password);
        }
        
        await File.WriteAllLinesAsync("dict.txt", variations);
        Console.WriteLine($"Total passwords generated: {variations.Count}");
    }

    static async Task<string?> BruteForcePasswordAsync(string username, string dictionaryFile)
    {
        if (!File.Exists(dictionaryFile))
        {
            throw new FileNotFoundException($"Dictionary file not found: {dictionaryFile}");
        }

        var passwords = await File.ReadAllLinesAsync(dictionaryFile);
        
        foreach (var password in passwords)
        {
            if (attemptCount >= MAX_ATTEMPTS)
            {
                Console.WriteLine("Reached maximum number of attempts.");
                return null;
            }

            attemptCount++;
            try
            {
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                Console.WriteLine($"Attempt {attemptCount}: Trying password: {password}");
                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                using var response = await client.GetAsync(API_URL);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Success! Password found: {password}");
                    return responseContent;
                }
                
                await Task.Delay(500);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error on attempt {attemptCount}:");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                await Task.Delay(1000);
            }
        }
        
        return null;
    }

    static async Task CreateZipAsync(string zipFile)
    {
        string[] requiredFiles = { "CV.pdf", "dict.txt", "Program.cs" };
        
        foreach (var file in requiredFiles)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"Required file not found: {file}");
            }
        }

        using var zipToCreate = new FileStream(zipFile, FileMode.Create);
        using var archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create);
            
        foreach (var file in requiredFiles)
        {
            archive.CreateEntryFromFile(file, file);
            Console.WriteLine($"Added {file} to zip");
        }
    }

    static async Task SubmitZipAsync(string uploadUrl, string zipFile)
    {
        if (!File.Exists(zipFile))
        {
            throw new FileNotFoundException($"ZIP file not found: {zipFile}");
        }

        byte[] zipBytes = await File.ReadAllBytesAsync(zipFile);
        string base64Zip = Convert.ToBase64String(zipBytes);

        var payload = new
        {
            Data = base64Zip,
            Name = "Prince Ngwako",
            Surname = "Mashumu",
            Email = "princengwakomashumu@gmail.com"
        };

        string json = JsonConvert.SerializeObject(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine("Submitting ZIP file...");
        using var response = await client.PostAsync(uploadUrl, content);
        string responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Submit response: {responseContent}");
    }
}