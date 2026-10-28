namespace Tuki.Reportes.Dtos;

public record ReportPeriod(DateOnly From, DateOnly To);

public record AccountCashFlowReport(
    ReportPeriod Period,
    IReadOnlyList<AccountCashFlowItem> Accounts
);

public record AccountCashFlowItem(
    Guid AccountId,
    string? AccountName,
    string? AccountType,
    string? CurrencyCode,
    bool? IsArchived,
    decimal IncomeCents,
    decimal ExpenseCents,
    decimal TransfersInCents,
    decimal TransfersOutCents,
    decimal NetMovementCents
);

public record CategorySpendingReport(
    ReportPeriod Period,
    IReadOnlyList<CategorySpendingItem> Categories
);

public record CategorySpendingItem(
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryKind,
    Guid? ParentId,
    decimal ExpenseCents
);

public record MonthlyBudgetExecutionReport(
    int Year,
    int Month,
    IReadOnlyList<BudgetExecutionItem> Budgets
);

public record BudgetExecutionItem(
    Guid BudgetId,
    Guid? CategoryId,
    string? CategoryName,
    decimal BudgetCents,
    decimal ExpenseCents,
    decimal RemainingCents,
    decimal UsagePercentage,
    bool IsOverBudget
);

public record TransfersReport(ReportPeriod Period, IReadOnlyList<TransferItem> Transfers);

public record TransferItem(
    Guid TransactionId,
    DateOnly OccurredOn,
    string? Description,
    IReadOnlyList<TransferAccountItem> FromAccounts,
    IReadOnlyList<TransferAccountItem> ToAccounts
);

public record TransferAccountItem(
    Guid AccountId,
    string? AccountName,
    decimal AmountCents
);

public record CategorySpendingTrendReport(
    ReportPeriod Period,
    IReadOnlyList<CategorySpendingTrendItem> Items
);

public record CategorySpendingTrendItem(
    int Year,
    int Month,
    Guid? CategoryId,
    string? CategoryName,
    decimal ExpenseCents
);

public record PerformanceMetric(
    string Name,
    ReportPeriod Period,
    decimal? Value,
    string Unit,
    int? EvaluatedBudgets = null
);
