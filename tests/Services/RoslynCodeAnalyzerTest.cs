// Project Name: DataModelerTests
// File Name: RoslynCodeAnalyzerTest.cs
// Author:  Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers




using System.Text.Json;

using CopilotModeler.Services;

using JetBrains.Annotations;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging.Abstractions;



namespace DataModelerTests.Services;


[TestClass]
[TestSubject(typeof(RoslynCodeAnalyzer))]
public class RoslynCodeAnalyzerTest
{



    [TestMethod]
    public async Task AnalyzeCodeAsync_InvalidDocument_ReturnsEmptyCodeAnalysisResult()
    {
        // Arrange
        var document = CreateDocument(string.Empty);
        Assert.IsNotNull(document, "The created document should not be null.");
        var analyzer = new RoslynCodeAnalyzer(new NullLogger<RoslynCodeAnalyzer>());

        // Act
        CodeAnalysisResult? result = null;

        try
        {
            result = await analyzer.AnalyzeCodeAsync(document);
        }
        catch (Exception ex)
        {
            Assert.Fail($"AnalyzeCodeAsync threw an unexpected exception: {ex.Message}");
        }

        // Assert
        Assert.IsNotNull(result, "The result should not be null.");
        Assert.IsNull(result!.ASTJson, "ASTJson should be null for an invalid document.");
        Assert.IsNull(result.CFGJson, "CFGJson should be null for an invalid document.");
        Assert.IsNull(result.DFGJson, "DFGJson should be null for an invalid document.");
        Assert.IsNull(result.MetricsJson, "MetricsJson should be null for an invalid document.");
        Assert.IsNull(result.NormalizedCode, "NormalizedCode should be null for an invalid document.");
        Assert.IsNull(result.AnonymizationMapJson, "AnonymizationMapJson should be null for an invalid document.");
    }






    [TestMethod]
    public async Task AnalyzeCodeAsync_ValidDocument_ReturnsCodeAnalysisResult()
    {
        // Arrange
        var code = "public class TestClass { public void TestMethod() { int x = 1; } }";
        var document = CreateDocument(code);
        var analyzer = new RoslynCodeAnalyzer(new NullLogger<RoslynCodeAnalyzer>());

        // Act
        var result = await analyzer.AnalyzeCodeAsync(document);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.ASTJson);
        Assert.IsNotNull(result.CFGJson);
        Assert.IsNotNull(result.DFGJson);
        Assert.IsNotNull(result.MetricsJson);
        Assert.IsNotNull(result.NormalizedCode);
        Assert.IsNotNull(result.AnonymizationMapJson);
    }






    [TestMethod]
    public void AnonymizeAndNormalizeCode_ValidInput_ReturnsNormalizedCodeAndMap()
    {
        // Arrange
        var code = "public class TestClass { public void TestMethod() { int x = 1; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var analyzer = new RoslynCodeAnalyzer(new NullLogger<RoslynCodeAnalyzer>());

        // Act
        var (normalizedCode, anonymizationMapJson) = analyzer.AnonymizeAndNormalizeCode(root, code);

        // Assert
        Assert.IsNotNull(normalizedCode);
        Assert.IsNotNull(anonymizationMapJson);
        var anonymizationMap = JsonSerializer.Deserialize<Dictionary<string, string>>(anonymizationMapJson);
        Assert.IsNotNull(anonymizationMap);
        Assert.IsTrue(anonymizationMap.ContainsKey("x"));
    }






    private Document CreateDocument(string code)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = new AdhocWorkspace().CurrentSolution.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp).AddDocument(documentId, "TestDocument.cs", SourceText.From(code));

        return solution.GetDocument(documentId);
    }






    [TestMethod]
    public void ExtractCfgDfg_ValidInput_ReturnsCfgAndDfgJson()
    {
        // Arrange
        var code = "public class TestClass { public void TestMethod() { int x = 1; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var analyzer = new RoslynCodeAnalyzer(new NullLogger<RoslynCodeAnalyzer>());

        // Act
        var (cfgJson, dfgJson) = analyzer.ExtractCfgDfg(tree, root);

        // Assert
        Assert.IsNotNull(cfgJson);
        Assert.IsNotNull(dfgJson);
    }






    [TestMethod]
    public void ExtractCommonAstPropertiesJson_ValidInput_ReturnsAstJson()
    {
        // Arrange
        var code = "public class TestClass { public void TestMethod() { int x = 1; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var analyzer = new RoslynCodeAnalyzer(new NullLogger<RoslynCodeAnalyzer>());

        // Act
        var astJson = analyzer.ExtractCommonAstPropertiesJson(root);

        // Assert
        Assert.IsNotNull(astJson);
    }






    [TestMethod]
    public void ExtractMetricsJson_ValidInput_ReturnsMetricsJson()
    {
        // Arrange
        var code = "public class TestClass { public void TestMethod() { int x = 1; } }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var analyzer = new RoslynCodeAnalyzer(new NullLogger<RoslynCodeAnalyzer>());

        // Act
        var metricsJson = analyzer.ExtractMetricsJson(root);

        // Assert
        Assert.IsNotNull(metricsJson);
    }






    [TestMethod]
    public void FloatArrayToByteArray_NullInput_ThrowsArgumentNullException()
    {
        // Act
        Assert.Throws<ArgumentNullException>(() => RoslynCodeAnalyzer.FloatArrayToByteArray(null));

    }






    [TestMethod]
    public void FloatArrayToByteArray_ValidInput_ReturnsCorrectByteArray()
    {
        // Arrange
        var floats = new[] { 1.0f, 2.0f, 3.0f };
        var expectedBytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, expectedBytes, 0, expectedBytes.Length);

        // Act
        var result = RoslynCodeAnalyzer.FloatArrayToByteArray(floats);

        // Assert
        CollectionAssert.AreEqual(expectedBytes, result);
    }

}
