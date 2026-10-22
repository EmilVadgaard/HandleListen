public class PersonalNote
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public required string Text { get; set; }

    // Null = a sticky note in the sidebar (no date). Set = appears in the chronological
    // noticeboard feed on that date.
    public DateOnly? Date { get; set; }
}
