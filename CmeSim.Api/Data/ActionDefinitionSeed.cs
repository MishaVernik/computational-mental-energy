using CmeSim.Api.Models.FlowDataset;
using Microsoft.EntityFrameworkCore;

namespace CmeSim.Api.Data;

public static class ActionDefinitionSeed
{
    // Deterministic GUIDs for seed data (categories use C0-prefix, actions use A0-prefix)
    static Guid C(int n) => Guid.Parse($"C0000000-0000-0000-0000-{n:D12}");
    static Guid A(int n) => Guid.Parse($"A0000000-0000-0000-0000-{n:D12}");

    public static void Seed(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var e = modelBuilder.Entity<ActionDefinition>();

        // ── Categories ───────────────────────────────────────────
        e.HasData(Cat(C(1), "Work",        "work",        "briefcase", now));
        e.HasData(Cat(C(2), "Study",       "study",       "book",      now));
        e.HasData(Cat(C(3), "Creative",    "creative",    "palette",   now));
        e.HasData(Cat(C(4), "Physical",    "physical",    "dumbbell",  now));
        e.HasData(Cat(C(5), "Social",      "social",      "users",     now));
        e.HasData(Cat(C(6), "Leisure",     "leisure",     "gamepad",   now));
        e.HasData(Cat(C(7), "Mindfulness", "mindfulness", "brain",     now));
        e.HasData(Cat(C(8), "Daily",       "daily",       "coffee",    now));

        // ── Work ─────────────────────────────────────────────────
        e.HasData(Act(A(1),  C(1), "Coding",          "coding",          0.70, "code",       now));
        e.HasData(Act(A(2),  C(1), "Code Review",     "code-review",     0.60, "search",     now));
        e.HasData(Act(A(3),  C(1), "Frontend Dev",    "frontend-dev",    0.70, "layout",     now));
        e.HasData(Act(A(4),  C(1), "Backend Dev",     "backend-dev",     0.75, "server",     now));
        e.HasData(Act(A(5),  C(1), "DevOps",          "devops",          0.65, "cloud",      now));
        e.HasData(Act(A(6),  C(1), "Debugging",       "debugging",       0.80, "bug",        now));
        e.HasData(Act(A(7),  C(1), "System Design",   "system-design",   0.85, "sitemap",    now));
        e.HasData(Act(A(8),  C(1), "Meetings",        "meetings",        0.40, "video",      now));
        e.HasData(Act(A(9),  C(1), "Email",           "email",           0.30, "mail",       now));
        e.HasData(Act(A(10), C(1), "Documentation",   "documentation",   0.50, "file-text",  now));

        // ── Study ────────────────────────────────────────────────
        e.HasData(Act(A(11), C(2), "Reading (Technical)",  "reading-technical", 0.60, "book-open",   now));
        e.HasData(Act(A(12), C(2), "Reading (General)",    "reading-general",   0.35, "bookmark",    now));
        e.HasData(Act(A(13), C(2), "Math / Problem Solving","math",             0.90, "calculator",  now));
        e.HasData(Act(A(14), C(2), "Research",             "research",          0.70, "microscope",  now));
        e.HasData(Act(A(15), C(2), "Note-Taking",          "note-taking",       0.45, "edit",        now));
        e.HasData(Act(A(16), C(2), "Exam Prep",            "exam-prep",         0.80, "clipboard",   now));
        e.HasData(Act(A(17), C(2), "Lecture / Webinar",    "lecture",           0.35, "presentation",now));
        e.HasData(Act(A(18), C(2), "Flashcards",           "flashcards",        0.50, "layers",      now));

        // ── Creative ─────────────────────────────────────────────
        e.HasData(Act(A(19), C(3), "Writing (Essays)",     "writing-essays",    0.60, "pen-tool",    now));
        e.HasData(Act(A(20), C(3), "Writing (Creative)",   "writing-creative",  0.55, "feather",     now));
        e.HasData(Act(A(21), C(3), "Drawing / Sketching",  "drawing",           0.50, "pencil",      now));
        e.HasData(Act(A(22), C(3), "Music Composition",    "music-composition", 0.70, "music",       now));
        e.HasData(Act(A(23), C(3), "Music Practice",       "music-practice",    0.55, "headphones",  now));
        e.HasData(Act(A(24), C(3), "Graphic Design",       "graphic-design",    0.60, "image",       now));
        e.HasData(Act(A(25), C(3), "Video Editing",        "video-editing",     0.65, "film",        now));

        // ── Physical ─────────────────────────────────────────────
        e.HasData(Act(A(26), C(4), "Exercise",    "exercise",    0.30, "activity",  now));
        e.HasData(Act(A(27), C(4), "Walking",     "walking",     0.15, "navigation",now));
        e.HasData(Act(A(28), C(4), "Yoga",        "yoga",        0.25, "heart",     now));
        e.HasData(Act(A(29), C(4), "Stretching",  "stretching",  0.15, "move",      now));
        e.HasData(Act(A(30), C(4), "Sports",      "sports",      0.35, "trophy",    now));
        e.HasData(Act(A(31), C(4), "Dance",       "dance",       0.40, "zap",       now));

        // ── Social ───────────────────────────────────────────────
        e.HasData(Act(A(32), C(5), "Conversation",     "conversation",     0.40, "message-circle", now));
        e.HasData(Act(A(33), C(5), "Presentation",     "presentation",     0.65, "monitor",        now));
        e.HasData(Act(A(34), C(5), "Teaching",          "teaching",         0.60, "award",          now));
        e.HasData(Act(A(35), C(5), "Interview",         "interview",        0.70, "mic",            now));
        e.HasData(Act(A(36), C(5), "Phone Call",        "phone-call",       0.30, "phone",          now));
        e.HasData(Act(A(37), C(5), "Group Discussion",  "group-discussion", 0.50, "users",          now));

        // ── Leisure ──────────────────────────────────────────────
        e.HasData(Act(A(38), C(6), "Gaming (Strategy)",    "gaming-strategy",  0.60, "target",   now));
        e.HasData(Act(A(39), C(6), "Gaming (Action)",      "gaming-action",    0.45, "crosshair",now));
        e.HasData(Act(A(40), C(6), "Gaming (Cards/Board)", "gaming-cards",     0.40, "grid",     now));
        e.HasData(Act(A(41), C(6), "Watching Video",       "watching-video",   0.15, "play",     now));
        e.HasData(Act(A(42), C(6), "Browsing",             "browsing",         0.20, "globe",    now));
        e.HasData(Act(A(43), C(6), "Social Media",         "social-media",     0.20, "share-2",  now));

        // ── Mindfulness ──────────────────────────────────────────
        e.HasData(Act(A(44), C(7), "Meditation",          "meditation",          0.10, "sunset",   now));
        e.HasData(Act(A(45), C(7), "Breathwork",          "breathwork",          0.10, "wind",     now));
        e.HasData(Act(A(46), C(7), "Body Scan",           "body-scan",           0.15, "eye",      now));
        e.HasData(Act(A(47), C(7), "Resting (Eyes Open)", "resting-eyes-open",   0.05, "eye",      now));
        e.HasData(Act(A(48), C(7), "Resting (Eyes Closed)","resting-eyes-closed",0.05, "moon",     now));

        // ── Daily ────────────────────────────────────────────────
        e.HasData(Act(A(49), C(8), "Eating",     "eating",     0.10, "utensils",    now));
        e.HasData(Act(A(50), C(8), "Commuting",  "commuting",  0.20, "train",       now));
        e.HasData(Act(A(51), C(8), "Cooking",    "cooking",    0.30, "thermometer", now));
        e.HasData(Act(A(52), C(8), "Cleaning",   "cleaning",   0.15, "trash-2",     now));
    }

    private static ActionDefinition Cat(Guid id, string name, string slug, string icon, DateTime now) => new()
    {
        Id = id, ParentId = null, Name = name, Slug = slug, Icon = icon,
        DefaultDifficulty = 0, IsSystem = true, IsActive = true, CreatedAt = now
    };

    private static ActionDefinition Act(Guid id, Guid parentId, string name, string slug, double diff, string icon, DateTime now) => new()
    {
        Id = id, ParentId = parentId, Name = name, Slug = slug, Icon = icon,
        DefaultDifficulty = diff, IsSystem = true, IsActive = true, CreatedAt = now
    };
}
