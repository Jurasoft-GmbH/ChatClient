using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ChatClient;

#pragma warning disable 

public class ByCodeStringIterator : RoslynSemanticBase
{
    public Microsoft.CodeAnalysis.SemanticModel CurrentSemanticModel;

    public override event EventHandler<ResultObject>? ResultEvent;

    public override void IterateSemanticModel()
    {
        Console.WriteLine("Iterator: Analyzing code string");
        // get the syntax tree from the solutions CurrentSolution object
        SyntaxTree tree = base.CurrentSolution.Projects.First().Documents.First().GetSyntaxTreeAsync().Result;
        SyntaxNode root = tree.GetRoot();
        // get the semantic model
        if (root.Language == LanguageNames.CSharp)
        {
            var compilation = CSharpCompilation.Create("Test")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);
            CurrentSemanticModel = compilation.GetSemanticModel(tree);
        } else if (root.Language == LanguageNames.VisualBasic)
        {
            var compilation = VisualBasicCompilation.Create("Test")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);
            CurrentSemanticModel = compilation.GetSemanticModel(tree);
        }
        var model_issues = CurrentSemanticModel.GetDiagnostics();
        var issue_prompt = "";
        var issue_count = 0;
        foreach (var issue in model_issues)
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
        if (issue_count > 0) Console.WriteLine($"Iterator: {issue_count} Roslyn issues");
        var code_prompt = root.ToFullString();
        if (ResultEvent != null)
        {
            var res = new ResultObject();
            res.code = code_prompt;
            res.issues = issue_prompt;
            res.issue_count = issue_count;
            res.file = "local code";
            ResultEvent(this, res);
        }
    }
}
