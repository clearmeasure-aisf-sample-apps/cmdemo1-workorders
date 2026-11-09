using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class CsvLogicalLineReaderTests
{
    [Test]
    public void ShouldReadOneRecordPerLine_WhenNoFieldIsQuoted()
    {
        var reader = new CsvLogicalLineReader(new StringReader("a,b\nc,d\n"));

        var first = reader.ReadRecord();
        var second = reader.ReadRecord();
        var end = reader.ReadRecord();

        first.ShouldBe("a,b");
        second.ShouldBe("c,d");
        end.ShouldBeNull();
        reader.LineNumber.ShouldBe(2);
    }

    [Test]
    public void ShouldJoinTheFollowingLines_WhileAQuotedFieldIsOpen()
    {
        var reader = new CsvLogicalLineReader(new StringReader("a,\"line one\nline two\",b\nnext"));

        var record = reader.ReadRecord();

        record.ShouldBe("a,\"line one\nline two\",b");
        reader.LineNumber.ShouldBe(2);
    }

    [Test]
    public void ShouldNotTreatADoubledQuoteAsOpen_WhenItIsEscaped()
    {
        var reader = new CsvLogicalLineReader(new StringReader("\"say \"\"hi\"\"\",b\nnext"));

        var record = reader.ReadRecord();

        record.ShouldBe("\"say \"\"hi\"\"\",b");
    }

    [Test]
    public void ShouldReturnWhatWasRead_WhenTheTextEndsInsideAQuote()
    {
        var reader = new CsvLogicalLineReader(new StringReader("a,\"never closed\nstill open"));

        var record = reader.ReadRecord();

        record.ShouldBe("a,\"never closed\nstill open");
        reader.LineNumber.ShouldBe(2);
    }
}
