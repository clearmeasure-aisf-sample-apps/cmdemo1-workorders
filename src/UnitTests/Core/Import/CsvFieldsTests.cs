using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class CsvFieldsTests
{
    private static IEnumerable<TestCaseData> Records() =>
    [
        new("a,b,c", new[] { "a", "b", "c" }),
        new("a,,c,", new[] { "a", "", "c", "" }),
        new("\"x, y\",z", new[] { "x, y", "z" }),
        new("\"say \"\"hi\"\"\",z", new[] { "say \"hi\"", "z" }),
        new("\"two\nlines\",z", new[] { "two\nlines", "z" }),
        new("\"unclosed", new[] { "unclosed" })
    ];

    [TestCaseSource(nameof(Records))]
    public void ShouldSplitTheRecordIntoFields_WhenFieldsAreQuotedOrNot(string record, string[] expected)
    {
        var fields = CsvFields.Split(record);

        fields.ShouldBe(expected);
    }

    [TestCase("\"\"", 0, true)]
    [TestCase("\"a", 0, false)]
    [TestCase("a\"", 1, false)]
    public void ShouldRecognizeAnEscapedQuote_WhenTwoQuotesStartAtTheIndex(string text, int index, bool expected)
    {
        var escaped = CsvFields.IsEscapedQuote(text, index);

        escaped.ShouldBe(expected);
    }
}
