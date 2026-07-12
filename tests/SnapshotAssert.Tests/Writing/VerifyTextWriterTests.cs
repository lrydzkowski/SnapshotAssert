using System.Text;
using SnapshotAssert.Writing;
using Xunit;

namespace SnapshotAssert.Tests.Writing;

public class VerifyTextWriterTests
{
    private static VerifyTextWriter CreateWriter(out StringBuilder builder)
    {
        builder = new StringBuilder();
        return new VerifyTextWriter(builder);
    }

    [Fact]
    public void SimpleObject()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Name");
        writer.WriteString("John");
        writer.WritePropertyName("Age");
        writer.WriteValue(30L);
        writer.WriteEndObject();

        Assert.Equal("{\n  Name: John,\n  Age: 30\n}", builder.ToString());
    }

    [Fact]
    public void NestedObjectAndArray()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Nested");
        writer.WriteStartObject();
        writer.WritePropertyName("X");
        writer.WriteValue(1L);
        writer.WriteEndObject();
        writer.WritePropertyName("List");
        writer.WriteStartArray();
        writer.WriteValue(1L);
        writer.WriteValue(2L);
        writer.WriteEndArray();
        writer.WriteEndObject();

        Assert.Equal(
            "{\n  Nested: {\n    X: 1\n  },\n  List: [\n    1,\n    2\n  ]\n}",
            builder.ToString()
        );
    }

    [Fact]
    public void TopLevelArrayOfObjects()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("TestCaseId");
        writer.WriteValue(1L);
        writer.WritePropertyName("StatusCode");
        writer.WriteString("OK");
        writer.WriteEndObject();
        writer.WriteStartObject();
        writer.WritePropertyName("TestCaseId");
        writer.WriteValue(2L);
        writer.WriteEndObject();
        writer.WriteEndArray();

        Assert.Equal(
            "[\n  {\n    TestCaseId: 1,\n    StatusCode: OK\n  },\n  {\n    TestCaseId: 2\n  }\n]",
            builder.ToString()
        );
    }

    [Fact]
    public void EmptyObjectAndArrayCollapse()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Empty");
        writer.WriteStartObject();
        writer.WriteEndObject();
        writer.WritePropertyName("Entries");
        writer.WriteStartArray();
        writer.WriteEndArray();
        writer.WriteEndObject();

        Assert.Equal("{\n  Empty: {},\n  Entries: []\n}", builder.ToString());
    }

    [Fact]
    public void NullValue()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("ResponseBody");
        writer.WriteNull();
        writer.WriteEndObject();

        Assert.Equal("{\n  ResponseBody: null\n}", builder.ToString());
    }

    [Fact]
    public void Booleans()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Yes");
        writer.WriteValue(true);
        writer.WritePropertyName("No");
        writer.WriteValue(false);
        writer.WriteEndObject();

        Assert.Equal("{\n  Yes: true,\n  No: false\n}", builder.ToString());
    }

    [Theory]
    [InlineData(1d, "1.0")]
    [InlineData(1.5d, "1.5")]
    [InlineData(-2d, "-2.0")]
    [InlineData(double.NaN, "NaN")]
    public void Doubles(double value, string expected)
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteValue(value);

        Assert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Decimals()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteValue(1m);

        Assert.Equal("1.0", builder.ToString());
    }

    [Fact]
    public void ByteArrayAsBase64()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteValue(new byte[] { 1, 2, 3 });

        Assert.Equal("AQID", builder.ToString());
    }

    [Fact]
    public void TimeSpanAsToString()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteValue(new TimeSpan(1, 2, 3, 4));

        Assert.Equal("1.02:03:04", builder.ToString());
    }

    [Fact]
    public void UnquotedUnescapedStrings()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Path");
        writer.WriteString("C:\\some\\path \"quoted\"");
        writer.WriteEndObject();

        Assert.Equal("{\n  Path: C:\\some\\path \"quoted\"\n}", builder.ToString());
    }

    [Fact]
    public void MultiLineStringInPropertyPositionStartsOnNextLine()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("ResponseBody");
        writer.WriteString("{\n  \"key\": \"value\"\n}");
        writer.WritePropertyName("StatusCode");
        writer.WriteString("OK");
        writer.WriteEndObject();

        Assert.Equal(
            "{\n  ResponseBody:\n{\n  \"key\": \"value\"\n},\n  StatusCode: OK\n}",
            builder.ToString()
        );
    }

    [Fact]
    public void MultiLineStringStartingWithNewLineAddsNoExtraBreak()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Text");
        writer.WriteString("\nline1\nline2");
        writer.WriteEndObject();

        Assert.Equal("{\n  Text:\nline1\nline2\n}", builder.ToString());
    }

    [Fact]
    public void MultiLineStringInArrayPositionWritesVerbatimOnItsLine()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartArray();
        writer.WriteString("line1\nline2");
        writer.WriteString("next");
        writer.WriteEndArray();

        Assert.Equal("[\n  line1\nline2,\n  next\n]", builder.ToString());
    }

    [Fact]
    public void ValueInsideObjectWithoutPropertyNameThrows()
    {
        VerifyTextWriter writer = CreateWriter(out _);
        writer.WriteStartObject();

        Assert.Throws<InvalidOperationException>(() => writer.WriteString("value"));
    }

    [Fact]
    public void EmptyStringPropertyValueHasNoTrailingSpace()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Name");
        writer.WriteString("");
        writer.WriteEndObject();

        Assert.Equal("{\n  Name:\n}", builder.ToString());
    }

    [Fact]
    public void EmptyStringBetweenArrayItemsRendersAsSeparatorOnlyLine()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartArray();
        writer.WriteString("a");
        writer.WriteString("");
        writer.WriteString("b");
        writer.WriteEndArray();

        Assert.Equal("[\n  a,\n,\n  b\n]", builder.ToString());
    }

    [Fact]
    public void EmptyStringAsLastArrayItemRendersAsBlankLine()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartArray();
        writer.WriteString("a");
        writer.WriteString("");
        writer.WriteEndArray();

        Assert.Equal("[\n  a,\n\n]", builder.ToString());
    }

    [Fact]
    public void PropertyNameWithNewlinesStaysOnOneLine()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("multi\nline\r\nname");
        writer.WriteString("value");
        writer.WriteEndObject();

        Assert.Equal("{\n  multi\\nline\\nname: value\n}", builder.ToString());
    }

    [Fact]
    public void EmptyValuesNeverProduceLinesEndingInWhitespace()
    {
        VerifyTextWriter writer = CreateWriter(out StringBuilder builder);
        writer.WriteStartObject();
        writer.WritePropertyName("Empty");
        writer.WriteString("");
        writer.WritePropertyName("Items");
        writer.WriteStartArray();
        writer.WriteString("");
        writer.WriteString("x");
        writer.WriteString("");
        writer.WriteEndArray();
        writer.WriteEndObject();

        foreach (string line in builder.ToString().Split('\n'))
        {
            Assert.False(line.EndsWith(' ') || line.EndsWith('\t'), $"Line ends in whitespace: '{line}'");
        }
    }
}
