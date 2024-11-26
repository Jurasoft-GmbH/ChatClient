using ChatClient;

#pragma warning disable

class Program
{
    static string version_string = "24.11.26.1";

    // This progam is a tool for analyzing .NET code using Roslyn and the code-server's AI models.
    // It can be used to analyze code in a solution or a single code file.
    // It can be used to check code for errors and provide suggestions for improvement.
    // It can also be used to check code for malware.
    // It checks on a classed-based model by default, but can be switched to a function-based model.
    // The class-based model anylizes the code at the class/module level, while the function-based model anylizes the code at the function level.
    // see prompting.cs to edit the chatbot prompts, or set them at runtime in the code below

    // Usage:
    // dotnet run [solutionName] | [filePath | projectPath] [options]
    // filePath: Path to a code file to analyze in adhoc workspace (optional, instead of a solutionName)
    // projectPath: Path to a project file (.vbproj, .cs.proj) to analyze in ad-hoc workspace (optional, use this if solutionName fails to load)
    // IMPORTANT: The solutionName must be the name of a folder in the 'slnfolder' base directory

    // Example:
    // dotnet run MySolution -malware -noeval -func             will do a malware check and use the function-based model without code analysis
    // dotnet run MySolution                                    will do a code analysis using the class-based model
    // dotnet run C:\path\to\file.cs                            will analyze the specified code file using an adhoc solution 
    // dotnet run                                               will analyze the built-in example code using an adhoc solution

    // Options:
    // -codeonly: run analysis on code-only w/o compiler issues, e.g. for prompting malware checks, default is code-with-issues  
    // -nodefault: Disable default code-with-issues analysis (you should enable the -codeonly check if you use this option)
    // -d, -detail: Enable detailed prompt useage (default: concise)
    // -de or -lang=de: Use German language DE prompts (default: English)
    // -model=<modelName>: Specify the model to use (default: "qwen2.5-coder:32b", alternatives: "mixtral:8x7b", "phi3:medium-128k", or "codegemma")
    //                     you can specify more than one model by separating them with a comma or semicolon, e.g. -model="qwen2.5-coder:32b,phi3:medium-128k"
    //                     you can also specify a gemini or open ai model by using the -openai_key or -google_key options
    // -openai_key=<key>:  Specify an API-Key for openai gpt chat completion models
    // -google_key=<key>:  Specify an API-Key for google gemini chat completion models
    // -func, -function: Use function-based model (default: class-based model, function-based model is more detailed but slower)
    // -promptfile=<path>: specify a file to use for chatbot prompts (default: prompting.cs)
    // -ignore=<path>: specify a file to use for ignoring issues (default: none)
    // -slnfolder=<path>: specify the base folder for the solution (default: c:\user_home_folder\source\repos\)
    // -sample: write the default prompts to "prompts.txt" in the current directory and exit
    // -v, -verbose: provides additional console output
    // -unlock: bypass the lock file check (only jura code server) (use with caution)

    // If the user is not able to run the program, it may be because the AI is locked by another user.
    // The program will check for a lock file and display an error message if the AI is locked.

    static string lockFile = @"\\code-server\daten\ai-lock.txt";
    static string baseFolder = "";
    static bool bypassLock = false;

    static bool checkForLockFile() {
        if (File.Exists(lockFile))
        {
            return true;
        }
        return false;
    }

    static string getLockFileContent() {
        if (File.Exists(lockFile))
        {
            return File.ReadAllText(lockFile);
        }
        return "";
    }

    static void setLockFileContent() {
        var content = "locked by " + Environment.UserName + " on " + Environment.MachineName + " at " + DateTime.Now.ToString();
        File.WriteAllText(lockFile, content);
    }

    static void removeLockFile() {
        if (File.Exists(lockFile))
        {
            if (!bypassLock) File.Delete(lockFile);
        }
    }

    // let's provide a handler if the user presses Ctrl+C so we can remove the lock file
    static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        removeLockFile();
    }

    static void Main(string[] args)
    {
        var userBaseFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        baseFolder = Path.Combine(userBaseFolder, "samba_shared_folder/repos/");
        if (!System.IO.Directory.Exists(baseFolder)) {
            Console.WriteLine("base not found:" + baseFolder);
            baseFolder = Path.Combine(userBaseFolder, "source/repos/");
            Console.WriteLine("set base:" + baseFolder);
        }
        bool showHelp = false;
        foreach (var argument in args)
        {
            var arg = argument.ToLower().Trim();
            if (arg.StartsWith("--")) arg = arg.Substring(1);
            if (arg == "-h" || arg == "-help" || arg == "/?" || arg == "-?" || arg == "/h" || arg == "/help")
            {
                showHelp = true;
                break;
            }
            if (arg.StartsWith("-slnfolder="))
            {
                baseFolder = arg.Replace("-slnfolder=", "").Replace("'", "").Replace("\"", "").Trim(); 
                continue;
            }
            if (arg == "-unlock" || arg.Contains("-openai_key") || arg.Contains("-google_key"))
            {
                bypassLock = true;
                break;
            }
        }
        Console.WriteLine("----------------------------------");
        Console.WriteLine("Jurasoft AI-Assisted Code Analysis");
        if (showHelp)
        {
            Console.WriteLine();
            print_help_to_console();
            return;
        }
        Console.WriteLine($"- v. {version_string}, checking lock...");
        if (bypassLock) {
            Console.WriteLine("- bypassing lock check");
        } else 
        {
            if (checkForLockFile())
            {
                Console.WriteLine("Error: AI is locked by another user: " + getLockFileContent());
                Console.WriteLine("Please try again later.");
                return;
            }
        }
        Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
        setLockFileContent();
        Console.WriteLine("- AI locked by: " + Environment.UserName);
        var solutionName = "";
        var open_code_file = "";
        var open_project_file = "";
        bool doMalwareCheck = false;
        bool doCodeAnalysis = true;
        bool useFunction = false;
        bool verbose = false;
        string Claude_ApiKey = "";
        string OpenAI_ApiKey = "";
        string Gemini_ApiKey = "";
        string promptFile = "";
        string ignoreIssuesFile = "";
        Prompts.Detail preferredDetail = Prompts.Detail.Concise;
        Prompts.Language preferredLanguage = Prompts.Language.EN;
        string[] requestedModels = new string[] { "qwen2.5-coder:32b" };
        bool writeSample = false;
        foreach (var argument in args)
        {
            var arg = argument.ToLower().Trim();
            if (arg.StartsWith("--")) arg = arg.Substring(1);
            if (arg == "-sample")
            {
                writeSample = true;
                continue;
            }
            if (arg.Contains("-promptfile="))
            {
                promptFile = argument.Replace("-promptfile=", "").Replace("'", "").Replace("\"", "").Trim();
                continue;
            }
            if (arg.Contains("-ignore=")) 
            {
                ignoreIssuesFile = argument.Replace("-ignore=", "").Replace("'", "").Replace("\"", "").Trim();
                continue;
            }
            if (arg.Contains("-openai_key="))
            {
                OpenAI_ApiKey = argument.Replace("-openai_key=", "").Replace("'", "").Replace("\"", "").Trim();
                continue;
            }
            if (arg.Contains("-google_key="))
            {
                Gemini_ApiKey = argument.Replace("-google_key=", "").Replace("'", "").Replace("\"", "").Trim();
                continue;
            }
            if (arg.Contains("-claude_key="))
            {
                Claude_ApiKey = argument.Replace("-claude_key=", "").Replace("'", "").Replace("\"", "").Trim();
                continue;
            }
            if (arg.Contains("-malware") || arg == "-codeonly") 
            {
                doMalwareCheck = true;
                continue;
            }
            if (arg.Contains("-verbose") || arg == "-v")
            {
                verbose = true;
                continue;
            }
            if (arg.Contains("-detail") || arg == "-d")
            {
                preferredDetail = Prompts.Detail.Detailed;
                continue;
            }
            if (arg.Contains("-de") || arg.Contains("-lang=de"))
            {
                preferredLanguage = Prompts.Language.DE;
                continue;
            }
            if (arg.Contains("-model=")) 
            {
                if (arg.Contains(",") || arg.Contains(";")) {
                    if (arg.Contains(",")) requestedModels = argument.Replace("-model=", "").Replace("'", "").Replace("\"", "").Split(',');
                    if (arg.Contains(";")) requestedModels = argument.Replace("-model=", "").Replace("'", "").Replace("\"", "").Split(';');
                }
                else {
                    requestedModels = new string[] { argument.Replace("-model=", "").Replace("'", "").Replace("\"", "").Trim() };
                }
                continue;
            }
            if (arg.Contains("-nodefault")) 
            {
                doCodeAnalysis = false;
                continue;
            }
            if (arg.Contains("-func") || arg.Contains("-function")) 
            {
                useFunction = true;
                continue;
            }
            if (System.IO.File.Exists(arg))
            {
                if (arg.EndsWith(".csproj") || arg.EndsWith(".vbproj"))
                    open_project_file = argument;
                else
                    open_code_file = argument;
                continue;
            }
            if (string.IsNullOrEmpty(solutionName))
            {
                solutionName = argument;
            }
        }
        if (writeSample)
        {
            try
            {
                Prompts.SavePromptStringsToFile("prompts.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing prompts.txt file: " + ex.Message);
            }
            Console.WriteLine("----------------------------------");
            return;
        }
        if (doCodeAnalysis) Console.WriteLine("- code w/issues analysis enabled"); else Console.WriteLine("- code w/issues analysis disabled");
        if (doMalwareCheck) Console.WriteLine("- code-only check enabled"); else Console.WriteLine("- code-only check disabled");
        Console.WriteLine("----------------------------------");
        Console.WriteLine("base:" + baseFolder);

        var solutionFolder = Path.Combine(baseFolder, solutionName);

        if (!string.IsNullOrEmpty(open_project_file))
        {
            solutionFolder = Path.GetDirectoryName(open_project_file);
            baseFolder = solutionFolder;
            solutionName = open_project_file;
        }
        if (!Directory.Exists(solutionFolder))
        {
            Console.WriteLine("Error: solution folder not found: " + solutionFolder);
            removeLockFile();
            return;
        }

        var logfilename = Path.Combine(solutionFolder, "ai-log.txt");
        if (verbose) Console.WriteLine("initializing log file - " + logfilename);
        if (File.Exists(logfilename))
        {
            File.Delete(logfilename);
        }
        // print date and time to the log file
        System.IO.File.AppendAllText(logfilename, DateTime.Now.ToString() + "\n\n");

        ChatBotInference chatBot = new ChatBotInference();
        chatBot.Verbose = verbose;
        chatBot.LanguageCode = preferredLanguage;
        chatBot.DetailLevel = preferredDetail;
        chatBot.OpenAIApiKey = OpenAI_ApiKey;
        chatBot.GeminiApiKey = Gemini_ApiKey;
        chatBot.ClaudeApiKey = Claude_ApiKey;
        if (!string.IsNullOrEmpty(promptFile))
        {
            if (File.Exists(promptFile))
            {
                try
                {
                    Prompts.LoadPromptStringsFromFile(promptFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading prompt file: " + ex.Message);
                }
            }
        }
        // this is the event that will be triggered when the chatbot returns a result, we can use this to log the results
        chatBot.ResultEvent += (sender, result) =>
        {
            var message = result.Item1;
            var code_prompt = result.Item2;
            if (verbose) Console.WriteLine("ChatBot: " + message);
            Console.WriteLine($"logging to file {logfilename}...");
            System.IO.File.AppendAllText(logfilename, "------------------------------------------------------------\n");
            if (verbose) {
                System.IO.File.AppendAllText(logfilename, code_prompt + "\n");
                System.IO.File.AppendAllText(logfilename, "------------------------------------------------------------\n");
            }
            System.IO.File.AppendAllText(logfilename, message + "\n");
            System.IO.File.AppendAllText(logfilename, "************************************************************\n\n");
            System.IO.File.AppendAllText(logfilename, DateTime.Now.ToString() + "\n\n\n");
        };


        RoslynSemanticBase demo_model;
        if (string.IsNullOrEmpty(solutionName))
        {
            string exampleCodeString = @"
            using System;
            class HelloWorld {
                static void Main() {
                    string[] args = Environment.GetCommandLineArgs()
                    Console.WriteLine(args[10]);
                }
            }";
            if (!string.IsNullOrEmpty(open_code_file))
            {
                exampleCodeString = System.IO.File.ReadAllText(open_code_file);
            }
            demo_model = new ByCodeStringIterator();
            demo_model.Verbose = verbose;
            demo_model.SolutionFolder = baseFolder;
            demo_model.CreateSolutionFromCSharpCode(exampleCodeString);
        }
        else
        {
            demo_model = new ByClassIterator();
            if (useFunction)
            {
                demo_model = new ByFunctionIterator();
                Console.WriteLine("granularity: analyzing by function");
            }
            else
            {
                Console.WriteLine("granularity: analyzing by class/module");
            }
            demo_model.Verbose = verbose;
            demo_model.SolutionFolder = baseFolder;
            if (!string.IsNullOrEmpty(open_project_file))
            {
                demo_model.LoadProjectIntoAdhocWorkspaceAsync(open_project_file);
            }
            else
            {
                demo_model.LoadSolutionFromFolder(solutionFolder);
            }
        }

        // load the ignore codes 
        if (!string.IsNullOrEmpty(ignoreIssuesFile))
        {
            if (File.Exists(ignoreIssuesFile))
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines(ignoreIssuesFile);
                    demo_model.ignoreCodes = new List<string>();
                    foreach (var ln in lines)
                    {
                        if (string.IsNullOrEmpty(ln)) continue;
                        var line = ln.Trim(); 
                        if (string.IsNullOrEmpty(line)) continue;
                        if (line.StartsWith("//")) continue;
                        if (line.StartsWith("#")) continue;
                        if (line.StartsWith(";")) continue;
                        if (line.StartsWith("'")) continue;
                        if (line.ToLower().StartsWith("rem")) continue;
                        demo_model.ignoreCodes.Add(line);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading ignore issues file: " + ex.Message);
                }
            }
        }

        // this is the event that will be triggered when each element i.e. class or function the semantic model is ready
        demo_model.ResultEvent += async (sender, result) =>
        {
            RoslynSemanticBase.ResultObject res = (RoslynSemanticBase.ResultObject)result;
            if (res == null)
            {
                Console.WriteLine("No result object.");
                return;
            }
            var code_prompt = result.code;
            var issue_prompt = result.issues;
            var logText = result.file;
            var issue_count = result.issue_count;
            if (verbose) Console.WriteLine(logText);
            // here's where we call the chatbot(s) to analyze the code and/or check for malware
            bool finished;
            if (doCodeAnalysis)
            {
                //Console.WriteLine($"Code Analysis with {issue_count} Issues (" + chatBot.ModelName + "): " + logText);
                System.IO.File.AppendAllText(logfilename, $"Code Analysis with {issue_count} Issues ( (" + chatBot.ModelName + "): " + logText + "\n");
                finished = chatBot.AnalyzeCode(code_prompt, logText, issue_prompt, issue_count).Result;
            }
            if (doMalwareCheck)
            {
                //Console.WriteLine("Code-Only Analysis or Malware Check (" + chatBot.ModelName + "): " + logText);
                System.IO.File.AppendAllText(logfilename, "Code-Only Analysis or Malware Check (" + chatBot.ModelName + "): " + logText + "\n");
                finished = chatBot.AnalyzeCode(code_prompt, logText).Result;
            }
        };
        // make sure the solution is loaded
        if (demo_model.CurrentSolution != null)
        {
            foreach (var ai_model in requestedModels)
            {
                chatBot.ModelName = ai_model;
                demo_model.IterateSemanticModel();  // this will trigger the above ResultEvent
            }
        }

        removeLockFile();

        Console.WriteLine("Finished.");
    }

    private static void print_help_to_console()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("dotnet run [solutionName] | [filePath | projectPath] [options]");
        Console.WriteLine("filePath: Path to a code file to analyze in adhoc workspace (optional, instead of a solutionName)");
        Console.WriteLine("projectPath: Path to a project file (.vbproj, .cs.proj) to analyze in ad-hoc workspace (optional, use this if solutionName fails to load)");
        Console.WriteLine("IMPORTANT: The solutionName must be the name of a folder in the 'slnfolder' base directory");
        Console.WriteLine("Example:");
        Console.WriteLine("ChatClient MySolution -malware -noeval -func             will do a malware check and use the function-based model without code analysis");
        Console.WriteLine("ChatClient MySolution                                    will do a code analysis using the class-based model");
        Console.WriteLine("ChatClient C:\\path\\to\\file.cs                         will analyze the specified code file using an adhoc solution ");
        Console.WriteLine("ChatClient                                               will analyze the built-in example code using an adhoc solution");
        Console.WriteLine("Options:");
        Console.WriteLine("-codeonly: run analysis on code-only w/o compiler issues, e.g. for prompting malware checks, default is code-with-issues  ");
        Console.WriteLine("-nodefault: Disable default code-with-issues analysis (you should enable the -codeonly check if you use this option)");
        Console.WriteLine("-d, -detail: Enable detailed prompt useage (default: concise)");
        Console.WriteLine("-de or -lang=de: Use German language DE prompts (default: English)");
        Console.WriteLine("-model=<modelName>: Specify the model to use (default: \"qwen2.5-coder:32b\", alternatives: \"mixtral:8x7b\", \"phi3:medium-128k\", or \"codegemma\")");
        Console.WriteLine("                    you can specify more than one model by separating them with a comma or semicolon, e.g. -model=\"qwen2.5-coder:32b,phi3:medium-128k\"");
        Console.WriteLine("                    you can also specify a gemini or open ai model by using the -openai_key or -google_key options");
        Console.WriteLine("-openai_key=<key>:  Specify an API-Key for openai gpt chat completion models");
        Console.WriteLine("-google_key=<key>:  Specify an API-Key for google gemini chat completion models");
        Console.WriteLine("-func, -function: Use function-based model (default: class-based model, function-based model is more detailed but slower)");
        Console.WriteLine("-promptfile=<path>: specify a file to use for chatbot prompts (default: prompting.cs)");
        Console.WriteLine("-ignore=<path>: specify a file to use for ignoring issues (default: none)");
        Console.WriteLine("-slnfolder=<path>: specify the base folder for the solution (default: c:\\user_home_folder\\source\\repos\\)");
        Console.WriteLine("-sample: write the default prompts to \"prompts.txt\" in the current directory and exit");
        Console.WriteLine("-v, -verbose: provides additional console output");
        Console.WriteLine("-unlock: bypass the lock file check (only jura code server) (use with caution)");
    }
}



