using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class DocPresenceTests
{
    private static readonly string[] Docs = ["docs/environments.md", "docs/stability-k.md", "docs/walkthrough-g.md"];

    private static readonly object[] Phrases =
    [
        new object[] { "docs/environments.md", "TDD" },
        new object[] { "docs/environments.md", "UAT" },
        new object[] { "docs/environments.md", "Production" },
        new object[] { "docs/stability-k.md", "Stability K" },
        new object[] { "docs/walkthrough-g.md", "Walkthrough G" },
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
