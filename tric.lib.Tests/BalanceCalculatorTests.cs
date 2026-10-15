using tric.lib;

namespace tric.lib.Tests;

public class BalanceCalculatorTests
{
    private static RegistryMembershipWrapper Member(string uuid)
        => new() { RegistryMembershipNonUser = new RegistryMembershipNonUser { Uuid = uuid } };

    private static RegistryMembershipWrapper DeletedMember(string uuid)
        => new() { RegistryMembershipNonUser = new RegistryMembershipNonUser { Uuid = uuid, Remover = new Remover { DisplayName = "Deleter", Uuid = uuid } } };

    private static Amount Eur(string value) => new() { Currency = "EUR", Value = value };

    private static RegistryEntry Expense(string owner, string total, params (string Member, string Amount)[] shares)
        => new()
        {
            Status = EntryStatus.Active,
            TypeTransaction = "NORMAL",
            Amount = Eur(total),
            MembershipOwned = Member(owner),
            Allocations = [.. shares.Select(s => new RegistryEntryAllocation { Membership = Member(s.Member), Amount = Eur(s.Amount), Type = "AMOUNT" })]
        };

    [Test]
    public void WhenExpenseIsSplitEquallyThenPayerIsCreditor()
    {
        var entries = new[] { Expense("a", "-30.00", ("a", "-10.00"), ("b", "-10.00"), ("c", "-10.00")) };

        var balances = BalanceCalculator.Calculate(entries);

        Assert.That(balances["a"], Is.EqualTo(20m));
    }

    [Test]
    public void WhenExpenseIsSplitEquallyThenParticipantOwesShare()
    {
        var entries = new[] { Expense("a", "30.00", ("a", "10.00"), ("b", "10.00"), ("c", "10.00")) };

        var balances = BalanceCalculator.Calculate(entries);

        Assert.That(balances["b"], Is.EqualTo(-10m));
    }

    [Test]
    public void WhenEntryIsInactiveThenItIsIgnored()
    {
        var entry = Expense("a", "30.00", ("b", "30.00"));
        entry.Status = EntryStatus.Inactive;

        var balances = BalanceCalculator.Calculate([entry]);

        Assert.That(balances, Is.Empty);
    }

    [Test]
    public void WhenAllocationUsesRatioThenAmountIsDerivedFromRatio()
    {
        var entry = Expense("a", "30.00");
        entry.Allocations =
        [
            new RegistryEntryAllocation { Membership = Member("a"), Type = "RATIO", ShareRatio = 1 },
            new RegistryEntryAllocation { Membership = Member("b"), Type = "RATIO", ShareRatio = 2 }
        ];

        var balances = BalanceCalculator.Calculate([entry]);

        Assert.That(balances["b"], Is.EqualTo(-20m));
    }

    [Test]
    public void WhenEntryIsIncomeThenOwnerOwesMoney()
    {
        var entry = Expense("a", "30.00", ("b", "30.00"));
        entry.TypeTransaction = "INCOME";

        var balances = BalanceCalculator.Calculate([entry]);

        Assert.That(balances["a"], Is.EqualTo(-30m));
    }

    [Test]
    public void WhenEntriesContainReimbursementThenTotalExpensesIgnoreIt()
    {
        var reimbursement = Expense("b", "10.00", ("a", "10.00"));
        reimbursement.TypeTransaction = "BALANCE";

        var total = BalanceCalculator.TotalExpenses([Expense("a", "30.00", ("b", "30.00")), reimbursement]);

        Assert.That(total, Is.EqualTo(30m));
    }
}

public class SettlementCalculatorTests
{
    [Test]
    public void WhenOneDebtorAndOneCreditorThenSingleTransfer()
    {
        var transfers = SettlementCalculator.Calculate(new Dictionary<string, decimal> { ["a"] = 10m, ["b"] = -10m });

        Assert.That(transfers, Is.EqualTo(new[] { new Transfer("b", "a", 10m) }));
    }

    [Test]
    public void WhenBalancesAreSettledThenNoTransfers()
    {
        var transfers = SettlementCalculator.Calculate(new Dictionary<string, decimal> { ["a"] = 0m, ["b"] = 0m });

        Assert.That(transfers, Is.Empty);
    }

    [Test]
    public void WhenTwoDebtorsOweOneCreditorThenTwoTransfers()
    {
        var transfers = SettlementCalculator.Calculate(new Dictionary<string, decimal> { ["a"] = 20m, ["b"] = -10m, ["c"] = -10m });

        Assert.That(transfers, Has.Count.EqualTo(2));
    }

    [Test]
    public void WhenTransfersAreAppliedThenAllBalancesAreZero()
    {
        var balances = new Dictionary<string, decimal> { ["a"] = 25m, ["b"] = 5m, ["c"] = -12.5m, ["d"] = -17.5m };

        var transfers = SettlementCalculator.Calculate(balances);

        foreach (var t in transfers)
        {
            balances[t.FromMembershipUuid] += t.Amount;
            balances[t.ToMembershipUuid] -= t.Amount;
        }

        Assert.That(balances.Values, Is.All.EqualTo(0m));
    }
}

