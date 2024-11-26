using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyModel;

namespace ChatClient;

#pragma warning disable 

public abstract class RoslynSemanticBase
{

    public abstract event EventHandler<ResultObject>? ResultEvent;

    public string SolutionFolder = "";
    public string CurrentSolutionFile = "";
    
    public Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace CurrentWorkspace;
    public Microsoft.CodeAnalysis.AdhocWorkspace AdhocWorkspace;
    public Microsoft.CodeAnalysis.Solution CurrentSolution;

    public List<string> ignoreCodes = new List<string>() { "System.", "Console.", "\"Console\"" };

    public bool Verbose = false;

    public class ResultObject
    {
        public string code { get; set; }
        public string issues { get; set; }
        public int issue_count { get; set; }
        public string file { get; set; }
    }

    public void LoadSolutionFromFolder(string folderName)
    {
        registerBuildLocator();
        var localSolutionFolder = System.IO.Path.Combine(SolutionFolder,folderName);
        //look througth the path and get all the .sln files
        var slnFiles = Directory.GetFiles(localSolutionFolder, "*.sln", SearchOption.AllDirectories);
        if (slnFiles.Length == 0)
        {
            Console.WriteLine("No solution files found.");
        }
        else 
        {
            var solutionPath = slnFiles[0];
            CurrentSolutionFile = solutionPath;
            Console.WriteLine("analyzing code in " + solutionPath + "...");
            CurrentWorkspace = MSBuildWorkspace.Create();
            CurrentWorkspace.WorkspaceFailed += (sender, e) =>
            {
                Console.WriteLine("SLNWORKSPACE: " + e.Diagnostic.ToString());
            };
            CurrentWorkspace.LoadMetadataForReferencedProjects = false;
            if (solutionPath.EndsWith(".sln"))
            {
                CurrentSolution = CurrentWorkspace.OpenSolutionAsync(solutionPath).Result;
            }
        } 
    }


    public void CreateSolutionFromCSharpCode(string code) 
    {
        Console.WriteLine("analyzing C# code...");
        registerBuildLocator();
        AdhocWorkspace = new AdhocWorkspace();
        // Create a new Solution
        CurrentSolution = AdhocWorkspace.CurrentSolution;

        // Create a new Project
        var projectId = ProjectId.CreateNewId();
        var versionStamp = VersionStamp.Create();

        var projectInfo = ProjectInfo.Create(
            projectId,
            versionStamp,
            "MyProject",           // Project name
            "MyProjectAssembly",   // Assembly name
            LanguageNames.CSharp);


        AdhocWorkspace.WorkspaceFailed += (sender, e) =>
        {
            Console.WriteLine("Adhoc C# WORKSPACE: " + e.Diagnostic.ToString());
        };

        CurrentSolution = CurrentSolution.AddProject(projectInfo);

        var project = CurrentSolution.GetProject(projectId);
        // Use DependencyContext to get all the runtime libraries
        var references = DependencyContext.Default.CompileLibraries
            .SelectMany(library => library.ResolveReferencePaths())
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();

        // Add the references to the project
        project = project.AddMetadataReferences(references);
        CurrentSolution = project.Solution;

        var documentId = DocumentId.CreateNewId(projectId);
        CurrentSolution = CurrentSolution.AddDocument(
            documentId,
            "MyDocument.cs",
            SourceText.From(code));

    }

    public void CreateSolutionFromVisualBasicCode(string code)
    {
        Console.WriteLine("analyzing VB Code");
        registerBuildLocator();
        AdhocWorkspace = new AdhocWorkspace();
        // Create a new Solution
        CurrentSolution = AdhocWorkspace.CurrentSolution;

        // Create a new Project
        var projectId = ProjectId.CreateNewId();
        var versionStamp = VersionStamp.Create();

        var projectInfo = ProjectInfo.Create(
            projectId,
            versionStamp,
            "MyProject",           // Project name
            "MyProjectAssembly",   // Assembly name
            LanguageNames.VisualBasic);


        AdhocWorkspace.WorkspaceFailed += (sender, e) =>
        {
            Console.WriteLine("Adhoc VB WORKSPACE: " + e.Diagnostic.ToString());
        };

        CurrentSolution = CurrentSolution.AddProject(projectInfo);

        var documentId = DocumentId.CreateNewId(projectId);

        CurrentSolution = CurrentSolution.AddDocument(
            documentId,
            "MyDocument.vb",
            SourceText.From(code));

    }

    public void LoadProjectIntoAdhocWorkspaceAsync(string projectPath)
    {
        Console.WriteLine("analyzing code in " + projectPath + "...");
        registerBuildLocator();
        // Create an AdhocWorkspace
        var workspace = new AdhocWorkspace();
        workspace.WorkspaceFailed += (sender, e) =>
        {
            Console.WriteLine("Workspace failure: " + e.Diagnostic.Message);
        };

        var projectName = Path.GetFileNameWithoutExtension(projectPath);

        // Determine the language
        string language = null;
        if (projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            language = LanguageNames.CSharp;
        }
        else if (projectPath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
        {
            language = LanguageNames.VisualBasic;
        }

        // Create a new project in the workspace
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            projectName,
            projectName,
            language,
            filePath: projectPath);
        var solution = workspace.CurrentSolution.AddProject(projectInfo);
        var projectId = projectInfo.Id;
        // Add documents (source files)
        var projectFile = System.IO.File.ReadAllText(projectPath);
        var projectFileLines = projectFile.Split('\n');
        var compileFiles = new List<string>();
        foreach (var line in projectFileLines)
        {
            if (line.Contains("<Compile Update="))
            {
                var file = line.Replace("<Compile Update=\"", "").Replace("\" />", "").Trim();
                compileFiles.Add(file);
            }
        }

        foreach (var file in compileFiles)
        {
            var _file = file.Replace("\n", "").Replace("\"", "").Replace(">", "").Trim();
            if (Verbose) Console.WriteLine("Adding file: " + _file);
            var documentId = DocumentId.CreateNewId(projectId);
            var src_file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(projectPath), _file);
            var src_code = System.IO.File.ReadAllText(src_file);
            solution = solution.AddDocument(
                              documentId,
                              _file,
                              SourceText.From(src_code));
        }

        // Now you can work with the project
        var project = workspace.CurrentSolution.GetProject(projectId);

        CurrentSolution = solution;
        AdhocWorkspace = workspace;
    }

    private void registerBuildLocator()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            try
            {
                var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                if (Verbose)
                {
                    foreach (var instance in visualStudioInstances)
                    {
                        Console.WriteLine($"Found MSBuild instance: {instance.MSBuildPath}");
                    }
                }
                var vsInstance = visualStudioInstances
                    .OrderByDescending(vsi => vsi.Version)
                    .FirstOrDefault(vsi => vsi.DiscoveryType == DiscoveryType.VisualStudioSetup);

                if (vsInstance != null)
                {
                    MSBuildLocator.RegisterInstance(vsInstance);
                    Console.WriteLine($"Registered MSBuild from Visual Studio at {vsInstance.MSBuildPath}");
                }
                else
                {
                    var instance = visualStudioInstances.FirstOrDefault();
                    if (instance != null)
                    {
                        MSBuildLocator.RegisterInstance(instance);
                        Console.WriteLine($"Registered MSBuild from {instance.MSBuildPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem with Build Locator: " + ex.Message);
            }
        }
    }


    public abstract void IterateSemanticModel(); 
}
