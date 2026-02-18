using Package.Infrastructure.Common.Extensions;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class StringExtensionsGreenfieldTests
{
    [TestMethod]
    public void FindTopMatches_returns_expected_prioritized_matches()
    {
        var values = new List<string>
        {
            "AlphaOne",
            "AlphaTwo",
            "BetaOne",
            "SomethingElse"
        };

        IReadOnlyCollection<string> readonlyValues = values;
        var matches = "Alpha".FindTopMatches(
            readonlyValues,
            maxMatches: 3,
            distanceThreshold: 5,
            returnExactOnlyIfMatch: false,
            prioritizeStartMatch: true,
            ignoreCase: true);

        Assert.IsTrue(matches.Count > 0);
        Assert.AreEqual("AlphaOne", matches[0]);
    }
}
