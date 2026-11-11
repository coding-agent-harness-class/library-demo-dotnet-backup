using Library.Domain.Fees;
using Library.Domain.Lending;

namespace Library.Domain.Tests;

public class OverdueLoansTests
{
    private readonly FixedClock _clock = new(new DateOnly(2026, 6, 1));
    private readonly LoanService _service;

    public OverdueLoansTests()
    {
        _service = new LoanService(new InMemoryBookCatalog(), new InMemoryMemberDirectory(),
            new InMemoryLoanRepository(), new LoanPolicy(), new DueDateCalculator(new LibraryCalendar()),
            new LateFeeCalculator(), _clock);
    }

    [Fact]
    public void Overdue_NothingPastDue_IsEmpty()
    {
        _service.Borrow("M-100", "9780134685991");

        Assert.Empty(_service.Overdue());
    }

    [Fact]
    public void Overdue_LoanPastDue_ReportsDaysAndFee()
    {
        var loan = _service.Borrow("M-100", "9780134685991").Loan!;
        _clock.Today = loan.DueOn.AddDays(5);

        var overdue = Assert.Single(_service.Overdue());

        Assert.Equal(loan.Id, overdue.LoanId);
        Assert.Equal(5, overdue.DaysOverdue);
        Assert.Equal(1.25m, overdue.FeeIfReturnedToday);
    }

    [Fact]
    public void Overdue_SeveralLoans_MostOverdueFirstAndReturnedLoansLeftOut()
    {
        var adult = _service.Borrow("M-100", "9780134685991").Loan!;     // due 15 June
        var student = _service.Borrow("M-200", "9780321125217").Loan!;   // due 22 June
        var returned = _service.Borrow("M-100", "9780135957059").Loan!;
        _service.Return(returned.Id);
        _clock.Today = new DateOnly(2026, 6, 30);

        var overdue = _service.Overdue();

        Assert.Equal([adult.Id, student.Id], overdue.Select(o => o.LoanId));
    }
}
