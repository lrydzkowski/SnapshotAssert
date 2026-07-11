using System.Net;
using System.Text.Json.Nodes;
using SimpleVerify.Scrubbing;
using SimpleVerify.Serialization;
using Xunit;

namespace SimpleVerify.Tests.Serialization;

public class ObjectWalkerTests
{
    private static string Render(object? target, VerifySettings? settings = null)
    {
        settings ??= new VerifySettings();
        Counter counter = new(settings.EffectiveScrubDateTimes, settings.EffectiveScrubGuids);
        return JsonFormatter.AsJson(target, settings, counter).ToString();
    }

    private static VerifySettings TmheSettings()
    {
        VerifySettings settings = new();
        settings.DontIgnoreEmptyCollections();
        settings.AddExtraSettings(
            _ =>
            {
                _.NullValueHandling = NullValueHandling.Include;
                _.DefaultValueHandling = DefaultValueHandling.Include;
            }
        );
        return settings;
    }

    private record SimpleRecord(string Name, int Age);

    [Fact]
    public void SimpleRecordRendersInDeclarationOrder()
    {
        Assert.Equal("{\n  Name: John,\n  Age: 30\n}", Render(new SimpleRecord("John", 30)));
    }

    [Fact]
    public void TopLevelNull()
    {
        Assert.Equal("null", Render(null));
    }

    private record AuditLogTestResult(int TestCaseId, HttpStatusCode StatusCode, string? ResponseBody);

    [Fact]
    public void AuditLogShapeWithMultiLineResponseBody()
    {
        List<AuditLogTestResult> results = new()
        {
            new AuditLogTestResult(1, HttpStatusCode.OK, "{\n  \"items\": []\n}"),
            new AuditLogTestResult(2, HttpStatusCode.NotFound, null)
        };

        Assert.Equal(
            "[\n"
            + "  {\n"
            + "    TestCaseId: 1,\n"
            + "    StatusCode: OK,\n"
            + "    ResponseBody:\n"
            + "{\n"
            + "  \"items\": []\n"
            + "}\n"
            + "  },\n"
            + "  {\n"
            + "    TestCaseId: 2,\n"
            + "    StatusCode: NotFound,\n"
            + "    ResponseBody: null\n"
            + "  }\n"
            + "]",
            Render(results, TmheSettings())
        );
    }

    private class DefaultsTarget
    {
        public string? NullText { get; set; }

        public List<string> EmptyList { get; set; } = [];

        public int ZeroNumber { get; set; }

        public bool FalseFlag { get; set; }

        public string Kept { get; set; } = "value";
    }

    [Fact]
    public void DefaultSettingsOmitNullsDefaultsAndEmptyCollectionsButKeepFalseBooleans()
    {
        Assert.Equal(
            "{\n  FalseFlag: false,\n  Kept: value\n}",
            Render(new DefaultsTarget())
        );
    }

    [Fact]
    public void IncludeEverythingSettingsKeepAllMembers()
    {
        Assert.Equal(
            "{\n  NullText: null,\n  EmptyList: [],\n  ZeroNumber: 0,\n  FalseFlag: false,\n  Kept: value\n}",
            Render(new DefaultsTarget(), TmheSettings())
        );
    }

    [Fact]
    public void NullableValueTypeWithDefaultValueIsKept()
    {
        var target = new { Number = (int?)0 };

        Assert.Equal("{\n  Number: 0\n}", Render(target));
    }

    [Fact]
    public void IgnoredMemberIsOmitted()
    {
        VerifySettings settings = TmheSettings();
        settings.IgnoreMember("errorStackTrace");
        var target = new { message = "failed", errorStackTrace = "at Foo()" };

        Assert.Equal("{\n  message: failed\n}", Render(target, settings));
    }

    private class BaseType
    {
        public string BaseMember { get; set; } = "base";
    }

    private class DerivedType : BaseType
    {
        public string DerivedMember { get; set; } = "derived";
    }

    [Fact]
    public void BaseMembersRenderBeforeDerivedMembers()
    {
        Assert.Equal(
            "{\n  BaseMember: base,\n  DerivedMember: derived\n}",
            Render(new DerivedType())
        );
    }

    [Fact]
    public void DictionariesAreSortedByKey()
    {
        Dictionary<string, int> target = new()
        {
            ["zebra"] = 1,
            ["alpha"] = 2,
            ["mango"] = 3
        };

        Assert.Equal("{\n  alpha: 2,\n  mango: 3,\n  zebra: 1\n}", Render(target));
    }

    private class SelfReferencing
    {
        public string Name { get; set; } = "self";

        public SelfReferencing? Me { get; set; }
    }

    [Fact]
    public void DirectSelfReferenceRendersParentValueToken()
    {
        SelfReferencing target = new();
        target.Me = target;

        Assert.Equal("{\n  Name: self,\n  Me: $parentValue\n}", Render(target));
    }

    private class CycleParent
    {
        public string Name { get; set; } = "parent";

        public CycleChild? Child { get; set; }
    }

    private class CycleChild
    {
        public CycleParent? Parent { get; set; }
    }

    [Fact]
    public void IndirectCycleIsSilentlyOmitted()
    {
        CycleParent parent = new();
        parent.Child = new CycleChild { Parent = parent };

        Assert.Equal("{\n  Name: parent,\n  Child: {}\n}", Render(parent));
    }

    [Fact]
    public void SharedReferenceIsNotACycleAndRendersTwice()
    {
        SimpleRecord shared = new("John", 30);
        var target = new { First = shared, Second = shared };

        Assert.Equal(
            "{\n  First: {\n    Name: John,\n    Age: 30\n  },\n  Second: {\n    Name: John,\n    Age: 30\n  }\n}",
            Render(target)
        );
    }

    [Fact]
    public void TypedGuidsAndDatesAreCounterScrubbedByDefault()
    {
        Guid guid = Guid.NewGuid();
        var target = new
        {
            Id = guid,
            SameId = guid,
            Created = new DateTime(2020, 6, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        Assert.Equal(
            "{\n  Id: Guid_1,\n  SameId: Guid_1,\n  Created: DateTime_1\n}",
            Render(target)
        );
    }

    [Fact]
    public void DontScrubRendersLiteralValues()
    {
        VerifySettings settings = new();
        settings.DontScrubGuids();
        settings.DontScrubDateTimes();
        var target = new
        {
            Id = Guid.Parse("173535ae-995b-4cc6-a74e-8cd4be57039c"),
            Created = new DateTime(2020, 6, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        Assert.Equal(
            "{\n  Id: 173535ae-995b-4cc6-a74e-8cd4be57039c,\n  Created: 2020-06-15 10:30 Utc\n}",
            Render(target, settings)
        );
    }

    [Fact]
    public void StringContainingOnlyAGuidSharesTheTypedCounter()
    {
        Guid guid = Guid.NewGuid();
        var target = new { Id = guid, Text = guid.ToString("D") };

        Assert.Equal("{\n  Id: Guid_1,\n  Text: Guid_1\n}", Render(target));
    }

    [Fact]
    public void EmptyStringRendersAsBlankValue()
    {
        Assert.Equal("{\n  S: \n}", Render(new { S = "" }));
    }

    [Fact]
    public void JsonNodeRendersInObjectGraphFormat()
    {
        JsonNode? node = JsonNode.Parse(
            "{\"key\": \"value\", \"count\": 1.0, \"flag\": true, \"missing\": null, \"list\": []}"
        );

        Assert.Equal(
            "{\n  key: value,\n  count: 1.0,\n  flag: true,\n  missing: null,\n  list: []\n}",
            Render(node)
        );
    }

    private enum SampleFlags
    {
        None = 0,
        First = 1,
        Second = 2
    }

    [Fact]
    public void EnumsRenderAsNames()
    {
        Assert.Equal(
            "{\n  Status: NotFound\n}",
            Render(new { Status = HttpStatusCode.NotFound })
        );
    }

    [Fact]
    public void DefaultEnumValueIsOmittedUnderDefaultValueIgnore()
    {
        Assert.Equal("{}", Render(new { Value = SampleFlags.None }));
    }
}
