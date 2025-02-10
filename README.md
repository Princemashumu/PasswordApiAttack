# 🔐 Password Cracker and File Submission

## 📝 Overview
This is a C# console application that performs a brute-force attack on an API endpoint to discover the correct password for a given username. Once the correct password is found, the program creates a ZIP file containing specified documents and submits it to the provided upload URL.

## ⭐ Features
- 🔑 Generates a dictionary of password variations.
- 🚀 Attempts authentication using a brute-force approach.
- 📂 Creates a ZIP archive containing required files.
- 📤 Submits the ZIP file to an API endpoint.
- 🔄 Implements error handling and logging for failed attempts.

## 🚀 How to Run
1. Ensure you have .NET installed on your system.
2. Copy the `Program.cs` file into a new C# Console Application project.
3. Place the required files (`CV.pdf`, `dict.txt`, `Program.cs`) in the project directory.
4. Open a terminal or command prompt and navigate to the project directory.
5. Compile and run the program using the following commands:
   ```sh
   dotnet run
   ```
6. Follow the on-screen instructions as the program executes.

## 🛠 Functionality Breakdown
1. **Dictionary Generation**
   - Generates variations of the base password using common substitutions.
   - Saves the generated passwords to `dict.txt`.

2. **Brute Force Password Attempt**
   - Reads passwords from `dict.txt`.
   - Uses HTTP Basic Authentication to attempt login.
   - Displays debug information for each attempt.
   - Delays between requests to prevent server flooding.
   - If successful, retrieves the upload URL.

3. **ZIP File Creation**
   - Ensures all required files exist.
   - Compresses the files into `submission.zip`.

4. **File Submission**
   - Converts the ZIP file to a base64-encoded string.
   - Sends a POST request with name, surname, email, and file data.
   - Displays success or failure message based on response.

## 📌 Example Output
```
Generating password dictionary...
Generated 100 password variations
Starting authentication attempts...
Attempt 1:
Testing username: John
Testing password: password
Base64 credentials: am9objpwYXNzd29yZA==
Status Code: 401 Unauthorized
...
Success! Password found: P@ssw0rd123
Creating ZIP file...
Added CV.pdf to zip
Added dict.txt to zip
Added Program.cs to zip
Submitting files...
Successfully submitted files!
```

## 📜 License
This project is open-source and can be used or modified freely.

## 👨‍💻 Author
Developed by Prince Ngwako Mashumu

