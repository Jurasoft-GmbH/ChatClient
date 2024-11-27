using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ChatClient;

#pragma warning disable 

public class ByClassIterator : RoslynSemanticBase
{
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
                // classes
                foreach (var classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var logText = tree.FilePath.Replace(base.SolutionFolder, "") + " | " + classDeclaration.Identifier.Text;
                    Console.WriteLine("Iterator: Analyzing class " + classDeclaration.Identifier.Text);
                    var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                    CurrentSemanticModel = semanticModel;
                    var modelDiagnostics = semanticModel.GetDiagnostics();
                    var classDiagnostics = semanticModel.GetDiagnostics(classDeclaration.Span);
                    var issue_prompt = "";
                    var issue_count = 0;
                    foreach (var issue in classDiagnostics)
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
                    var code_prompt = classDeclaration.ToFullString();
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
                // do the same for VB classes
                foreach (var classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassBlockSyntax>())
                {
                    var idtext = classDeclaration.BlockStatement.Identifier.GetIdentifierText();
                    // get the class name
                    
                    var logText = tree.FilePath.Replace(base.SolutionFolder, "") + " | " + idtext;

                    Console.WriteLine("Iterator: Analyzing class " + idtext);
                    var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                    CurrentSemanticModel = semanticModel;
                    var modelDiagnostics = semanticModel.GetDiagnostics();
                    var classDiagnostics = semanticModel.GetDiagnostics(classDeclaration.Span);
                    var issue_prompt = "";
                    var issue_count = 0;
                    foreach (var issue in classDiagnostics)
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

                    var code_prompt = classDeclaration.ToFullString();
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
                //do the same for modules in VB
                foreach (var moduleDeclaration in tree.GetRoot().DescendantNodes().OfType<ModuleBlockSyntax>())
                {
                    var logText = tree.FilePath.Replace(base.SolutionFolder, "") + " | " + moduleDeclaration.ModuleStatement.Identifier.Text;
                    Console.WriteLine("Iterator: Analyzing module " + moduleDeclaration.ModuleStatement.Identifier.Text);
                    var semanticModel = compilation.GetSemanticModel(moduleDeclaration.SyntaxTree);
                    CurrentSemanticModel = semanticModel;
                    var modelDiagnostics = semanticModel.GetDiagnostics();
                    var moduleDiagnostics = semanticModel.GetDiagnostics(moduleDeclaration.Span);
                    var issue_prompt = "";
                    var issue_count = 0;
                    foreach (var issue in moduleDiagnostics)
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
                    var code_prompt = moduleDeclaration.ToFullString();
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
