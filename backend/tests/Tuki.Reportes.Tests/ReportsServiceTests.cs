using System.Net;
using System.Text;
using Tuki.Reportes.Dtos;
using Tuki.Reportes.Services;
using Xunit;

namespace Tuki.Reportes.Tests;

public sealed class ReportsServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid AccountOne = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid AccountTwo = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid CategoryId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid BudgetId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid GlobalBudgetId = Guid.Parse("55555555-5555-4555-8555-555555555555");
    private static readonly Guid ExpenseId = Guid.Parse("66666666-6666-4666-8666-666666666666");
    private static readonly Guid IncomeId = Guid.Parse("77777777-7777-4777-8777-777777777777");
    private static readonly Guid TransferId = Guid.Parse("88888888-8888-4888-8888-888888888888");
    private static readonly Guid OpeningId = Guid.Parse("99999999-9999-4999-8999-999999999999");
    private static readonly ReportPeriod October = new(
        new DateOnly(2026, 10, 1),
        new DateOnly(2026, 10, 31)
    );

    [Fact]
    public async Task AccountCashFlow_CombinesMovementLinesWithAccountDetailsAndKeepsOpeningSeparate()
    {
        var (service, handler) = CreateService();

        var report = await service.GetAccountCashFlowAsync(TenantId, October, CancellationToken.None);

        var cash = Assert.Single(report.Accounts, item => item.AccountId == AccountOne);
        Assert.Equal("Caja", cash.AccountName);
        Assert.Equal(10_000m, cash.IncomeCents);
        Assert.Equal(2_500m, cash.ExpenseCents);
        Assert.Equal(0m, cash.TransfersInCents);
        Assert.Equal(1_000m, cash.TransfersOutCents);
        Assert.Equal(6_500m, cash.NetMovementCents);
        Assert.All(handler.TenantHeaders, value => Assert.Equal(TenantId.ToString(), value));
    }

    [Fact]
    public async Task CategorySpending_EnrichesExpenseWithCategoryName()
    {
        var (service, _) = CreateService();

        var report = await service.GetCategorySpendingAsync(TenantId, October, CancellationToken.None);

        var category = Assert.Single(report.Categories);
        Assert.Equal(CategoryId, category.CategoryId);
        Assert.Equal("Ingredientes", category.CategoryName);
        Assert.Equal("Expense", category.CategoryKind);
        Assert.Equal(2_500m, category.ExpenseCents);
    }

    [Fact]
    public async Task BudgetExecution_CombinesBudgetsCategoriesAndActualExpenses()
    {
        var (service, _) = CreateService();

        var report = await service.GetMonthlyBudgetExecutionAsync(
            TenantId,
            2026,
            10,
            CancellationToken.None
        );

        var categoryBudget = Assert.Single(report.Budgets, item => item.BudgetId == BudgetId);
        Assert.Equal("Ingredientes", categoryBudget.CategoryName);
        Assert.Equal(5_000m, categoryBudget.BudgetCents);
        Assert.Equal(2_500m, categoryBudget.ExpenseCents);
        Assert.Equal(2_500m, categoryBudget.RemainingCents);
        Assert.Equal(50m, categoryBudget.UsagePercentage);
        Assert.False(categoryBudget.IsOverBudget);

        var globalBudget = Assert.Single(report.Budgets, item => item.BudgetId == GlobalBudgetId);
        Assert.Equal(2_500m, globalBudget.ExpenseCents);
    }

    [Fact]
    public async Task Transfers_EnrichesBothSidesWithAccountNames()
    {
        var (service, _) = CreateService();

        var report = await service.GetTransfersAsync(TenantId, October, CancellationToken.None);

        var transfer = Assert.Single(report.Transfers);
        Assert.Equal(TransferId, transfer.TransactionId);
        Assert.Equal("Caja", Assert.Single(transfer.FromAccounts).AccountName);
        Assert.Equal(1_000m, transfer.FromAccounts[0].AmountCents);
        Assert.Equal("Ahorro", Assert.Single(transfer.ToAccounts).AccountName);
        Assert.Equal(1_000m, transfer.ToAccounts[0].AmountCents);
    }

    [Fact]
    public async Task CategoryTrend_GroupsExpensesByMonthAndCategory()
    {
        var (service, _) = CreateService();

        var report = await service.GetCategorySpendingTrendAsync(
            TenantId,
            October,
            CancellationToken.None
        );

        var item = Assert.Single(report.Items);
        Assert.Equal(2026, item.Year);
        Assert.Equal(10, item.Month);
        Assert.Equal(CategoryId, item.CategoryId);
        Assert.Equal("Ingredientes", item.CategoryName);
        Assert.Equal(2_500m, item.ExpenseCents);
    }

    [Fact]
    public async Task PerformanceMetrics_ReturnExpectedValuesForSampleData()
    {
        var (service, _) = CreateService();
        var fourDays = new ReportPeriod(
            new DateOnly(2026, 10, 10),
            new DateOnly(2026, 10, 13)
        );

        var income = await service.GetIncomeAsync(TenantId, fourDays, CancellationToken.None);
        var expenses = await service.GetExpensesAsync(TenantId, fourDays, CancellationToken.None);
        var net = await service.GetNetCashFlowAsync(TenantId, fourDays, CancellationToken.None);
        var average = await service.GetAverageDailyExpenseAsync(
            TenantId,
            fourDays,
            CancellationToken.None
        );
        var adherence = await service.GetBudgetAdherenceAsync(
            TenantId,
            2026,
            10,
            CancellationToken.None
        );

        Assert.Equal(100m, income.Value);
        Assert.Equal("BOB", income.Unit);
        Assert.Equal(25m, expenses.Value);
        Assert.Equal(75m, net.Value);
        Assert.Equal(6.25m, average.Value);
        Assert.Equal("BOB por día", average.Unit);
        Assert.Equal(100m, adherence.Value);
        Assert.Equal(2, adherence.EvaluatedBudgets);
    }

    [Fact]
    public async Task FailedSource_IsSurfacedInsteadOfReturningPartialReport()
    {
        var (service, _) = CreateService(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        );

        var exception = await Assert.ThrowsAsync<ReportDataSourceException>(() =>
            service.GetAccountCashFlowAsync(TenantId, October, CancellationToken.None)
        );

        Assert.Equal("Movimientos", exception.ServiceName);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    private static (ReportsService Service, RecordingHandler Handler) CreateService(
        Func<HttpRequestMessage, HttpResponseMessage>? overrideResponse = null
    )
    {
        var handler = new RecordingHandler(overrideResponse);
        var factory = new TestHttpClientFactory(handler);
        return (new ReportsService(factory), handler);
    }

    private sealed class TestHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false)
            {
                BaseAddress = new Uri($"https://{name.ToLowerInvariant()}.test/"),
            };
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage>? overrideResponse
    ) : HttpMessageHandler
    {
        public List<string> TenantHeaders { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            TenantHeaders.Add(request.Headers.GetValues("X-Tenant-Id").Single());
            if (overrideResponse is not null)
            {
                return Task.FromResult(overrideResponse(request));
            }

            var path = request.RequestUri!.AbsolutePath;
            var body = path switch
            {
                "/api/cuentas/accounts" => AccountsJson,
                "/api/cuentas/categories" => CategoriesJson,
                "/api/presupuestos/budgets" => BudgetsJson,
                "/api/movimientos/transactions" => TransactionsJson,
                _ => throw new InvalidOperationException($"Unexpected request path: {path}"),
            };

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
        }

        private static string AccountsJson =>
            $$"""
            [
              {
                "id": "{{AccountOne}}",
                "accountTypeId": "aaaaaaaa-0000-4000-8000-000000000001",
                "accountType": "Efectivo",
                "name": "Caja",
                "currencyCode": "BOB",
                "isDefault": true,
                "isArchived": false,
                "createdAt": "2026-10-01T00:00:00Z"
              },
              {
                "id": "{{AccountTwo}}",
                "accountTypeId": "aaaaaaaa-0000-4000-8000-000000000002",
                "accountType": "Ahorro",
                "name": "Ahorro",
                "currencyCode": "BOB",
                "isDefault": false,
                "isArchived": false,
                "createdAt": "2026-10-01T00:00:00Z"
              }
            ]
            """;

        private static string CategoriesJson =>
            $$"""
            [
              {
                "id": "{{CategoryId}}",
                "parentId": null,
                "name": "Ingredientes",
                "kind": "Expense",
                "isArchived": false,
                "createdAt": "2026-10-01T00:00:00Z"
              }
            ]
            """;

        private static string BudgetsJson =>
            $$"""
            [
              {
                "id": "{{BudgetId}}",
                "categoryId": "{{CategoryId}}",
                "amountCents": 5000,
                "period": "Monthly",
                "isArchived": false,
                "createdAt": "2026-10-01T00:00:00Z"
              },
              {
                "id": "{{GlobalBudgetId}}",
                "categoryId": null,
                "amountCents": 10000,
                "period": "Monthly",
                "isArchived": false,
                "createdAt": "2026-10-01T00:00:00Z"
              }
            ]
            """;

        private static string TransactionsJson =>
            $$"""
            {
              "items": [
                {
                  "id": "{{ExpenseId}}",
                  "type": "Expense",
                  "occurredOn": "2026-10-10",
                  "description": "Compra de harina",
                  "createdAt": "2026-10-10T12:00:00Z",
                  "updatedAt": "2026-10-10T12:00:00Z",
                  "lines": [
                    {
                      "id": "bbbbbbbb-0000-4000-8000-000000000001",
                      "accountId": "{{AccountOne}}",
                      "categoryId": "{{CategoryId}}",
                      "amountCents": -2500
                    }
                  ],
                  "tagIds": []
                },
                {
                  "id": "{{IncomeId}}",
                  "type": "Income",
                  "occurredOn": "2026-10-11",
                  "description": "Venta de torta",
                  "createdAt": "2026-10-11T12:00:00Z",
                  "updatedAt": "2026-10-11T12:00:00Z",
                  "lines": [
                    {
                      "id": "bbbbbbbb-0000-4000-8000-000000000002",
                      "accountId": "{{AccountOne}}",
                      "categoryId": "{{CategoryId}}",
                      "amountCents": 10000
                    }
                  ],
                  "tagIds": []
                },
                {
                  "id": "{{TransferId}}",
                  "type": "Transfer",
                  "occurredOn": "2026-10-12",
                  "description": "Ahorro",
                  "createdAt": "2026-10-12T12:00:00Z",
                  "updatedAt": "2026-10-12T12:00:00Z",
                  "lines": [
                    {
                      "id": "bbbbbbbb-0000-4000-8000-000000000003",
                      "accountId": "{{AccountOne}}",
                      "categoryId": null,
                      "amountCents": -1000
                    },
                    {
                      "id": "bbbbbbbb-0000-4000-8000-000000000004",
                      "accountId": "{{AccountTwo}}",
                      "categoryId": null,
                      "amountCents": 1000
                    }
                  ],
                  "tagIds": []
                },
                {
                  "id": "{{OpeningId}}",
                  "type": "Opening",
                  "occurredOn": "2026-10-01",
                  "description": "Saldo inicial",
                  "createdAt": "2026-10-01T12:00:00Z",
                  "updatedAt": "2026-10-01T12:00:00Z",
                  "lines": [
                    {
                      "id": "bbbbbbbb-0000-4000-8000-000000000005",
                      "accountId": "{{AccountOne}}",
                      "categoryId": null,
                      "amountCents": 50000
                    }
                  ],
                  "tagIds": []
                }
              ],
              "page": 1,
              "pageSize": 200,
              "total": 4
            }
            """;
    }
}
