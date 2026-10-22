// "Slet kun dette event" on one occurrence of a recurring series - the series itself (and its
// Date/Recurrence rule) is untouched; this just marks one computed occurrence date as skipped.
public class CalendarEventException
{
    public int Id { get; set; }
    public int CalendarEventId { get; set; }
    public DateOnly ExceptionDate { get; set; }
}
