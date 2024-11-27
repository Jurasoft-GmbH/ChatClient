using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ChatClient;

#pragma warning disable 

public class ByFunctionIterator : RoslynSemanticBase
{

    // define an event handler that the instantiator can subscribe to
    
    public override event EventHandler<ResultObject>? ResultEvent;

    public Microsoft.CodeAnalysis.SemanticModel CurrentSemanticModel;

    public override void IterateSemanticModel()
    {
        foreach (var project in CurrentSolution.Projects)
        {
            if (base.Verbose) Console.WriteLine($"Iterator: Analyzing project {project.Name}");
            var compilation = project.GetCompilationAsync().Result;
            var compilationDiagnostics = compilation.GetDiagnostics();
            foreach (var tree in compilation.SyntaxTrees)
            {
                if (base.Verbose) Console.WriteLine("Iterator: Analyzing file " + tree.FilePath);
                foreach (var function in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
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
                // do the same for VB functions and subs
                foreach (var function in tree.GetRoot().DescendantNodes().OfType<MethodBlockSyntax>())
                {
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
            }
        }
    }
}
