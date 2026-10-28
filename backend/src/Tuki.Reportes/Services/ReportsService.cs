using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tuki.Reportes.Dtos;

namespace Tuki.Reportes.Services;

public sealed class ReportsService(IHttpClientFactory clients)
{
    private const int PageSize = 200;
    private const string AccountsSource = "Cuentas";
    private const string MovementsSource = "Movimientos";
    private const string BudgetsSource = "Presupuestos";

    public async Task<AccountCashFlowReport> GetAccountCashFlowAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var accounts = await GetAccountsAsync(tenantId, ct);
        var linesByAccount = transactions
            .SelectMany(transaction =>
                transaction.Lines.Select(line => (Transaction: transaction, Line: line))
            )
            .GroupBy(item => item.Line.AccountId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var items = accounts
            .Select(account =>
            {
                decimal income = 0;
                decimal expenses = 0;
                decimal transfersIn = 0;
                decimal transfersOut = 0;

                if (linesByAccount.TryGetValue(account.Id, out var accountLines))
                {
                    foreach (var item in accountLines)
                    {
                        switch (item.Transaction.Type)
                        {
                            case "Income":
                                income += item.Line.AmountCents;
                                break;
                            case "Expense":
                                expenses += decimal.Abs(item.Line.AmountCents);
                                break;
                            case "Transfer" when item.Line.AmountCents > 0:
                                transfersIn += item.Line.AmountCents;
                                break;
                            case "Transfer" when item.Line.AmountCents < 0:
                                transfersOut += decimal.Abs(item.Line.AmountCents);
                                break;
                        }
                    }
                }

                return new AccountCashFlowItem(
                    account.Id,
                    account.Name,
                    account.AccountType,
                    account.CurrencyCode,
                    account.IsArchived,
                    income,
                    expenses,
                    transfersIn,
                    transfersOut,
                    income - expenses + transfersIn - transfersOut
                );
            })
            .OrderBy(item => item.AccountName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AccountCashFlowReport(period, items);
    }

    public async Task<CategorySpendingReport> GetCategorySpendingAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var categories = await GetCategoriesAsync(tenantId, ct);
        var categoriesById = categories.ToDictionary(category => category.Id);

        var items = transactions
            .Where(transaction => transaction.Type == "Expense")
            .SelectMany(transaction => transaction.Lines)
            .GroupBy(line => line.CategoryId)
            .Select(group =>
            {
                categoriesById.TryGetValue(group.Key ?? Guid.Empty, out var category);
                return new CategorySpendingItem(
                    group.Key,
                    category?.Name,
                    category?.Kind,
                    category?.ParentId,
                    group.Sum(line => decimal.Abs(line.AmountCents))
                );
            })
            .OrderByDescending(item => item.ExpenseCents)
            .ThenBy(item => item.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CategorySpendingReport(period, items);
    }

    public async Task<MonthlyBudgetExecutionReport> GetMonthlyBudgetExecutionAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken ct
    )
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var period = new ReportPeriod(from, to);
        var budgets = await GetBudgetsAsync(tenantId, ct);
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var categories = await GetCategoriesAsync(tenantId, ct);
        var categoriesById = categories.ToDictionary(category => category.Id);
        var expensesByCategory = transactions
            .Where(transaction => transaction.Type == "Expense")
            .SelectMany(transaction => transaction.Lines)
            .GroupBy(line => line.CategoryId ?? Guid.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(line => decimal.Abs(line.AmountCents))
            );
        var totalExpenses = expensesByCategory.Values.Sum();

        var items = budgets
            .Select(budget =>
            {
                var expense = budget.CategoryId is null
                    ? totalExpenses
                    : expensesByCategory.GetValueOrDefault(budget.CategoryId.Value);
                categoriesById.TryGetValue(budget.CategoryId ?? Guid.Empty, out var category);
                var usage = expense / budget.AmountCents * 100m;

                return new BudgetExecutionItem(
                    budget.Id,
                    budget.CategoryId,
                    category?.Name,
                    budget.AmountCents,
                    expense,
                    budget.AmountCents - expense,
                    decimal.Round(usage, 2, MidpointRounding.AwayFromZero),
                    expense > budget.AmountCents
                );
            })
            .OrderBy(item => item.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.BudgetId)
            .ToList();

        return new MonthlyBudgetExecutionReport(year, month, items);
    }

    public async Task<TransfersReport> GetTransfersAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var accounts = (await GetAccountsAsync(tenantId, ct)).ToDictionary(account => account.Id);

        var transfers = transactions
            .Where(transaction => transaction.Type == "Transfer")
            .Select(transaction =>
                new TransferItem(
                    transaction.Id,
                    transaction.OccurredOn,
                    transaction.Description,
                    transaction.Lines
                        .Where(line => line.AmountCents < 0)
                        .Select(line =>
                        {
                            accounts.TryGetValue(line.AccountId, out var account);
                            return new TransferAccountItem(
                                line.AccountId,
                                account?.Name,
                                decimal.Abs(line.AmountCents)
                            );
                        })
                        .ToList(),
                    transaction.Lines
                        .Where(line => line.AmountCents > 0)
                        .Select(line =>
                        {
                            accounts.TryGetValue(line.AccountId, out var account);
                            return new TransferAccountItem(
                                line.AccountId,
                                account?.Name,
                                line.AmountCents
                            );
                        })
                        .ToList()
                )
            )
            .OrderByDescending(transfer => transfer.OccurredOn)
            .ThenBy(transfer => transfer.TransactionId)
            .ToList();

        return new TransfersReport(period, transfers);
    }

    public async Task<CategorySpendingTrendReport> GetCategorySpendingTrendAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var categories = (await GetCategoriesAsync(tenantId, ct)).ToDictionary(
            category => category.Id
        );

        var items = transactions
            .Where(transaction => transaction.Type == "Expense")
            .SelectMany(transaction =>
                transaction.Lines.Select(line => new
                {
                    Month = new DateOnly(transaction.OccurredOn.Year, transaction.OccurredOn.Month, 1),
                    line.CategoryId,
                    line.AmountCents,
                })
            )
            .GroupBy(item => new { item.Month, item.CategoryId })
            .Select(group =>
            {
                categories.TryGetValue(group.Key.CategoryId ?? Guid.Empty, out var category);
                return new CategorySpendingTrendItem(
                    group.Key.Month.Year,
                    group.Key.Month.Month,
                    group.Key.CategoryId,
                    category?.Name,
                    group.Sum(item => decimal.Abs(item.AmountCents))
                );
            })
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ThenBy(item => item.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CategorySpendingTrendReport(period, items);
    }

    public async Task<PerformanceMetric> GetIncomeAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var cents = transactions
            .Where(transaction => transaction.Type == "Income")
            .SelectMany(transaction => transaction.Lines)
            .Sum(line => (decimal)line.AmountCents);

        return MoneyMetric("income", period, cents);
    }

    public async Task<PerformanceMetric> GetExpensesAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var cents = transactions
            .Where(transaction => transaction.Type == "Expense")
            .SelectMany(transaction => transaction.Lines)
            .Sum(line => decimal.Abs(line.AmountCents));

        return MoneyMetric("expenses", period, cents);
    }

    public async Task<PerformanceMetric> GetNetCashFlowAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var transactions = await GetTransactionsAsync(tenantId, period, ct);
        var income = transactions
            .Where(transaction => transaction.Type == "Income")
            .SelectMany(transaction => transaction.Lines)
            .Sum(line => (decimal)line.AmountCents);
        var expenses = transactions
            .Where(transaction => transaction.Type == "Expense")
            .SelectMany(transaction => transaction.Lines)
            .Sum(line => decimal.Abs(line.AmountCents));

        return MoneyMetric("netCashFlow", period, income - expenses);
    }

    public async Task<PerformanceMetric> GetAverageDailyExpenseAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var expenses = await GetExpensesAsync(tenantId, period, ct);
        var days = period.To.DayNumber - period.From.DayNumber + 1;
        return expenses with
        {
            Name = "averageDailyExpense",
            Value = expenses.Value / days,
            Unit = "BOB por día",
        };
    }

    public async Task<PerformanceMetric> GetBudgetAdherenceAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken ct
    )
    {
        var execution = await GetMonthlyBudgetExecutionAsync(tenantId, year, month, ct);
        var budgets = execution.Budgets;
        decimal? adherence = budgets.Count == 0
            ? null
            : decimal.Round(
                budgets.Count(budget => !budget.IsOverBudget) * 100m / budgets.Count,
                2,
                MidpointRounding.AwayFromZero
            );

        return new PerformanceMetric(
            "budgetAdherence",
            new ReportPeriod(new DateOnly(year, month, 1), new DateOnly(year, month, 1).AddMonths(1).AddDays(-1)),
            adherence,
            "porcentaje de presupuestos dentro del límite",
            budgets.Count
        );
    }

    private async Task<List<TransactionRecord>> GetTransactionsAsync(
        Guid tenantId,
        ReportPeriod period,
        CancellationToken ct
    )
    {
        var items = new List<TransactionRecord>();
        var pageNumber = 1;
        var expectedTotal = int.MaxValue;

        while (items.Count < expectedTotal)
        {
            var path = string.Create(
                CultureInfo.InvariantCulture,
                $"api/movimientos/transactions?from={period.From:yyyy-MM-dd}&to={period.To:yyyy-MM-dd}&page={pageNumber}&pageSize={PageSize}"
            );
            var page = await GetAsync<TransactionPage>(
                MovementsSource,
                path,
                tenantId,
                ct
            );

            if (
                page.Page != pageNumber
                || page.PageSize != PageSize
                || page.Total < 0
                || page.Items.Count > PageSize
                || items.Count + page.Items.Count > page.Total
            )
            {
                throw new ReportDataSourceException(MovementsSource, HttpStatusCode.BadGateway);
            }

            if (page.Items.Count == 0 && items.Count < page.Total)
            {
                throw new ReportDataSourceException(MovementsSource, HttpStatusCode.BadGateway);
            }

            items.AddRange(page.Items);
            expectedTotal = page.Total;
            if (items.Count >= expectedTotal)
            {
                break;
            }

            pageNumber++;
        }

        return items;
    }

    private Task<List<AccountRecord>> GetAccountsAsync(Guid tenantId, CancellationToken ct) =>
        GetAsync<List<AccountRecord>>(
            AccountsSource,
            "api/cuentas/accounts?includeArchived=true",
            tenantId,
            ct
        );

    private Task<List<CategoryRecord>> GetCategoriesAsync(Guid tenantId, CancellationToken ct) =>
        GetAsync<List<CategoryRecord>>(
            AccountsSource,
            "api/cuentas/categories?includeArchived=true",
            tenantId,
            ct
        );

    private Task<List<BudgetRecord>> GetBudgetsAsync(Guid tenantId, CancellationToken ct) =>
        GetAsync<List<BudgetRecord>>(
            BudgetsSource,
            "api/presupuestos/budgets?period=Monthly",
            tenantId,
            ct
        );

    private async Task<T> GetAsync<T>(
        string source,
        string path,
        Guid tenantId,
        CancellationToken ct
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());

        HttpResponseMessage response;
        try
        {
            response = await clients.CreateClient(source).SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            );
        }
        catch (HttpRequestException exception)
        {
            throw new ReportDataSourceException(source, exception.StatusCode, exception);
        }
        catch (TaskCanceledException exception) when (!ct.IsCancellationRequested)
        {
            throw new ReportDataSourceException(source, null, exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ReportDataSourceException(source, response.StatusCode);
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
                    ?? throw new JsonException("La respuesta no contiene datos.");
            }
            catch (JsonException exception)
            {
                throw new ReportDataSourceException(source, response.StatusCode, exception);
            }
        }
    }

    private static PerformanceMetric MoneyMetric(
        string name,
        ReportPeriod period,
        decimal cents
    ) => new(name, period, cents / 100m, "BOB");

    private sealed record TransactionPage(
        List<TransactionRecord> Items,
        int Page,
        int PageSize,
        int Total
    );

    private sealed record TransactionRecord(
        Guid Id,
        string Type,
        DateOnly OccurredOn,
        string? Description,
        List<TransactionLineRecord> Lines,
        List<Guid> TagIds
    );

    private sealed record TransactionLineRecord(
        Guid Id,
        Guid AccountId,
        Guid? CategoryId,
        long AmountCents
    );

    private sealed record AccountRecord(
        Guid Id,
        Guid AccountTypeId,
        string AccountType,
        string Name,
        string CurrencyCode,
        bool IsDefault,
        bool IsArchived,
        DateTimeOffset CreatedAt
    );

    private sealed record CategoryRecord(
        Guid Id,
        Guid? ParentId,
        string Name,
        string Kind,
        bool IsArchived,
        DateTimeOffset CreatedAt
    );

    private sealed record BudgetRecord(
        Guid Id,
        Guid? CategoryId,
        long AmountCents,
        string Period,
        bool IsArchived,
        DateTimeOffset CreatedAt
    );
}
