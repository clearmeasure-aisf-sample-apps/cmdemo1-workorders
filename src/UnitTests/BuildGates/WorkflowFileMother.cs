namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// A small workflow for the <see cref="WorkflowFile"/> fixture: an <c>on.push</c> key, two jobs, and named,
/// unnamed and <c>uses</c>-first steps.
/// </summary>
internal static class WorkflowFileMother
{
    public const string FileName = "sample.yml";

    private const string Workflow = """
        name: Sample
        on:
          push:
            branches: [master]
        jobs:
          build:
            runs-on: ubuntu-latest
            steps:
              - uses: actions/checkout@v4
              - name: Restore
                run: dotnet restore
              - name: Build
                run: dotnet build
              - uses: actions/upload-artifact@v4
                name: Upload
                with:
                  name: build-output
                  retention-days: 7
          push:
            needs: build
            steps:
              - name: Push the image
                run: docker push
        """;

    public static ScratchTree CheckoutWithSampleWorkflow()
    {
        return new ScratchTree("workflow-file").Write($".github/workflows/{FileName}", Workflow);
    }
}
