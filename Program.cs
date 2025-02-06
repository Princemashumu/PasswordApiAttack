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
    static async Task Main()
    {
        string username = "Prince";
        string dictionaryFile = "dict.txt";
        string zipFile = "submission.zip";

        // Step 1: Generate dictionary file
        GenerateDictionary(dictionaryFile);

        // Step 2: Try passwords from dictionary
        string uploadUrl = await BruteForcePassword(username, dictionaryFile);
        if (string.IsNullOrEmpty(uploadUrl))
        {
            Console.WriteLine("Failed to authenticate.");
            return;
        }

        // Step 3: Create ZIP file
        CreateZip(zipFile);

        // Step 4: Encode ZIP and submit
        await SubmitZip(uploadUrl, zipFile);
    }

    static void GenerateDictionary(string filePath)
    {
        string basePassword = "password";
        List<string> variations = new List<string>();

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
        File.WriteAllLines(filePath, variations);
    }

    static async Task<string> BruteForcePassword(string username, string dictionaryFile)
{
    using HttpClient client = new HttpClient();
    
    foreach (var password in File.ReadLines(dictionaryFile))
    {
        try
        {
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            HttpResponseMessage response = await client.GetAsync("http://recruitment.warpdevelopment.co.za/api/authenticate");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Success! Password found: {password}");
                return await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        await Task.Delay(100); // Prevent flooding the server
    }
    
    return null;
}


    static void CreateZip(string zipFile)
    {
        using FileStream zipToCreate = new FileStream(zipFile, FileMode.Create);
        using ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create);
        archive.CreateEntryFromFile("CV.pdf", "CV.pdf");
        archive.CreateEntryFromFile("dict.txt", "dict.txt");
        archive.CreateEntryFromFile("Program.cs", "Program.cs");
    }

    static async Task SubmitZip(string uploadUrl, string zipFile)
    {
        byte[] zipBytes = File.ReadAllBytes(zipFile);
        string base64Zip = Convert.ToBase64String(zipBytes);

        var payload = new
        {
            Data = base64Zip,
            Name = "Prince Ngwako",
            Surname = "Mashumu",
            Email = "princengwakomashumu@gmail.com"
        };

        string json = JsonConvert.SerializeObject(payload);
        using HttpClient client = new HttpClient();
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(uploadUrl, content);
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}
