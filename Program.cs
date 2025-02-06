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
    private static readonly HttpClient client = new HttpClient(); // Reuse HttpClient instance
    private const string API_URL = "http://recruitment.warpdevelopment.co.za/api/authenticate";
    
    static async Task Main()
    {
        try
        {
            string username = "Prince";
            string dictionaryFile = "dict.txt";
            string zipFile = "submission.zip";

            // Step 1: Generate dictionary file
            await GenerateDictionaryAsync(dictionaryFile);

            // Step 2: Try passwords from dictionary
            string uploadUrl = await BruteForcePasswordAsync(username, dictionaryFile);
            if (string.IsNullOrEmpty(uploadUrl))
            {
                Console.WriteLine("Failed to authenticate.");
                return;
            }

            // Step 3: Create ZIP file
            await CreateZipAsync(zipFile);

            // Step 4: Encode ZIP and submit
            await SubmitZipAsync(uploadUrl, zipFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }

    static async Task GenerateDictionaryAsync(string filePath)
    {
        try
        {
            string basePassword = "password";
            var variations = new List<string>();

            void Permute(string current, int index)
            {
                if (index == basePassword.Length)
                {
                    variations.Add(current);
                    return;
                }

                char[] options = basePassword[index] switch
                {
                    'a' => new[] { 'a', '@' },
                    's' => new[] { 's', '5' },
                    'o' => new[] { 'o', '0' },
                    _ => new[] { basePassword[index], char.ToUpper(basePassword[index]) }
                };

                foreach (var option in options)
                {
                    Permute(current + option, index + 1);
                }
            }

            Permute("", 0);
            await File.WriteAllLinesAsync(filePath, variations);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error writing dictionary file: {ex.Message}");
            throw;
        }
    }

    static async Task<string> BruteForcePasswordAsync(string username, string dictionaryFile)
    {
        if (!File.Exists(dictionaryFile))
        {
            throw new FileNotFoundException($"Dictionary file not found: {dictionaryFile}");
        }

        foreach (var password in await File.ReadAllLinesAsync(dictionaryFile))
        {
            try
            {
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                using var response = await client.GetAsync(API_URL);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Success! Password found: {password}");
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error: {ex.Message}");
                continue;
            }

            await Task.Delay(100); // Prevent flooding the server
        }
        
        return null;
    }

    static async Task CreateZipAsync(string zipFile)
    {
        try
        {
            // Check if required files exist
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
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating ZIP file: {ex.Message}");
            throw;
        }
    }

    static async Task SubmitZipAsync(string uploadUrl, string zipFile)
    {
        try
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

            using var response = await client.PostAsync(uploadUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Upload failed with status code: {response.StatusCode}. Response: {responseContent}");
            }
            
            Console.WriteLine($"Upload successful. Response: {responseContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting ZIP file: {ex.Message}");
            throw;
        }
    }
}