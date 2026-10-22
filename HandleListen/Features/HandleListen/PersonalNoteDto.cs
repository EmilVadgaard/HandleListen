public record PersonalNoteDto(int Id, string Text, DateOnly? Date);

public record CreatePersonalNoteRequest(string Text, DateOnly? Date);

public record UpdatePersonalNoteRequest(string Text, DateOnly? Date);
