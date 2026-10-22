using System.Xml;

namespace LanguageCore.Tests;

static class TestResults
{
    static string? GetLatestResultFile(string testResultsDirectory)
    {
        string[] files = Directory.GetFiles(testResultsDirectory, "*.trx");
        if (files.Length == 0) return null;
        FileInfo[] fileInfos = files.Select(v => new FileInfo(v)).ToArray();
        Array.Sort(fileInfos, (a, b) => b.CreationTimeUtc.CompareTo(a.CreationTimeUtc));
        return fileInfos[0].FullName;
    }

    record struct TrxTestDefinition(string Name, string[] Categories, string? ExecutionId, string? MethodClassName, string? MethodName);
    record struct TrxTestResult(string Id, string Name, string Outcome, string? ErrorMessage, string? StackTrace);

    static (Dictionary<string, TrxTestDefinition> Definitions, TrxTestResult[] Results) LoadTestResults(string trxFile)
    {
        XmlDocument xml = new();
        xml.LoadXml(File.ReadAllText(trxFile));

        Dictionary<string, TrxTestDefinition> definitions = new();

        {
            XmlElement? _definitions = xml["TestRun"]?["TestDefinitions"];
            if (_definitions != null)
            {
                for (int i = 0; i < _definitions.ChildNodes.Count; i++)
                {
                    XmlNode? _definition = _definitions.ChildNodes.Item(i);

                    if (_definition is null)
                    { continue; }

                    string? id = _definition.Attributes?["id"]?.Value;
                    string? name = _definition.Attributes?["name"]?.Value;
                    List<string> categories = new();
                    XmlElement? _categories = _definition["TestCategory"];
                    if (_categories != null)
                    {
                        for (int j = 0; j < _categories.ChildNodes.Count; j++)
                        {
                            XmlNode? _category = _categories.ChildNodes[j];
                            if (_category == null) continue;
                            string? category = _category.Attributes?["TestCategory"]?.Value;
                            if (category == null) continue;
                            categories.Add(category);
                        }
                    }
                    string? executionId = _definition["Execution"]?.Attributes["id"]?.Value;
                    string? methodClassName = _definition["TestMethod"]?.Attributes["className"]?.Value;
                    string? methodName = _definition["TestMethod"]?.Attributes["name"]?.Value;

                    if (id is null || name is null)
                    { continue; }

                    definitions[id] = new TrxTestDefinition(name, categories.ToArray(), executionId, methodClassName, methodName);
                }
            }
        }

        List<TrxTestResult> results = new();

        {
            XmlElement? _results = xml["TestRun"]?["Results"];
            if (_results != null)
            {
                for (int i = 0; i < _results.ChildNodes.Count; i++)
                {
                    XmlNode? _result = _results.ChildNodes.Item(i);

                    if (_result is null)
                    { continue; }

                    string? id = _result.Attributes?["testId"]?.Value;
                    string? name = _result.Attributes?["testName"]?.Value;
                    string? outcome = _result.Attributes?["outcome"]?.Value;

                    if (id is null || name is null || outcome is null)
                    { continue; }

                    XmlElement? errorMessage_ = _result["Output"]?["ErrorInfo"]?["Message"];
                    string? errorMessage = null;
                    if (errorMessage_ is not null && errorMessage_.FirstChild?.NodeType == XmlNodeType.Text)
                    { errorMessage = errorMessage_.FirstChild.Value; }

                    XmlElement? stackTrace_ = _result["Output"]?["ErrorInfo"]?["StackTrace"];
                    string? stackTrace = null;
                    if (stackTrace_ is not null && stackTrace_.FirstChild?.NodeType == XmlNodeType.Text)
                    { stackTrace = stackTrace_.FirstChild.Value; }

                    results.Add(new TrxTestResult(id, name, outcome, errorMessage, stackTrace));
                }
            }
        }

        return (definitions, results.ToArray());
    }

    public static void GenerateResultsFile(string testResultsDirectory, string resultFile)
    {
        string? latest = GetLatestResultFile(testResultsDirectory) ?? throw new FileNotFoundException($"No test result file found in directory {testResultsDirectory}");

        (Dictionary<string, TrxTestDefinition> definitions, TrxTestResult[] results) = LoadTestResults(latest);

        Dictionary<string, List<(string? Category, string Outcome, string? ErrorMessage, string? StackTrace)>> testFiles = [];

        int passingTestCount = 0;
        int failedTestCount = 0;
        int notRunTestCount = 0;

        foreach ((string id, string name, string outcome, string? errorMessage, string? stackTrace) in results)
        {
            string[] categories = definitions[id].Categories;

            switch (outcome)
            {
                case "Passed": passingTestCount++; break;
                case "Failed": failedTestCount++; break;
                case "NotExecuted": notRunTestCount++; break;
            }

            string? category = null;
            bool isFileTest = false;
            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] == "Generic")
                { isFileTest = true; }
                else
                { category = categories[i]; }
            }

            if (!isFileTest) continue;

            if (!testFiles.TryGetValue(name, out List<(string? Category, string Outcome, string? ErrorMessage, string? StackTrace)>? fileResults))
            {
                fileResults = [];
                testFiles[name] = fileResults;
            }

            fileResults.Add((category, outcome, errorMessage, stackTrace));
        }

        (int SerialNumber, List<(string? Category, string Outcome, string? ErrorMessage, string? StackTrace)> Value)[] sortedTestFiles = testFiles.Select(v => (int.Parse(v.Key[4..]), v.Value)).ToArray();
        Array.Sort(sortedTestFiles, (a, b) => a.SerialNumber.CompareTo(b.SerialNumber));

        using StreamWriter file = File.CreateText(resultFile);

        file.WriteLine("# Test Results");
        file.WriteLine();

        file.WriteLine($"![{passingTestCount} passed, {failedTestCount} failed, {notRunTestCount} skipped](https://svg.test-summary.com/dashboard.svg?p={passingTestCount}&f={failedTestCount}&s={notRunTestCount})");

        file.Write($"![{passingTestCount} passed](https://img.shields.io/badge/Passing-{passingTestCount}-brightgreen?style=plastic]) ");
        file.Write($"![{failedTestCount} failed](https://img.shields.io/badge/Failing-{failedTestCount}-red?style=plastic]) ");
        file.Write($"![{notRunTestCount} skipped](https://img.shields.io/badge/Skipped-{notRunTestCount}-silver?style=plastic])");
        file.WriteLine();

        file.WriteLine(" - ✅ Passed");
        file.WriteLine(" - ❌ Failed");
        file.WriteLine(" - ✖️ Not supported");
        file.WriteLine(" - ➖ Skipped");

        file.WriteLine();

        file.WriteLine("| File | Bytecode | Brainfuck | MSIL |");
        file.WriteLine("| :--: | :------: | :-------: | :-------: |");

        foreach ((int serialNumber, List<(string? Category, string Outcome, string? ErrorMessage, string? StackTrace)>? fileResults) in sortedTestFiles)
        {
            (string? Outcome, string? ErrorMessage, string? StackTrace) bytecodeResult = (null, null, null);
            (string? Outcome, string? ErrorMessage, string? StackTrace) brainfuckResult = (null, null, null);
            (string? Outcome, string? ErrorMessage, string? StackTrace) msilResult = (null, null, null);

            foreach ((string? category, string outcome, string? errorMessage, string? stackTrace) in fileResults)
            {
                switch (category)
                {
                    case "Bytecode": bytecodeResult = (outcome, errorMessage, stackTrace); break;
                    case "Brainfuck": brainfuckResult = (outcome, errorMessage, stackTrace); break;
                    case "MSIL": msilResult = (outcome, errorMessage, stackTrace); break;
                }
            }

            if (bytecodeResult.Outcome == "NotExecuted" &&
                brainfuckResult.Outcome == "NotExecuted" &&
                msilResult.Outcome == "NotExecuted")
            { continue; }

            static string? TranslateOutcome(string? outcome, string? errorMessage, string? stackTrace) => outcome switch
            {
                "Passed" => "✅",
                "Failed" =>
                    (stackTrace?.Contains("not supported", StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (errorMessage?.Contains("not supported", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    ? "✖️"
                    : "❌",
                "NotExecuted" => "➖",
                _ => outcome,
            };

            bytecodeResult.Outcome = TranslateOutcome(bytecodeResult.Outcome, bytecodeResult.ErrorMessage, bytecodeResult.StackTrace);
            brainfuckResult.Outcome = TranslateOutcome(brainfuckResult.Outcome, brainfuckResult.ErrorMessage, brainfuckResult.StackTrace);
            msilResult.Outcome = TranslateOutcome(msilResult.Outcome, msilResult.ErrorMessage, msilResult.StackTrace);

            string translatedName = $"https://github.com/banszkyy/Vibrissa/blob/master/TestFiles/{serialNumber.ToString().PadLeft(3, '0')}{LanguageConstants.LanguageExtension}";
            translatedName = $"[{serialNumber}]({translatedName})";

            file.Write("| ");
            file.Write(translatedName);
            file.Write(" | ");

            file.Write(bytecodeResult.Outcome);

            file.Write(" | ");

            file.Write(brainfuckResult.Outcome);

            file.Write(" | ");

            file.Write(msilResult.Outcome);

            file.Write(" |");
            file.WriteLine();
        }
    }
}
