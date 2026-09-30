using System.Text.Json;

namespace StarLink;

public sealed class PlatformData
{
    public List<Profile> Profiles { get; set; } = [];
    public List<Resource> Resources { get; set; } = [];
    public List<Topic> Topics { get; set; } = [];
}
public sealed class Profile { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public string Role { get; set; } = "Educator"; public string School { get; set; } = ""; public string Region { get; set; } = ""; public string Subject { get; set; } = ""; public string Bio { get; set; } = ""; }
public sealed class Resource { public string Id { get; set; } = ""; public string Title { get; set; } = ""; public string Subject { get; set; } = ""; public string Region { get; set; } = ""; public string Type { get; set; } = ""; public string Author { get; set; } = ""; public string Summary { get; set; } = ""; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; }
public sealed class Topic { public string Id { get; set; } = ""; public string Title { get; set; } = ""; public string Body { get; set; } = ""; public string Region { get; set; } = ""; public string Author { get; set; } = ""; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; public List<Reply> Replies { get; set; } = []; }
public sealed class Reply { public string Id { get; set; } = ""; public string Author { get; set; } = ""; public string Body { get; set; } = ""; public DateTimeOffset CreatedAt { get; set; } }
public sealed record ProfileInput(string Name, string School, string Region, string Subject, string Bio);
public sealed record ResourceInput(string Title, string Subject, string Region, string Type, string Summary);
public sealed record TopicInput(string Title, string Body, string Region);
public sealed record ReplyInput(string Body);

public sealed class PlatformStore(IWebHostEnvironment environment)
{
    private readonly string _path = Path.Combine(environment.ContentRootPath, "App_Data", "platform.json");
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    public async Task<PlatformData> ReadAsync()
    {
        await _mutex.WaitAsync();
        try
        {
            if (!File.Exists(_path)) { var seed = Seed(); await Save(seed); return seed; }
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<PlatformData>(stream, Options) ?? Seed();
        }
        finally { _mutex.Release(); }
    }
    public async Task WriteAsync(PlatformData data)
    {
        await _mutex.WaitAsync(); try { await Save(data); } finally { _mutex.Release(); }
    }
    private async Task Save(PlatformData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var stream = File.Create(_path); await JsonSerializer.SerializeAsync(stream, data, Options);
    }
    private static PlatformData Seed()
    {
        var profiles = new List<Profile>
        {
            new() { Id="you", Name="Alex Rivera", School="Mabini Integrated School", Region="CALABARZON", Subject="Science", Bio="STEM educator sharing hands-on science activities and classroom research." },
            new() { Id="p2", Name="Maria Santos", School="Rizal National High School", Region="NCR", Subject="Mathematics", Bio="Exploring inclusive numeracy and practical lesson design." },
            new() { Id="p3", Name="Jose Dela Cruz", School="Cebu City Science High School", Region="Central Visayas", Subject="Science", Bio="Building local STEM partnerships and inquiry-based learning." },
            new() { Id="p4", Name="Liza Mendoza", School="Davao City National High School", Region="Davao Region", Subject="English", Bio="Sharing literacy projects and peer mentoring resources." },
            new() { Id="p5", Name="Ramon Flores", School="Baguio City National High School", Region="Cordillera Administrative Region", Subject="Technology and Livelihood Education", Bio="Connecting classroom innovation with community needs." },
            new() { Id="p6", Name="Ana Reyes", School="Iloilo National High School", Region="Western Visayas", Subject="Mathematics", Bio="Teacher researcher focused on collaborative problem solving." }
        };
        var resources = new List<Resource>
        {
            new() { Id="r1", Title="Low-cost water quality investigation", Subject="Science", Region="CALABARZON", Type="Action research", Author="Alex Rivera", Summary="A classroom-ready investigation using locally available materials to explore water quality and evidence collection." },
            new() { Id="r2", Title="Number talks for mixed-ability classes", Subject="Mathematics", Region="NCR", Type="Lesson plan", Author="Maria Santos", Summary="A short routine that encourages learners to explain different ways of solving the same problem." },
            new() { Id="r3", Title="Community stories as reading material", Subject="English", Region="Davao Region", Type="Teaching resource", Author="Liza Mendoza", Summary="A guide for building reading activities from stories and oral histories in the local community." },
            new() { Id="r4", Title="School garden data notebook", Subject="Science", Region="Central Visayas", Type="Extension project", Author="Jose Dela Cruz", Summary="Student observation sheets for tracking plant growth and sharing results with the community." },
            new() { Id="r5", Title="Peer coaching circle starter kit", Subject="Professional learning", Region="Western Visayas", Type="Guide", Author="Ana Reyes", Summary="Prompts and a simple agenda for a teacher-led monthly practice-sharing circle." }
        };
        var topics = new List<Topic>
        {
            new() { Id="t1", Title="What worked for you in a low-bandwidth science lesson?", Body="I’m planning an inquiry activity for learners with intermittent internet access. What offline materials or routines have worked well in your class?", Region="CALABARZON", Author="Alex Rivera", Replies=[new() { Id="x1", Author="Jose Dela Cruz", Body="We prepared printed observation cards and used a shared class notebook so learners could compare results the next day.", CreatedAt=DateTimeOffset.UtcNow.AddHours(-18) }] },
            new() { Id="t2", Title="Sharing numeracy activities for large classes", Body="Looking for practical ways to hear from more learners during a short number talk. What routines do you use?", Region="NCR", Author="Maria Santos", Replies=[new() { Id="x2", Author="Ana Reyes", Body="Pair explanations followed by two contrasting solutions helped us include more voices.", CreatedAt=DateTimeOffset.UtcNow.AddHours(-7) }] },
            new() { Id="t3", Title="Connecting local stories with reading lessons", Body="Our department is collecting local stories for reading practice. How do you get students involved in choosing and documenting them?", Region="Davao Region", Author="Liza Mendoza" }
        };
        return new PlatformData { Profiles=profiles, Resources=resources, Topics=topics };
    }
}

public sealed record AdminLoginInput(string? Passcode);
