using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Build.Locator;

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
            Console.WriteLine("Solution Workspace: No solution files found.");
        }
        else 
        {
            var solutionPath = slnFiles[0];
            CurrentSolutionFile = solutionPath;
            Console.WriteLine("Solution Workspace: analyzing code in " + solutionPath + "...");
            CurrentWorkspace = MSBuildWorkspace.Create();
            CurrentWorkspace.WorkspaceFailed += (sender, e) =>
            {
                Console.WriteLine("Solution Workspace:  " + e.Diagnostic.ToString());
            };
            CurrentWorkspace.LoadMetadataForReferencedProjects = true;
            if (solutionPath.EndsWith(".sln"))
            {
                try
                {
                    CurrentSolution = CurrentWorkspace.OpenSolutionAsync(solutionPath).Result;
                    foreach (var project in CurrentSolution.Projects)
                    {
                        var compilation = project.GetCompilationAsync().Result;
                        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.FirstOrDefault());
                        var diagnostics = semanticModel.GetDiagnostics();
                        if (Verbose) Console.WriteLine($"Solution Workspace: {folderName}, {project.Name}:" + diagnostics.Length + " issue(s) found.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Solution Workspace: Error loading solution: " + ex.Message);
                    Console.WriteLine("Verify the solution can be loaded in Visual Studio, before running it through this app!");
                }
            }
        } 
    }

    private void createSolutionFromCode(string code, bool VBCode = false)
    {
        string c_type = "C#";
        if (VBCode) c_type = "VB";
        Console.WriteLine($"Adhoc {c_type} WORKSPACE: analyzing {c_type} code...");
        registerBuildLocator();
        AdhocWorkspace = new AdhocWorkspace();
        // Create a new Solution
        CurrentSolution = AdhocWorkspace.CurrentSolution;

        // Create a new Project
        var projectId = ProjectId.CreateNewId();
        var versionStamp = VersionStamp.Create();

        var lang_type = LanguageNames.CSharp;
        if (VBCode) lang_type = LanguageNames.VisualBasic;
        var projectInfo = ProjectInfo.Create(
            projectId,
            versionStamp,
            "MyProject",           // Project name
            "MyProjectAssembly",   // Assembly name
            lang_type);


        AdhocWorkspace.WorkspaceFailed += (sender, e) =>
        {
            Console.WriteLine($"Adhoc {c_type} WORKSPACE: " + e.Diagnostic.ToString());
        };

        CurrentSolution = CurrentSolution.AddProject(projectInfo);

        // Add necessary core references from GAC
        string[] coreAssemblies = new[]
        {
            "System.Text",
            "System.Runtime",
            "mscorlib",
            "System.Private.CoreLib",
            "System.Console",
            "System.Collections",
            "System.Linq",
        };

        if (VBCode)
        {
            coreAssemblies.Append("Microsoft.VisualBasic");
            coreAssemblies.Append("Microsoft.VisualBasic.Core");
        }

        foreach (var coreAssembly in coreAssemblies)
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                                .FirstOrDefault(a => a.GetName().Name == coreAssembly);
                if (assembly != null)
                {
                    if (Verbose) Console.WriteLine($"Adhoc {c_type} WORKSPACE: Adding core reference to: " + coreAssembly);
                    CurrentSolution = CurrentSolution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(assembly.Location));
                }
                else
                {
                    Console.WriteLine($"Adhoc {c_type} WORKSPACE: Warning - Could not find core assembly: " + coreAssembly);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Adhoc {c_type} WORKSPACE: Error adding core reference to: " + coreAssembly + " - " + ex.Message);
            }
        }

        var doc_ext = "cs";
        if (VBCode) doc_ext = "vb";
        var documentId = DocumentId.CreateNewId(projectId);
        CurrentSolution = CurrentSolution.AddDocument(
            documentId,
            $"MyDocument.{doc_ext}",
            SourceText.From(code));

        var project = CurrentSolution.GetProject(projectId);
        var compilation = project.GetCompilationAsync().Result;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.FirstOrDefault());
        var diagnostics = semanticModel.GetDiagnostics();
        if (Verbose) Console.WriteLine($"Adhoc {c_type} WORKSPACE: " + diagnostics.Length + " issues found.");

    }


    public void CreateSolutionFromCSharpCode(string code) 
    {
        createSolutionFromCode(code);
    }

    public void CreateSolutionFromVisualBasicCode(string code)
    {
        createSolutionFromCode(code, true);
    }

    public void LoadProjectIntoAdhocWorkspaceAsync(string projectPath)
    {
        Console.WriteLine("Adhoc Project: analyzing code in " + projectPath + "...");
        registerBuildLocator();
        // Create an AdhocWorkspace
        var workspace = new AdhocWorkspace();
        workspace.WorkspaceFailed += (sender, e) =>
        {
            Console.WriteLine("Adhoc Project Workspace failure: " + e.Diagnostic.Message);
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

        // Add necessary core references from GAC
        string[] coreAssemblies = new[]
        {
            "System.Text",
            "System.Runtime",
            "mscorlib",
            "System.Private.CoreLib",
            "System.Console",
            "System.Collections",
            "System.Linq",
        };

        if (language == LanguageNames.VisualBasic)
        {
            coreAssemblies.Append("Microsoft.VisualBasic");
            coreAssemblies.Append("Microsoft.VisualBasic.Core");
        }

        foreach (var coreAssembly in coreAssemblies)
        {
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                                .FirstOrDefault(a => a.GetName().Name == coreAssembly);
                if (assembly != null)
                {
                    if (Verbose) Console.WriteLine($"Adhoc Project: Adding core reference to: " + coreAssembly);
                    solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(assembly.Location));
                }
                else
                {
                    Console.WriteLine($"Adhoc Project: Warning - Could not find core assembly: " + coreAssembly);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Adhoc Project: Adding core reference to: " + coreAssembly + " - " + ex.Message);
            }
        }

        // Add documents (source files)
        var projectFile = System.IO.File.ReadAllText(projectPath);
        var projectFileLines = projectFile.Split('\n');
        var compileFiles = new List<string>();
        var references = new List<string>();
        foreach (var line in projectFileLines)
        {
            if (line.Contains("<Compile Update="))
            {
                var file = line.Replace("<Compile Update=\"", "").Replace("\" />", "").Trim();
                compileFiles.Add(file);
            }
            if (line.Contains("<Reference Include="))
            {
                var file = line.Replace("<Reference Include=\"", "").Replace("\">", "").Trim();
                references.Add(file);
            }
        }

        foreach (var file in compileFiles)
        {
            var _file = file.Replace("\n", "").Replace("\"", "").Replace(">", "").Trim();
            if (Verbose) Console.WriteLine("Adhoc Project: Adding file: " + _file);
            var documentId = DocumentId.CreateNewId(projectId);
            var src_file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(projectPath), _file);
            var src_code = System.IO.File.ReadAllText(src_file);
            solution = solution.AddDocument(
                              documentId,
                              _file,
                              SourceText.From(src_code));
        }

        foreach (var reference in references)
        {
            var _reference = reference.Replace("\n", "").Replace("\"", "").Replace(">", "").Trim();
            if (Verbose) Console.WriteLine("Adhoc Project: Adding reference: " + _reference);
            try
            {
                solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(_reference));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Adhoc Project: Error adding reference: " + _reference + " - " + ex.Message);
            }
        }

        CurrentSolution = solution;
        workspace.TryApplyChanges(solution);
        AdhocWorkspace = workspace;

        var project = workspace.CurrentSolution.GetProject(projectId);
        var compilation = project.GetCompilationAsync().Result;
        var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.FirstOrDefault());
        var diagnostics = semanticModel.GetDiagnostics();
        if (Verbose) Console.WriteLine($"Adhoc Project: " + diagnostics.Length + " issues found.");
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
