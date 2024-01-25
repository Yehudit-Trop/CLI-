using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
//c#-.cs  c++-.cpp,.h python-.py java-.java
static void CopyCodeFiles(FileInfo folderPath, string[] programmingLanguages, SortOptions sort, bool removeEmptyLines, bool includeSourceNote, string fileCreator)
{
    var languageExtensions = new Dictionary<string, List<string>>()
        {
            { "java", new List<string>() { ".java" } },
            { "c#", new List<string>() { ".cs" } },
            { "c++", new List<string>() { ".cpp", ".h" } },
            { "c", new List<string>() { ".c", ".h" } },
            { "python", new List<string>() { ".py" } }
        };
    try
    {
        string[] codeFileNames = Directory.GetFiles(folderPath.DirectoryName, "*", SearchOption.AllDirectories)
            .Where(f => IsFileExtensionMatch(f, programmingLanguages, languageExtensions))
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.vs\\") &&
            !f.Contains("\\Debug\\") && !f.Contains("\\Properties\\"))
            .ToArray();

        if (sort == SortOptions.type)
        {
            Array.Sort(codeFileNames, CompareByType);
        }
        else
        {
            Array.Sort(codeFileNames, CompareByName);
        }

        string outputPath = Path.Combine(folderPath.DirectoryName, folderPath.FullName);

        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            if (fileCreator != null)
            {
                writer.WriteLine($"----File Creator: {fileCreator}----");
                writer.WriteLine();
            }
            foreach (string codeFile in codeFileNames)
            {
                string fileContent = File.ReadAllText(codeFile);
                if (removeEmptyLines)
                {
                    fileContent = RemoveEmptyLines(fileContent);
                }
                if (includeSourceNote)
                {
                    string sourceNote = $"#### Source: {GetRelativePath(codeFile, folderPath.DirectoryName)} ####";
                    writer.WriteLine(sourceNote);
                }
                writer.WriteLine(fileContent);
                writer.WriteLine(new string('-', 50)); // Add a separation line
            }
        }
    }
    catch (Exception ex) { Console.WriteLine("Error while creating the file"); }

}
static bool IsFileExtensionMatch(string filePath, string[] programmingLanguages, Dictionary<string, List<string>> languageExtensions)
{
    string fileExtension = Path.GetExtension(filePath);
    if (programmingLanguages.Contains("all", StringComparer.OrdinalIgnoreCase))
    {
        foreach (var languageExtension in languageExtensions.Values.SelectMany(e => e))
        {
            if (string.Equals(languageExtension, fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
    }
    else
    {
        foreach (string language in programmingLanguages)
        {
            if (languageExtensions.ContainsKey(language) && languageExtensions[language].Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }
    }

    return false;
}
static int CompareByName(string x, string y)
{
    return string.Compare(Path.GetFileName(x), Path.GetFileName(y));
}
static int CompareByType(string x, string y)
{
    string xExtension = Path.GetExtension(x).ToLower();
    string yExtension = Path.GetExtension(y).ToLower();

    return string.Compare(xExtension, yExtension);
}
static string RemoveEmptyLines(string code)
{
    return string.Join(Environment.NewLine, code.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
}
static string GetRelativePath(string fullPath, string basePath)
{
    Uri fullPathUri = new Uri(fullPath);
    Uri baseUri = new Uri(basePath);
    return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullPathUri).ToString());
}

//options
var outputOption = new Option<FileInfo>("--output", "file path and name");
outputOption.AddAlias("--o");

var languageOption = new Option<string[]>("--language", "Languages of files to add")
{ IsRequired = true, AllowMultipleArgumentsPerToken = true }.FromAmong("c#", "java", "python", "c++", "all");
languageOption.AddAlias("--lng");

var noteOption = new Option<bool>("--note", "add code source note");
noteOption.AddAlias("--nt");

var sortOption = new Option<SortOptions>(
           name: "--sort",
           description: "sort the code files by file-name or code-kind",
           getDefaultValue: () => SortOptions.name);
sortOption.AddAlias("--srt");

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines");
removeEmptyLinesOption.AddAlias("--rmvel");

var authorOption = new Option<string>("--author", "add author name to file");
authorOption.AddAlias("--athr");

//commands
var createRspCommand = new Command("create-rsp", "create response file");

var bundleCommand = new Command("bundle", "bundle code file")
{
    outputOption,languageOption,noteOption,authorOption,sortOption,removeEmptyLinesOption
};

bundleCommand.SetHandler((output, lang, sort, note, removeEL, author) =>
{
    CopyCodeFiles(output, lang, sort, removeEL, note, author);

}, outputOption, languageOption, sortOption, noteOption, removeEmptyLinesOption, authorOption);

createRspCommand.SetHandler(() =>
{
    Console.WriteLine("Welcome to the create-rsp command!");
    Console.WriteLine("Please provide the desired values for each command option:");

    Console.Write("Output file path and name: ");
    string outputFile = Console.ReadLine();

    Console.Write("File path and name for --output option: ");
    string outputFilePath = Console.ReadLine();

    Console.Write("Languages for --language option (separated by space): ");
    string languagesInput = Console.ReadLine();
    string[] languages = languagesInput.Split(' ');

    Console.Write("Add code source note (--note option)? (Y/N): ");
    bool addNote = Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase);

    Console.Write("Sort code files by file-name or code-kind (--sort option)? (name/type): ");
    string sortOptionInput = Console.ReadLine();
    SortOptions sortOption = sortOptionInput.Equals("type", StringComparison.OrdinalIgnoreCase)
        ? SortOptions.type
        : SortOptions.name;

    Console.Write("Remove empty lines (--remove-empty-lines option)? (Y/N): ");
    bool removeEmptyLines = Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase);

    Console.Write("Author name (--author option): ");
    string authorName = Console.ReadLine();

    string rspContent =$"--output {outputFilePath}{Environment.NewLine}" +
                       $"--language {string.Join(" ", languages)}{Environment.NewLine}" +
                       $"--note {addNote}{Environment.NewLine}" +
                       $"--sort {sortOption}{Environment.NewLine}" +
                       $"--remove-empty-lines {removeEmptyLines}{Environment.NewLine}" +
                       $"--author {authorName}";
    string rspFilePath = $@"{Environment.CurrentDirectory}\{outputFile}.rsp";
    File.WriteAllText(rspFilePath, rspContent);

    Console.WriteLine($"Response file '{rspFilePath}' has been created successfully!");
});

var rootCommand = new RootCommand("root command");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);

enum SortOptions
{
    type,
    name
}
