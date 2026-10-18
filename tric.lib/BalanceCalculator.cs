using System.Globalization;

namespace tric.lib;

/// <summary>
/// A suggested payment that settles part of the debts.
/// </summary>
public sealed record Transfer(string FromMembershipUuid, string ToMembershipUuid, decimal Amount);

/// <summary>
/// Calculates per member balances of a tricount. A positive balance means the member gets money back.
/// </summary>
public static class BalanceCalculator
{
    private const string TypeIncome = "INCOME";

    /// <summary>
    /// Calculates the balance per membership uuid from all active entries.
    /// Expenses and reimbursements credit the owner and debit the allocated members, income does the opposite.
    /// </summary>
    public static Dictionary<string, decimal> Calculate(IEnumerable<RegistryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var balances = new Dictionary<string, decimal>();

        foreach (var entry in entries.Where(IsActive))
        {
            var ownerKey = GetKey(entry.MembershipOwned);
            var total = Math.Abs(ParseAmount(entry.AmountLocal ?? entry.Amount));
            var sign = string.Equals(entry.TypeTransaction, TypeIncome, StringComparison.OrdinalIgnoreCase) ? -1 : 1;

            if (ownerKey is not null)
            {
                Add(balances, ownerKey, sign * total);
            }

            foreach (var (key, amount) in GetShares(entry, total))
            {
                Add(balances, key, -sign * amount);
            }
        }

        // Ratio shares are kept unrounded while summing; the app shows rounded balances.
        foreach (var key in balances.Keys.ToList())
        {
            balances[key] = Math.Round(balances[key], 2, MidpointRounding.AwayFromZero);
        }

        return balances;
    }

    /// <summary>
    /// Sum of all active normal expenses (what the group spent in total).
    /// </summary>
    public static decimal TotalExpenses(IEnumerable<RegistryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries
            .Where(IsActive)
            .Where(e => string.Equals(e.TypeTransaction, "NORMAL", StringComparison.OrdinalIgnoreCase))
            .Sum(e => Math.Abs(ParseAmount(e.AmountLocal ?? e.Amount)));
    }

    /// <summary>
    /// Parses a decimal amount from the api (invariant culture).
    /// </summary>
    public static decimal ParseAmount(Amount? amount)
        => decimal.TryParse(amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0m;

    /// <summary>
    /// Returns the uuid of a membership or its id as fallback.
    /// </summary>
    public static string? GetKey(RegistryMembershipWrapper? membership)
    {
        var member = membership?.RegistryMembershipNonUser;
        return member?.Uuid ?? member?.Id.ToString(CultureInfo.InvariantCulture);
    }

    private static bool IsActive(RegistryEntry entry)
        => entry.Status is null || entry.Status == EntryStatus.Active;

    private static IEnumerable<(string Key, decimal Amount)> GetShares(RegistryEntry entry, decimal total)
    {
        var allocations = (entry.Allocations ?? [])
            .Select(a => (Key: GetKey(a.Membership), Allocation: a))
            .Where(a => a.Key is not null)
            .ToList();

        static bool IsRatio(RegistryEntryAllocation a)
            => string.Equals(a.Type, "RATIO", StringComparison.OrdinalIgnoreCase) && a.ShareRatio is > 0m;

        // Like the Tricount app: fixed amounts are taken as they are, the remainder is split by ratio (unrounded).
        var totalRatio = allocations.Where(a => IsRatio(a.Allocation)).Sum(a => a.Allocation.ShareRatio!.Value);
        var fixedAmount = allocations.Where(a => !IsRatio(a.Allocation))
            .Sum(a => Math.Abs(ParseAmount(a.Allocation.AmountLocal ?? a.Allocation.Amount)));

        foreach (var (key, allocation) in allocations)
        {
            var amount = Math.Abs(ParseAmount(allocation.AmountLocal ?? allocation.Amount));

            if (IsRatio(allocation))
            {
                amount = (total - fixedAmount) * allocation.ShareRatio!.Value / totalRatio;
            }

            yield return (key!, amount);
        }
    }

    private static void Add(Dictionary<string, decimal> balances, string key, decimal amount)
        => balances[key] = balances.GetValueOrDefault(key) + amount;
}

/// <summary>
/// Calculates a small set of payments that settles all balances.
/// </summary>
public static class SettlementCalculator
{
    private const decimal Tolerance = 0.005m;

    /// <summary>
    /// Greedy matching of the biggest debtor with the biggest creditor.
    /// </summary>
    public static List<Transfer> Calculate(IReadOnlyDictionary<string, decimal> balances)
    {
        ArgumentNullException.ThrowIfNull(balances);

        var creditors = balances.Where(b => b.Value > Tolerance).OrderByDescending(b => b.Value).Select(b => (b.Key, Amount: b.Value)).ToList();
        var debtors = balances.Where(b => b.Value < -Tolerance).OrderBy(b => b.Value).Select(b => (b.Key, Amount: -b.Value)).ToList();

        var transfers = new List<Transfer>();
        int c = 0, d = 0;

        while (c < creditors.Count && d < debtors.Count)
        {
            var amount = Math.Min(creditors[c].Amount, debtors[d].Amount);
            transfers.Add(new Transfer(debtors[d].Key, creditors[c].Key, Math.Round(amount, 2, MidpointRounding.AwayFromZero)));

            creditors[c] = (creditors[c].Key, creditors[c].Amount - amount);
            debtors[d] = (debtors[d].Key, debtors[d].Amount - amount);

            if (creditors[c].Amount <= Tolerance)
            {
                c++;
            }

            if (debtors[d].Amount <= Tolerance)
            {
                d++;
            }
        }

        return transfers;
    }
}
