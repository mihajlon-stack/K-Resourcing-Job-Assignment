namespace Claims.Domain;

/// <summary>
/// Billable length of a cover. The end date is exclusive: a cover from Jan 1 to Jan 31
/// bills 30 days. This is a billing-days concept only and must not be reused for
/// coverage/containment or calendar-span checks, which follow different conventions.
/// </summary>
public readonly struct CoverPeriod
{
    public CoverPeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentOutOfRangeException(nameof(endDate), "End date cannot precede start date.");
        }

        StartDate = startDate;
        EndDate = endDate;
    }

    public DateOnly StartDate { get; }

    public DateOnly EndDate { get; }

    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber;
}
