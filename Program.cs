using ChatClient;

#pragma warning disable

class Program
{
    static string version_string = "24.11.29.2";

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
    // IMPORTANT: The solutionName must be the name of a folder in the 'slnfolder' base directory. When a solution name is provided,
    //            the the msbuild workspace is used instead of an adhoc workspace. You should have the .NET SDK and all appropriate dependencies
    //            installed to use this feature.

    // Example:
    // dotnet run MySolution -malware -noeval -func             will do a malware check and use the function-based model without code analysis
    // dotnet run MySolution                                    will do a code analysis using the class-based model
    // dotnet run C:\path\to\file.cs                            will analyze the specified code file using an adhoc solution 
    // dotnet run                                               will analyze the built-in example code using an adhoc solution

    // Options:
    // -git: run analysis on git changes only
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
    // -job=<jobName>: specify a job file for an automated job, you can specify more than one job 

    // If the user is not able to run the program, it may be because the AI is locked by another user.
    // The program will check for a lock file and display an error message if the AI is locked.

    static string baseFolder = "";
    static bool bypassLock = false;
    static List<AutomatedJobBase> automatedJobs = new List<AutomatedJobBase>();


    // let's provide a handler if the user presses Ctrl+C so we can remove the lock file
    static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Ctrl+C pressed, removing lock file(s)...");
        foreach (var jobItem in automatedJobs)
        {
            jobItem.removeLockFile();
        }
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
            if (arg.StartsWith("-slnfolder="))
            {
                baseFolder = arg.Replace("-slnfolder=", "").Replace("'", "").Replace("\"", "").Trim();
                continue;
            }
            if (arg == "-h" || arg == "-help" || arg == "/?" || arg == "-?" || arg == "/h" || arg == "/help")
            {
                showHelp = true;
                break;
            }
            if (arg == "-unlock" || arg.Contains("-openai_key") || arg.Contains("-google_key") || arg.Contains("-claude_key"))
            {
                bypassLock = true;
                continue;
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
        Console.WriteLine($"- v. {version_string}");
        Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
        var solutionName = "";
        var open_code_file = "";
        var open_project_file = "";
        bool doMalwareCheck = false;
        bool doCodeAnalysis = true;
        bool useFunction = false;
        bool verbose = false;
        bool useGit = false;
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
            if (arg == "-git")
            {
                useGit = true;
                continue;
            }
            if (arg == "-unlock")
            {
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
            if (arg.Contains("-job="))
            {
                var jobFile = argument.Replace("-job=", "").Replace("'", "").Replace("\"", "").Trim();
                try
                {
                    var j = new AutomatedJobBase();
                    j.ReadJobDataFromFile(jobFile);
                    automatedJobs.Add(j);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading job file {jobFile}: " + ex.Message);
                }
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
            if (arg.Contains("-nodefault") || arg.Contains("-noeval")) 
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
            if (arg.Contains("-slnfolder="))
            {
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
                Prompts.SavePromptStringsToFile("sample_prompts.txt");
                AutomatedJobBase automatedJobBase = new AutomatedJobBase();
                automatedJobBase.JobName = "Sample Job";
                automatedJobBase.baseFolder = baseFolder;
                automatedJobBase.SaveJobToFile("sample_job.txt");
                RoslynSemanticBase rsb;
                rsb = new ByFunctionIterator();
                // open a sample ignore code file
                var ignoreFile = "sample_ignore.txt";
                List<string> ignoreCodes = new List<string>();
                foreach (var code in rsb.ignoreCodes)
                {
                    ignoreCodes.Add(code);
                }
                System.IO.File.WriteAllLines(ignoreFile, ignoreCodes);
                string currentDirectory = System.IO.Directory.GetCurrentDirectory();
                Console.WriteLine($"Sample files 'sample_promts.txt',\n             'sample_job.txt',\n             'sample_ignore.txt'\nwere written to {currentDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing sample file(s): " + ex.Message);
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

        string commandLineArgs = "";
        foreach (var arg in args)
        {
            commandLineArgs += arg + " ";
        }
        AutomatedJobBase job = new AutomatedJobBase();
        job.JobName = "command line: " + commandLineArgs;
        job.baseFolder = baseFolder;
        job.solutionFolder = solutionFolder;
        job.solutionName = solutionName;
        job.open_project_file = open_project_file;
        job.open_code_file = open_code_file;
        job.ignoreIssuesFile = ignoreIssuesFile;
        job.verbose = verbose;
        job.useFunction = useFunction;
        job.doCodeAnalysis = doCodeAnalysis;
        job.doMalwareCheck = doMalwareCheck;
        job.requestedModels = requestedModels.ToList();
        job.promptFile = promptFile;
        job.preferredDetail = preferredDetail;
        job.preferredLanguage = preferredLanguage;
        job.gitHub = useGit;
        job.OpenAI_ApiKey = OpenAI_ApiKey;
        job.Gemini_ApiKey = Gemini_ApiKey;
        job.Claude_ApiKey = Claude_ApiKey;
        job.bypassLock = bypassLock;
        

        automatedJobs.Add(job);

        foreach (var jobItem in automatedJobs)
        {
            try
            {
                jobItem.Run();
            }
            catch (Exception ex)    
            {
                Console.WriteLine("Error running job: " + ex.Message);
            }
        }


        Console.WriteLine("Finished!");
    }

    private static void print_help_to_console()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("dotnet run [solutionName] | [filePath | projectPath] [options]");
        Console.WriteLine("filePath: Path to a code file to analyze in adhoc workspace (optional, instead of a solutionName)");
        Console.WriteLine("projectPath: Path to a project file (.vbproj, .cs.proj) to analyze in ad-hoc workspace (optional, use this if solutionName fails to load)");
        Console.WriteLine("IMPORTANT: The solutionName must be the name of a folder in the 'slnfolder' base directory. When a solution name is provided,");
        Console.WriteLine("           the the msbuild workspace is used instead of an adhoc workspace. You should have the .NET SDK and all appropriate ");
        Console.WriteLine("           dependencies installed to use this feature.");
        Console.WriteLine("Example:");
        Console.WriteLine("ChatClient MySolution -codeonly -noeval -func            will do a malware check and use the function-based model without code analysis");
        Console.WriteLine("ChatClient MySolution                                    will do a code analysis using the class-based model");
        Console.WriteLine("ChatClient C:\\path\\to\\file.cs                         will analyze the specified code file using an adhoc solution ");
        Console.WriteLine("ChatClient                                               will analyze the built-in example code using an adhoc solution");
        Console.WriteLine("Options:");
        Console.WriteLine("-git: run analysis on git changes only");
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
        Console.WriteLine("-job=<jobName>: specify a job file for an automated job, you can specify more than one job");

    }
}



