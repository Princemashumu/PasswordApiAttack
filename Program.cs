using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Collections.Generic;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string API_URL = "http://recruitment.warpdevelopment.co.za/api/authenticate";
    
    static async Task Main()
    {
        try
        {
            string username = "John"; // Corrected username
            string dictionaryFile = "dict.txt";
            string zipFile = "submission.zip";

            // Step 1: Generate dictionary file
            Console.WriteLine("Generating password dictionary...");
            GenerateDictionary(dictionaryFile);

            // Step 2: Try passwords from dictionary
            Console.WriteLine("Starting authentication attempts...");
            string? uploadUrl = await BruteForcePasswordAsync(username, dictionaryFile);
            
            if (uploadUrl == null)
            {
                Console.WriteLine("Failed to find correct password.");
                return;
            }

            // Step 3: Create ZIP file
            Console.WriteLine("Creating ZIP file...");
            CreateZip(zipFile);

            // Step 4: Submit ZIP
            Console.WriteLine("Submitting files...");
            await SubmitZip(uploadUrl, zipFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void GenerateDictionary(string filePath)
    {
        var variations = new List<string>();
        string basePassword = "password";
        
        void GenerateVariations(string current, int index)
        {
            if (index == basePassword.Length)
            {
                variations.Add(current);
                return;
            }

            // Get possible characters for current position
            char[] options = basePassword[index] switch
            {
                'a' => new[] { 'a', 'A', '@' },
                's' => new[] { 's', 'S', '5' },
                'o' => new[] { 'o', 'O', '0' },
                _ => new[] { char.ToLower(basePassword[index]), char.ToUpper(basePassword[index]) }
            };

            foreach (char c in options)
            {
                GenerateVariations(current + c, index + 1);
            }
        }

        GenerateVariations("", 0);
        File.WriteAllLines(filePath, variations);
        Console.WriteLine($"Generated {variations.Count} password variations");
    }

  static async Task<string?> BruteForcePasswordAsync(string username, string dictionaryFile)
{
    if (!File.Exists(dictionaryFile))
    {
        Console.WriteLine("Dictionary file not found!");
        return null;
    }

    int attemptCount = 0;
    foreach (string password in File.ReadLines(dictionaryFile))
    {
        attemptCount++;
        try
        {
            string credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{username}:{password}")
            );
            
            // Debug output
            Console.WriteLine($"\nAttempt {attemptCount}:");
            Console.WriteLine($"Testing username: {username}");
            Console.WriteLine($"Testing password: {password}");
            Console.WriteLine($"Base64 credentials: {credentials}");
            
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", credentials);

            using var response = await client.GetAsync(API_URL);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Debug output
            Console.WriteLine($"Status Code: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"Response Content: {responseContent}");
                
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Success! Password found: {password}");
                return responseContent;
            }
                
            await Task.Delay(100); // Prevent flooding the server
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"\nNetwork error on attempt {attemptCount}:");
            Console.WriteLine($"Password being tried: {password}");
            Console.WriteLine($"Error Message: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            await Task.Delay(1000); // Longer delay on error
        }
    }
        
    return null;
}
    static void CreateZip(string zipFile)
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

    static async Task SubmitZip(string uploadUrl, string zipFile)
    {
        byte[] zipBytes = File.ReadAllBytes(zipFile);
        
        // Check file size (5MB limit)
        if (zipBytes.Length > 5 * 1024 * 1024)
        {
            throw new Exception("ZIP file exceeds 5MB limit");
        }

        string base64Zip = Convert.ToBase64String(zipBytes);

        var payload = new
        {
            Data = base64Zip,
            Name = "Prince Ngwako",
            Surname = "Mashumu",
            Email = "email@domain.com"
        };

        string json = JsonConvert.SerializeObject(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(uploadUrl, content);
        string responseContent = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Successfully submitted files!");
        }
        else
        {
            Console.WriteLine($"Submission failed: {responseContent}");
        }
    }
}