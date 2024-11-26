
namespace ChatClient;

#pragma warning disable

public static class Prompts {

    public enum Language 
    {
        EN,
        DE
    }

    public enum Detail 
    {
        Concise,
        Detailed,
    }

    static string codeOnlyUserPrompt = "Examine this code and see if you can spot any malicious or dangerous code:\n{code_prompt}";
    static string codeOnlyUserPromptDetailed = "Examine this code and see if you can spot any malicious or dangerous code:\n{code_prompt}";
    static string codeOnlyUserPromptDE = "Untersuche diesen Code und schau, ob du schädlichen oder gefährlichen Code entdecken kannst:\n{code_prompt}";
    static string codeOnlyUserPromptDEDetailed = "Untersuche diesen Code sorgfältig und schau, ob du schädlichen oder gefährlichen Code entdecken kannst:\n{code_prompt}";
    
    static string codeOnlySystemPrompt = "You are an expert in .NET 8 Core and the VB.NET and C# languages. You are tasked with examining code for any malicious or dangerous code. If you spot any, respond with a concise list of changes you would make to the code.";
    static string codeOnlySystemPromptDetailed = "You are an expert in .NET 8 Core and the VB.NET and C# languages. You are tasked with examining code for any malicious or dangerous code. If you spot any, respond with a detailed list of changes you would make to the code.";
    static string codeOnlySystemPromptDE = "Du bist ein Experte für .NET 8 Core und die Sprachen VB.NET und C#. Deine Aufgabe ist es, Code auf schädlichen oder gefährlichen Code zu untersuchen. Wenn du welchen findest, antworte mit einer Liste von Änderungen, die du am Code vornehmen würdest.";
    static string codeOnlySystemPromptDEDetailed = "Du bist ein Experte für .NET 8 Core und die Sprachen VB.NET und C#. Deine Aufgabe ist es, Code gründlich auf schädlichen oder gefährlichen Code zu untersuchen. Wenn du welchen findest, antworte mit einer detaillierten Liste von Änderungen, die du am Code vornehmen würdest, einschließlich Erklärungen, warum diese Änderungen notwendig sind.";
    
    static string codeWithIssuesUserPrompt = "Examine this code and concisely explain its function, and then see if you can spot any errors:\n{code_prompt}";
    static string codeWithIssuesUserPromptDetailed = "Examine this code and explain its function in detail, and then see if you can spot any errors:\n{code_prompt}";
    static string codeWithIssuesUserPromptDE = "Untersuche diesen Code und erkläre kurz und knapp seine Funktion. Schau anschließend, ob du Fehler finden kannst:\n{code_prompt}";
    static string codeWithIssuesUserPromptDEDetailed = "Untersuche diesen Code und erkläre seine Funktion im Detail. Schau anschließend, ob du Fehler finden kannst:\n{code_prompt}";   
    
    static string codeWithIssuesSystemPrompt = "You are an expert in .NET 8 Core and the VB.NET and C# languages. Whenever you are asked to spot errors in code, you should provide a modified version of the code that fixes any error you spotted, followed by a list of changes you've made.";
    static string codeWithIssuesSystemPromptDetailed = "You are an expert in .NET 8 Core and the VB.NET and C# languages. Whenever you are asked to spot errors in code, you should provide a modified version of the code that fixes any error you spotted, followed by a list of changes you've made.";
    static string codeWithIssuesSystemDEPrompt = "Du bist ein Experte für .NET 8 Core und die Sprachen VB.NET und C#. Immer wenn du gebeten wirst, Fehler in Code zu finden, solltest du eine modifizierte Version des Codes liefern, die alle gefundenen Fehler behebt, gefolgt von einer Liste der von dir vorgenommenen Änderungen.";
    static string codeWithIssuesSystemPromptDEDetailed = "Du bist ein Experte für .NET 8 Core und die Sprachen VB.NET und C#. Immer wenn du gebeten wirst, Fehler in Code zu finden, solltest du eine modifizierte Version des Codes liefern, die alle gefundenen Fehler behebt. Anschließend füge eine detaillierte Liste der von dir vorgenommenen Änderungen hinzu, einschließlich Erklärungen, warum diese Änderungen notwendig waren.";

    static string codeWithIssuesIssuePrompt = "The following issue was detected by code analysis, fix this in addition to any issues you may have spotted:\n{issue_prompt}";
    static string codeWithIssuesIssuePromptDetailed = "The following issue was detected by code analysis, fix this in addition to any issues you may have spotted:\n{issue_prompt}";
    static string codeWithIssuesIssuePromptDE = "Die Codeanalyse hat das folgende Problem festgestellt. Behebe dieses zusätzlich zu allen anderen Problemen, die du möglicherweise gefunden hast:\n{issue_prompt}";
    static string codeWithIssuesIssuePromptDEDetailed = "Die Codeanalyse hat das folgende Problem festgestellt. Behebe dieses zusätzlich zu allen anderen Problemen, die du möglicherweise gefunden hast:\n{issue_prompt}";
    
    static string codeWithIssuesIssuePromptMultiple = "The following issues were detected by code analysis, fix these in addition to any issues you may have spotted:\n{issue_prompt}";
    static string codeWithIssuesIssuePromptMultipleDetailed = "The following issues were detected by code analysis, fix these in addition to any issues you may have spotted:\n{issue_prompt}";
    static string codeWithIssuesIssuePromptMultipleDE = "Die Codeanalyse hat die folgenden Probleme festgestellt. Behebe diese zusätzlich zu allen anderen Problemen, die du möglicherweise gefunden hast:\n{issue_prompt}";
    static string codeWithIssuesIssuePromptMultipleDEDetailed = "Die Codeanalyse hat die folgenden Probleme festgestellt. Behebe diese zusätzlich zu allen anderen Problemen, die du möglicherweise gefunden hast:\n{issue_prompt}";
    

    public static void SavePromptStringsToFile(string filename) {
        System.IO.File.WriteAllText(filename, 
            "codeOnlyUserPrompt = \"" + codeOnlyUserPrompt + "\";\n" +
            "codeOnlyUserPromptDetailed = \"" + codeOnlyUserPromptDetailed + "\";\n" +
            "codeOnlyUserPromptDE = \"" + codeOnlyUserPromptDE + "\";\n" +
            "codeOnlyUserPromptDEDetailed = \"" + codeOnlyUserPromptDEDetailed + "\";\n" +
            "codeOnlySystemPrompt = \"" + codeOnlySystemPrompt + "\";\n" +
            "codeOnlySystemPromptDetailed = \"" + codeOnlySystemPromptDetailed + "\";\n" +
            "codeOnlySystemPromptDE = \"" + codeOnlySystemPromptDE + "\";\n" +
            "codeOnlySystemPromptDEDetailed = \"" + codeOnlySystemPromptDEDetailed + "\";\n" +
            "codeWithIssuesUserPrompt = \"" + codeWithIssuesUserPrompt + "\";\n" +
            "codeWithIssuesUserPromptDetailed = \"" + codeWithIssuesUserPromptDetailed + "\";\n" +
            "codeWithIssuesUserPromptDE = \"" + codeWithIssuesUserPromptDE + "\";\n" +
            "codeWithIssuesUserPromptDEDetailed = \"" + codeWithIssuesUserPromptDEDetailed + "\";\n" +
            "codeWithIssuesSystemPrompt = \"" + codeWithIssuesSystemPrompt + "\";\n" +
            "codeWithIssuesSystemPromptDetailed = \"" + codeWithIssuesSystemPromptDetailed + "\";\n" +
            "codeWithIssuesSystemDEPrompt = \"" + codeWithIssuesSystemDEPrompt + "\";\n" +
            "codeWithIssuesSystemPromptDEDetailed = \"" + codeWithIssuesSystemPromptDEDetailed + "\";\n" +
            "codeWithIssuesIssuePrompt = \"" + codeWithIssuesIssuePrompt + "\";\n" +
            "codeWithIssuesIssuePromptDetailed = \"" + codeWithIssuesIssuePromptDetailed + "\";\n" +
            "codeWithIssuesIssuePromptDE = \"" + codeWithIssuesIssuePromptDE + "\";\n" +
            "codeWithIssuesIssuePromptDEDetailed = \"" + codeWithIssuesIssuePromptDEDetailed + "\";\n" +
            "codeWithIssuesIssuePromptMultiple = \"" + codeWithIssuesIssuePromptMultiple + "\";\n" +
            "codeWithIssuesIssuePromptMultipleDetailed = \"" + codeWithIssuesIssuePromptMultipleDetailed + "\";\n" +
            "codeWithIssuesIssuePromptMultipleDE = \"" + codeWithIssuesIssuePromptMultipleDE + "\";\n" +
            "codeWithIssuesIssuePromptMultipleDEDetailed = \"" + codeWithIssuesIssuePromptMultipleDEDetailed + "\";\n"
        );
    }

    public static void LoadPromptStringsFromFile(string filename) {
        if (System.IO.File.Exists(filename)) {
            try 
            {
                string[] lines = System.IO.File.ReadAllLines(filename);
                foreach (string line in lines) {
                    string[] parts = line.Split(new char[] { '=' });
                    if (parts.Length == 2) {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        if (key == "codeOnlyUserPrompt") codeOnlyUserPrompt = value.Substring(1, value.Length - 2);
                        if (key == "codeOnlyUserPromptDetailed") codeOnlyUserPromptDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeOnlyUserPromptDE") codeOnlyUserPromptDE = value.Substring(1, value.Length - 2);
                        if (key == "codeOnlyUserPromptDEDetailed") codeOnlyUserPromptDEDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeOnlySystemPrompt") codeOnlySystemPrompt = value.Substring(1, value.Length - 2);
                        if (key == "codeOnlySystemPromptDetailed") codeOnlySystemPromptDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeOnlySystemPromptDE") codeOnlySystemPromptDE = value.Substring(1, value.Length - 2);
                        if (key == "codeOnlySystemPromptDEDetailed") codeOnlySystemPromptDEDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesUserPrompt") codeWithIssuesUserPrompt = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesUserPromptDetailed") codeWithIssuesUserPromptDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesUserPromptDE") codeWithIssuesUserPromptDE = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesUserPromptDEDetailed") codeWithIssuesUserPromptDEDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesSystemPrompt") codeWithIssuesSystemPrompt = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesSystemPromptDetailed") codeWithIssuesSystemPromptDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesSystemDEPrompt") codeWithIssuesSystemDEPrompt = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesSystemPromptDEDetailed") codeWithIssuesSystemPromptDEDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePrompt") codeWithIssuesIssuePrompt = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePromptDetailed") codeWithIssuesIssuePromptDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePromptDE") codeWithIssuesIssuePromptDE = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePromptDEDetailed") codeWithIssuesIssuePromptDEDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePromptMultiple") codeWithIssuesIssuePromptMultiple = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePromptMultipleDetailed") codeWithIssuesIssuePromptMultipleDetailed = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePromptMultipleDE") codeWithIssuesIssuePromptMultipleDE = value.Substring(1, value.Length - 2);
                        if (key == "codeWithIssuesIssuePromptMultipleDEDetailed") codeWithIssuesIssuePromptMultipleDEDetailed = value.Substring(1, value.Length - 2);
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine("Error loading prompt strings from file: " + e.Message);
            }
        }
    }

    
    public static string CodeOnlyUser(Language language, Detail detail) 
    {
        switch  (language) 
        {
            case Language.DE:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeOnlyUserPromptDEDetailed;
                    default:
                        return codeOnlyUserPromptDE;
                }
            default:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeOnlyUserPromptDetailed;
                    default:
                        return codeOnlyUserPrompt;
                }
        }
    }
    public static string CodeOnlySystem(Language language, Detail detail) 
    {
        switch  (language) 
        {
            case Language.DE:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeOnlySystemPromptDEDetailed;
                    default:
                        return codeOnlySystemPromptDE;
                }
            default:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeOnlySystemPromptDetailed;
                    default:
                        return codeOnlySystemPrompt;
                }
        }
    }

    public static string CodeWithIssuesUser(Language language, Detail detail) 
    {
        switch  (language) 
        {
            case Language.DE:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesUserPromptDEDetailed;
                    default:
                        return codeWithIssuesUserPromptDE;
                }
            default:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesUserPromptDetailed;
                    default:
                        return codeWithIssuesUserPrompt;
                }
        }
    }

    public static string CodeWithIssuesSystem(Language language, Detail detail) 
    {
        switch  (language) 
        {
            case Language.DE:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesSystemPromptDEDetailed;
                    default:
                        return codeWithIssuesSystemDEPrompt;
                }
            default:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesSystemPromptDetailed;
                    default:
                        return codeWithIssuesSystemPrompt;
                }
        }
    }

    public static string CodeWithIssuesIssue(Language language, Detail detail) 
    {
        switch  (language) 
        {
            case Language.DE:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesIssuePromptDEDetailed;
                    default:
                        return codeWithIssuesIssuePromptDE;
                }
            default:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesIssuePromptDetailed;
                    default:
                        return codeWithIssuesIssuePrompt;
                }
        }
    }

    public static string CodeWithIssuesMultipleIssues(Language language, Detail detail) 
    {
        switch  (language) 
        {
            case Language.DE:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesIssuePromptMultipleDEDetailed;
                    default:
                        return codeWithIssuesIssuePromptMultipleDE;
                }
            default:
                switch (detail) 
                {
                    case Detail.Detailed:
                        return codeWithIssuesIssuePromptMultipleDetailed;
                    default:
                        return codeWithIssuesIssuePromptMultiple;
                }
        }
    }
}
