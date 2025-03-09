// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

namespace Sammo.Oeis.Tests;

public class TestDataProvider
{
    public Stream GetTestData(string name)
    {
        var testDataStream = GetType().Assembly.GetManifestResourceStream(name);

        Assert.NotNull(testDataStream);

        return testDataStream;
    }
}
