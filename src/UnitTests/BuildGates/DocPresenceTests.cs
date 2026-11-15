using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class DocPresenceTests
{
    private static readonly string[] Docs = ["docs/environments.md"];

    private static readonly object[] Phrases =
    [
        new object[] { "docs/environments.md", "TDD" },
        new object[] { "docs/environments.md", "UAT" },
        new object[] { "docs/environments.md", "Production" },
    ];

    [TestCaseSource(nameof(Docs))]
    public void ShouldStartWithAnH1Heading_WhenDocIsRead(string doc)
    {
        var text = RepositoryCheckout.Current.ReadText(doc);

        text.ShouldStartWith("# ");
    }

    [TestCaseSource(nameof(Phrases))]
    public void ShouldMentionItsSubject_WhenDocIsRead(string doc, string phrase)
    {
        var text = RepositoryCheckout.Current.ReadText(doc);

        text.ShouldContain(phrase);
    }
}
