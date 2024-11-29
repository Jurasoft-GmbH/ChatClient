using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using LibGit2Sharp;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatClient;

#pragma warning disable 

public class ByGitChangeIterator : RoslynSemanticBase
{

    // define an event handler that the instantiator can subscribe to
    
    public override event EventHandler<ResultObject>? ResultEvent;

    public Microsoft.CodeAnalysis.SemanticModel CurrentSemanticModel;

    public override void IterateSemanticModel()
    {

        string repoPath = Repository.Discover(base.SolutionFolder);

        if (repoPath != null)
        {
            using (var repo = new Repository(repoPath))
            {
                if (base.Verbose) Console.WriteLine($"Iterator: Analyzing project repository for git changes");
                // Get the last two commits
                var commits = repo.Commits.Take(2).ToList();
                if (commits.Count >= 2)
                {
                    var lastCommit = commits[0];
                    var previousCommit = commits[1];
                    // Compare the last commit with the previous commit
                    var changes = repo.Diff.Compare<TreeChanges>(previousCommit.Tree, lastCommit.Tree);
                    foreach (var change in changes)
                    {
                        if (base.Verbose) Console.WriteLine($"File {change.Path} has change type {change.Status}");
                        if (change.Path.EndsWith(".cs") || change.Path.EndsWith(".vb")) // Only consider C# and VB source files
                        {
                            string currentFilePath = Path.Combine(repo.Info.WorkingDirectory, change.Path);
                            string currentCode = File.Exists(currentFilePath) ? File.ReadAllText(currentFilePath) : null;

                            // Get the content from the previous commit
                            var blob = previousCommit[change.Path]?.Target as Blob;
                            string previousCode = blob?.GetContentText();

                            SyntaxTree currentTree = currentCode != null ? CSharpSyntaxTree.ParseText(currentCode) : null;
                            SyntaxTree previousTree = previousCode != null ? CSharpSyntaxTree.ParseText(previousCode) : null;

                            // Proceed if at least one of the versions exists
                            if (currentTree != null || previousTree != null)
                            {
                                // Analyze methods
                                AnalyzeMethods(previousTree, currentTree);
                            }
                        }
                    }
                }
                else
                {
                    if (base.Verbose) Console.WriteLine("Iterator: Not enough commits to compare changes.");
                }
            }
        }
        else
        {
            if (base.Verbose) Console.WriteLine("Iterator: solution is not under Git version control.");
        }
    }


    void AnalyzeMethods(SyntaxTree previousTree, SyntaxTree currentTree)
    {
        var previousMethods = GetMethodDeclarations(previousTree);
        var currentMethods = GetMethodDeclarations(currentTree);

        CompareMethods(previousMethods, currentMethods);

        var previousBlocks = GetMethodBlocks(previousTree);
        var currentBlocks = GetMethodBlocks(currentTree);

        CompareBlockMethods(previousBlocks, currentBlocks);
    }

    Dictionary<string, MethodDeclarationSyntax> GetMethodDeclarations(SyntaxTree tree)
    {
        var methods = new Dictionary<string, MethodDeclarationSyntax>();
        if (tree != null)
        {
            var root = tree.GetRoot();
            var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methodDeclarations)
            {
                string methodSignature = method.Identifier.Text + method.ParameterList.ToString();
                methods[methodSignature] = method;
            }
        }
        return methods;
    }
    Dictionary<string, MethodBlockSyntax> GetMethodBlocks(SyntaxTree tree)
    {
        var methods = new Dictionary<string, MethodBlockSyntax>();
        if (tree != null)
        {
            var root = tree.GetRoot();
            var methodBlockDeclarations = root.DescendantNodes().OfType<MethodBlockSyntax>();
            foreach (var method in methodBlockDeclarations)
            {
                var idText = method.BlockStatement.DeclarationKeyword.GetIdentifierText();
                idText += " " + method.SubOrFunctionStatement.Identifier.Text;
                string methodSignature = idText;
                methods[methodSignature] = method;
            }
        }
        return methods;
    }

    void CompareMethods(Dictionary<string, MethodDeclarationSyntax> previousMethods, Dictionary<string, MethodDeclarationSyntax> currentMethods)
    {
        var previousKeys = previousMethods.Keys;
        var currentKeys = currentMethods.Keys;
        // Methods added in the current version
        var addedMethods = currentKeys.Except(previousKeys);
        foreach (var methodKey in addedMethods)
        {
            var function = currentMethods[methodKey];
            var tree = function.SyntaxTree;
            var compilation = CSharpCompilation.Create("temp").AddSyntaxTrees(tree);
            var logText = tree.FilePath.Replace(base.SolutionFolder, "") + " | " + function.Identifier.Text;
            Console.WriteLine("Iterator: Analyzing function " + function.Identifier.Text);
            var semanticModel = compilation.GetSemanticModel(function.SyntaxTree);
            CurrentSemanticModel = semanticModel;
            var modelDiagnostics = semanticModel.GetDiagnostics();
            var functionDiagnostics = semanticModel.GetDiagnostics(function.Span);
            var issue_prompt = "";
            var issue_count = 0;
            foreach (var issue in functionDiagnostics)
            {
                bool contineFlag = false;
                foreach (var ignore in base.ignoreCodes)
                {
                    if (issue.ToString().Contains(ignore))
                    {
                        contineFlag = true;
                        break;
                    }
                }
                if (contineFlag) continue;
            
                issue_count++;
                issue_prompt += $"\n{issue}";
                if (Verbose) Console.WriteLine("Issue: " + issue);
            }
            if (issue_count > 0) Console.WriteLine($"Iterator: {issue_count} Roslyn issue(s)");
            var code_prompt = function.ToFullString();
            if (ResultEvent != null)
            {
                var res = new ResultObject();
                res.code = code_prompt;
                res.issues = issue_prompt;
                res.file = logText;
                res.issue_count = issue_count;
                ResultEvent(this, res);
            }
        }
        // Methods that exist in both versions (potentially modified)
        var commonMethods = previousKeys.Intersect(currentKeys);
        foreach (var methodKey in commonMethods)
        {
            var previousMethod = previousMethods[methodKey];
            var currentMethod = currentMethods[methodKey];
            if (!previousMethod.Body.IsEquivalentTo(currentMethod.Body))
            {
                var function = currentMethods[methodKey];
                var tree = function.SyntaxTree;
                var compilation = CSharpCompilation.Create("temp").AddSyntaxTrees(tree);
                var logText = tree.FilePath.Replace(base.SolutionFolder, "") + " | " + function.Identifier.Text;
                Console.WriteLine("Iterator: Analyzing function " + function.Identifier.Text);
                var semanticModel = compilation.GetSemanticModel(function.SyntaxTree);
                CurrentSemanticModel = semanticModel;
                var modelDiagnostics = semanticModel.GetDiagnostics();
                var functionDiagnostics = semanticModel.GetDiagnostics(function.Span);
                var issue_prompt = "";
                var issue_count = 0;
                foreach (var issue in functionDiagnostics)
                {
                    bool contineFlag = false;
                    foreach (var ignore in base.ignoreCodes)
                    {
                        if (issue.ToString().Contains(ignore))
                        {
                            contineFlag = true;
                            break;
                        }
                    }
                    if (contineFlag) continue;

                    issue_count++;
                    issue_prompt += $"\n{issue}";
                    if (Verbose) Console.WriteLine("Issue: " + issue);
                }
                if (issue_count > 0) Console.WriteLine($"Iterator: {issue_count} Roslyn issue(s)");
                issue_prompt += $"\n\nThe previous version of the code retrieved from the repository looked like this: \n" + previousMethod.Body + "\n";
                var code_prompt = function.ToFullString();
                if (ResultEvent != null)
                {
                    var res = new ResultObject();
                    res.code = code_prompt;
                    res.issues = issue_prompt;
                    res.file = logText;
                    res.issue_count = issue_count;
                    ResultEvent(this, res);
                }
            }
        }
    }

    void CompareBlockMethods(Dictionary<string, MethodBlockSyntax> previousMethods, Dictionary<string, MethodBlockSyntax> currentMethods)
    {
        var previousKeys = previousMethods.Keys;
        var currentKeys = currentMethods.Keys;
        // Methods added in the current version
        var addedMethods = currentKeys.Except(previousKeys);
        foreach (var methodKey in addedMethods)
        {
            var tree = currentMethods[methodKey].SyntaxTree;
            var compilation = VisualBasicCompilation.Create("temp").AddSyntaxTrees(tree);
            var function = currentMethods[methodKey];
            var idText = function.BlockStatement.DeclarationKeyword.GetIdentifierText();
            idText += " " + function.SubOrFunctionStatement.Identifier.Text;
            var logText = tree.FilePath.Replace(base.SolutionFolder, "") + " | " + idText;
            Console.WriteLine("Iterator: Analyzing " + idText);
            var semanticModel = compilation.GetSemanticModel(function.SyntaxTree);
            CurrentSemanticModel = semanticModel;
            var modelDiagnostics = semanticModel.GetDiagnostics();
            var functionDiagnostics = semanticModel.GetDiagnostics(function.Span);
            var issue_prompt = "";
            var issue_count = 0;
            foreach (var issue in functionDiagnostics)
            {
                bool contineFlag = false;
                foreach (var ignore in base.ignoreCodes)
                {
                    if (issue.ToString().Contains(ignore))
                    {
                        contineFlag = true;
                        break;
                    }
                }
                if (contineFlag) continue;

                issue_count++;
                issue_prompt += $"\n{issue}";
                if (Verbose) Console.WriteLine("Issue: " + issue);
            }
            if (issue_count > 0) Console.WriteLine($"Iterator: {issue_count} Roslyn issue(s)");
            var code_prompt = function.ToFullString();
            if (ResultEvent != null)
            {
                var res = new ResultObject();
                res.code = code_prompt;
                res.issues = issue_prompt;
                res.file = logText;
                res.issue_count = issue_count;
                ResultEvent(this, res);
            }
        }
        // Methods that exist in both versions (potentially modified)
        var commonMethods = previousKeys.Intersect(currentKeys);
        foreach (var methodKey in commonMethods)
        {
            var previousMethod = previousMethods[methodKey];
            var currentMethod = currentMethods[methodKey];
            if (!previousMethod.IsEquivalentTo(currentMethod))
            {
                var tree = currentMethods[methodKey].SyntaxTree;
                var compilation = VisualBasicCompilation.Create("temp").AddSyntaxTrees(tree);
                var function = currentMethods[methodKey];
                var idText = function.BlockStatement.DeclarationKeyword.GetIdentifierText();
                idText += " " + function.SubOrFunctionStatement.Identifier.Text;
                var logText = tree.FilePath.Replace(base.SolutionFolder, "") + " | " + idText;
                Console.WriteLine("Iterator: Analyzing " + idText);
                var semanticModel = compilation.GetSemanticModel(function.SyntaxTree);
                CurrentSemanticModel = semanticModel;
                var modelDiagnostics = semanticModel.GetDiagnostics();
                var functionDiagnostics = semanticModel.GetDiagnostics(function.Span);
                var issue_prompt = "";
                var issue_count = 0;
                foreach (var issue in functionDiagnostics)
                {
                    bool contineFlag = false;
                    foreach (var ignore in base.ignoreCodes)
                    {
                        if (issue.ToString().Contains(ignore))
                        {
                            contineFlag = true;
                            break;
                        }
                    }
                    if (contineFlag) continue;

                    issue_count++;
                    issue_prompt += $"\n{issue}";
                    if (Verbose) Console.WriteLine("Issue: " + issue);
                }
                if (issue_count > 0) Console.WriteLine($"Iterator: {issue_count} Roslyn issue(s)");
                issue_prompt += $"\n\nThe previous version of the code retrieved from the repository looked like this: \n" + previousMethod + "\n";
                var code_prompt = function.ToFullString();
                if (ResultEvent != null)
                {
                    var res = new ResultObject();
                    res.code = code_prompt;
                    res.issues = issue_prompt;
                    res.file = logText;
                    res.issue_count = issue_count;
                    ResultEvent(this, res);
                }
            }
        }
    }

}
