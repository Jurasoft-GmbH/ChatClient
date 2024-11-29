using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    internal class AutomatedJobBase
    {
        public string JobName { get; set; }
        public string solutionFolder { get; set; }
        public string baseFolder { get; set; }
        public string solutionName { get; set; }
        public string open_project_file { get; set; }
        public string open_code_file { get; set; }
        public string ignoreIssuesFile { get; set; }
        public bool verbose { get; set; }
        public bool useFunction { get; set; }
        public bool doCodeAnalysis { get; set; }
        public bool doMalwareCheck { get; set; }
        public List<string> requestedModels { get; set; }

        public string promptFile { get; set; }
        public Prompts.Language preferredLanguage { get; set; }
        public Prompts.Detail preferredDetail { get; set; }
        public string OpenAI_ApiKey { get; set; }
        public string Gemini_ApiKey { get; set; }
        public string Claude_ApiKey { get; set; }

        public string lockFile = @"\\code-server\daten\ai-lock.txt";
        public bool bypassLock = false;

        public void Run()
        {
            if (!string.IsNullOrEmpty(OpenAI_ApiKey))
            {
                bypassLock = true;
            }
            if (!string.IsNullOrEmpty(Gemini_ApiKey))
            {
                bypassLock = true;
            }
            if (!string.IsNullOrEmpty(Claude_ApiKey))
            {
                bypassLock = true;
            }
            Console.WriteLine("Running job: " + JobName);
            if (!Directory.Exists(solutionFolder))
            {
                Console.WriteLine("Error: solution folder not found: " + solutionFolder);
                return;
            }
            if (!bypassLock) 
            { 
                if (checkForLockFile())
                {
                    Console.WriteLine("Error: AI is locked by another user: " + getLockFileContent());
                    Console.WriteLine("Please try again later.");
                    return;
                }
            }


            if (!bypassLock)
            {
                setLockFileContent();
            }

            var logfilename = Path.Combine(solutionFolder, "ai-log.txt");

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
                if (verbose)
                {
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
                    string aim = ai_model.Replace(":","_");
                    logfilename = Path.Combine(solutionFolder, aim + "-log.txt");
                    if (verbose) Console.WriteLine("initializing log file - " + logfilename);
                    if (File.Exists(logfilename))
                    {
                        File.Delete(logfilename);
                    }
                    // print date and time to the log file
                    System.IO.File.AppendAllText(logfilename, DateTime.Now.ToString() + "\n\n");

                    chatBot.ModelName = ai_model;
                    demo_model.IterateSemanticModel();  // this will trigger the above ResultEvent
                }
            }

            // remove the lock file
            removeLockFile();
            Console.WriteLine("Finished job: " + JobName);
        }

        public void ReadJobDataFromFile(string pathToJobFile)
        {
            if (File.Exists(pathToJobFile))
            {
                Dictionary<string,string> jobData = File.ReadAllLines(pathToJobFile).Select(line => line.Split('=')).ToDictionary(line => line[0].Trim(), line => line[1].Trim());
                foreach (var item in jobData)
                {
                    if (item.Key == "JobName")
                    {
                        JobName = item.Value;
                    }
                    if (item.Key == "solutionFolder")
                    {
                        solutionFolder = item.Value;
                    }
                    if (item.Key == "baseFolder")
                    {
                        baseFolder = item.Value;
                    }
                    if (item.Key == "solutionName")
                    {
                        solutionName = item.Value;
                    }
                    if (item.Key == "open_project_file")
                    {
                        open_project_file = item.Value;
                    }
                    if (item.Key == "open_code_file")
                    {
                        open_code_file = item.Value;
                    }
                    if (item.Key == "ignoreIssuesFile")
                    {
                        ignoreIssuesFile = item.Value;
                    }
                    if (item.Key == "verbose")
                    {
                        verbose = bool.Parse(item.Value);
                    }
                    if (item.Key == "useFunction")
                    {
                        useFunction = bool.Parse(item.Value);
                    }
                    if (item.Key == "doCodeAnalysis")
                    {
                        doCodeAnalysis = bool.Parse(item.Value);
                    }
                    if (item.Key == "doMalwareCheck")
                    {
                        doMalwareCheck = bool.Parse(item.Value);
                    }
                    if (item.Key == "requestedModels")
                    {
                        requestedModels = item.Value.Split(',').ToList();
                    }
                    if (item.Key == "promptFile")
                    {
                        promptFile = item.Value;
                    }
                    if (item.Key == "preferredLanguage")
                    {
                        preferredLanguage = (Prompts.Language)Enum.Parse(typeof(Prompts.Language), item.Value);
                    }
                    if (item.Key == "preferredDetail")
                    {
                        preferredDetail = (Prompts.Detail)Enum.Parse(typeof(Prompts.Detail), item.Value);
                    }
                    if (item.Key == "OpenAI_ApiKey")
                    {
                        OpenAI_ApiKey = item.Value;
                    }
                    if (item.Key == "Gemini_ApiKey")
                    {
                        Gemini_ApiKey = item.Value;
                    }
                    if (item.Key == "Claude_ApiKey")
                    {
                        Claude_ApiKey = item.Value;
                    }
                    if (item.Key == "lockFile")
                    {
                        lockFile = item.Value;
                    }
                    if (item.Key == "bypassLock")
                    {
                        bypassLock = bool.Parse(item.Value);
                    }
                }
            }
        }

        public void SaveJobToFile(string pathToJobFile)
        {
            try
            {
                string requestedModelsString = "";
                if (requestedModels != null)
                {
                    requestedModelsString = string.Join(",", requestedModels);
                }
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(pathToJobFile))
                {
                    sw.WriteLine("JobName=" + JobName);
                    sw.WriteLine("solutionFolder=" + solutionFolder);
                    sw.WriteLine("baseFolder=" + baseFolder);
                    sw.WriteLine("solutionName=" + solutionName);
                    sw.WriteLine("open_project_file=" + open_project_file);
                    sw.WriteLine("open_code_file=" + open_code_file);
                    sw.WriteLine("ignoreIssuesFile=" + ignoreIssuesFile);
                    sw.WriteLine("verbose=" + verbose);
                    sw.WriteLine("useFunction=" + useFunction);
                    sw.WriteLine("doCodeAnalysis=" + doCodeAnalysis);
                    sw.WriteLine("doMalwareCheck=" + doMalwareCheck);
                    sw.WriteLine("requestedModels=" + requestedModelsString);
                    sw.WriteLine("promptFile=" + promptFile);
                    sw.WriteLine("preferredLanguage=" + preferredLanguage);
                    sw.WriteLine("preferredDetail=" + preferredDetail);
                    sw.WriteLine("OpenAI_ApiKey=" + OpenAI_ApiKey);
                    sw.WriteLine("Gemini_ApiKey=" + Gemini_ApiKey);
                    sw.WriteLine("Claude_ApiKey=" + Claude_ApiKey);
                    sw.WriteLine("lockFile=" + lockFile);
                    sw.WriteLine("bypassLock=" + bypassLock);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving job to {pathToJobFile}: " + ex.Message);
            }
        }

        internal bool checkForLockFile()
        {
            if (File.Exists(lockFile))
            {
                return true;
            }
            return false;
        }

        internal string getLockFileContent()
        {
            if (File.Exists(lockFile))
            {
                return File.ReadAllText(lockFile);
            }
            return "";
        }

        internal void setLockFileContent()
        {
            if (!bypassLock)
            {
                var content = "locked by " + Environment.UserName + " on " + Environment.MachineName + " at " + DateTime.Now.ToString();
                File.WriteAllText(lockFile, content);
                Console.WriteLine("AI locked by " + Environment.UserName);
            }
        }

        internal void removeLockFile()
        {
            if (File.Exists(lockFile))
            {
                if (!bypassLock) File.Delete(lockFile);
            }
        }

    }
}
