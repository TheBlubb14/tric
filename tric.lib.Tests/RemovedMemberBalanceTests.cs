using System.Text.Json;

namespace tric.lib.Tests;

public class RemovedMemberBalanceTests
{
    private static RegistryMembershipWrapper ActiveMember(string uuid)
        => new() { RegistryMembershipNonUser = new RegistryMembershipNonUser { Uuid = uuid } };

    private static RegistryMembershipWrapper RemovedMember(string uuid)
        => new()
        {
            RegistryMembershipNonUser = new RegistryMembershipNonUser
            {
                Uuid = uuid,
                Status = "INACTIVE",
                Remover = new Remover { DisplayName = "Alice", Uuid = "a" }
            }
        };

    private static Amount Eur(string value) => new() { Currency = "EUR", Value = value };

    private static RegistryEntryAllocation Share(RegistryMembershipWrapper member, string value)
        => new() { Membership = member, Amount = Eur(value), Type = "AMOUNT" };

    private static RegistryEntry Expense(RegistryMembershipWrapper owner, string total, params RegistryEntryAllocation[] allocations)
        => new()
        {
            Status = EntryStatus.Active,
            TypeTransaction = "NORMAL",
            Amount = Eur(total),
            MembershipOwned = owner,
            Allocations = [.. allocations]
        };

    [Test]
    public void WhenAllocatedMemberWasRemovedThenHisShareIsStillCounted()
    {
        var alice = ActiveMember("a");
        var bob = RemovedMember("b");
        var entry = Expense(alice, "-30.00", Share(alice, "-15.00"), Share(bob, "-15.00"));

        var balances = BalanceCalculator.Calculate([entry]);

        Assert.Multiple(() =>
        {
            Assert.That(balances["a"], Is.EqualTo(15m));
            Assert.That(balances["b"], Is.EqualTo(-15m));
        });
    }

    [Test]
    public void WhenOwnerWasRemovedThenHeIsStillCreditedForHisPayment()
    {
        var alice = ActiveMember("a");
        var bob = RemovedMember("b");
        var entry = Expense(bob, "-30.00", Share(alice, "-15.00"), Share(bob, "-15.00"));

        var balances = BalanceCalculator.Calculate([entry]);

        Assert.Multiple(() =>
        {
            Assert.That(balances["b"], Is.EqualTo(15m));
            Assert.That(balances["a"], Is.EqualTo(-15m));
        });
    }

    [Test]
    public void WhenMemberWasRemovedThenBalancesStillAddUpToZero()
    {
        var alice = ActiveMember("a");
        var bob = RemovedMember("b");
        var carol = ActiveMember("c");
        var entries = new[]
        {
            Expense(alice, "-30.00", Share(alice, "-10.00"), Share(bob, "-10.00"), Share(carol, "-10.00")),
            Expense(bob, "-12.00", Share(alice, "-6.00"), Share(bob, "-6.00"))
        };

        var balances = BalanceCalculator.Calculate(entries);

        Assert.Multiple(() =>
        {
            Assert.That(balances.Keys, Is.EquivalentTo(new[] { "a", "b", "c" }));
            Assert.That(balances.Values.Sum(), Is.Zero);
        });
    }

    [Test]
    public void WhenMemberWasRemovedThenHeStillAppearsInSettlements()
    {
        var alice = ActiveMember("a");
        var bob = RemovedMember("b");
        var entry = Expense(alice, "-30.00", Share(alice, "-15.00"), Share(bob, "-15.00"));

        var transfers = SettlementCalculator.Calculate(BalanceCalculator.Calculate([entry]));

        Assert.That(transfers, Is.EqualTo(new[] { new Transfer("b", "a", 15m) }));
    }

}
