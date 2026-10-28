using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Tuki.Reportes.Dtos;
using Tuki.Reportes.Services;

namespace Tuki.Reportes.Controllers;

[ApiController]
[Route("api/reportes/reports")]
public sealed class ReportsController(
    ReportsService reports,
    ILogger<ReportsController> logger
) : ControllerBase
{
    private const int MaximumPeriodDays = 366;

    [HttpGet("cash-flow/accounts")]
    public Task<IActionResult> GetAccountCashFlow(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    )
    {
        if (!TryGetTenant(out var tenantId, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!TryCreatePeriod(from, to, out var period, out error))
        {
            return Task.FromResult(error!);
        }

        return ExecuteAsync(() => reports.GetAccountCashFlowAsync(tenantId, period, ct));
    }

    [HttpGet("spending/categories")]
    public Task<IActionResult> GetCategorySpending(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    )
    {
        if (!TryGetTenant(out var tenantId, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!TryCreatePeriod(from, to, out var period, out error))
        {
            return Task.FromResult(error!);
        }

        return ExecuteAsync(() => reports.GetCategorySpendingAsync(tenantId, period, ct));
    }

    [HttpGet("budgets/execution")]
    public Task<IActionResult> GetMonthlyBudgetExecution(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken ct
    )
    {
        if (!TryGetTenant(out var tenantId, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!TryGetMonth(year, month, out var selectedYear, out var selectedMonth, out error))
        {
            return Task.FromResult(error!);
        }

        return ExecuteAsync(() =>
            reports.GetMonthlyBudgetExecutionAsync(tenantId, selectedYear, selectedMonth, ct)
        );
    }

    [HttpGet("transfers")]
    public Task<IActionResult> GetTransfers(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    )
    {
        if (!TryGetTenant(out var tenantId, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!TryCreatePeriod(from, to, out var period, out error))
        {
            return Task.FromResult(error!);
        }

        return ExecuteAsync(() => reports.GetTransfersAsync(tenantId, period, ct));
    }

    [HttpGet("spending/categories/trend")]
    public Task<IActionResult> GetCategorySpendingTrend(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    )
    {
        if (!TryGetTenant(out var tenantId, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!TryCreatePeriod(from, to, out var period, out error, defaultMonths: 12))
        {
            return Task.FromResult(error!);
        }

        return ExecuteAsync(() => reports.GetCategorySpendingTrendAsync(tenantId, period, ct));
    }

    [HttpGet("performance/income")]
    public Task<IActionResult> GetIncome(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    ) => RunForPeriodAsync(from, to, reports.GetIncomeAsync, ct);

    [HttpGet("performance/expenses")]
    public Task<IActionResult> GetExpenses(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    ) => RunForPeriodAsync(from, to, reports.GetExpensesAsync, ct);

    [HttpGet("performance/net-cash-flow")]
    public Task<IActionResult> GetNetCashFlow(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    ) => RunForPeriodAsync(from, to, reports.GetNetCashFlowAsync, ct);

    [HttpGet("performance/average-daily-expense")]
    public Task<IActionResult> GetAverageDailyExpense(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct
    ) => RunForPeriodAsync(from, to, reports.GetAverageDailyExpenseAsync, ct);

    [HttpGet("performance/budget-adherence")]
    public Task<IActionResult> GetBudgetAdherence(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken ct
    )
    {
        if (!TryGetTenant(out var tenantId, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!TryGetMonth(year, month, out var selectedYear, out var selectedMonth, out error))
        {
            return Task.FromResult(error!);
        }

        return ExecuteAsync(() =>
            reports.GetBudgetAdherenceAsync(tenantId, selectedYear, selectedMonth, ct)
        );
    }

    private Task<IActionResult> RunForPeriodAsync(
        DateOnly? from,
        DateOnly? to,
        Func<Guid, ReportPeriod, CancellationToken, Task<PerformanceMetric>> query,
        CancellationToken ct
    )
    {
        if (!TryGetTenant(out var tenantId, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!TryCreatePeriod(from, to, out var period, out error))
        {
            return Task.FromResult(error!);
        }

        return ExecuteAsync(() => query(tenantId, period, ct));
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> query)
    {
        try
        {
            return Ok(await query());
        }
        catch (ReportDataSourceException exception)
        {
            logger.LogError(
                exception,
                "No se pudo completar un reporte porque falló la consulta a {Source}.",
                exception.ServiceName
            );
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    error = "No se pudo completar el reporte porque una fuente de datos no está disponible.",
                    fuente = exception.ServiceName,
                }
            );
        }
    }

    private bool TryGetTenant(out Guid tenantId, out IActionResult? error)
    {
        var header = Request.Headers["X-Tenant-Id"];
        if (
            header.Count != 1
            || !Guid.TryParse(header[0], out tenantId)
            || tenantId == Guid.Empty
        )
        {
            tenantId = Guid.Empty;
            error = BadRequest(new { error = "Debes enviar un X-Tenant-Id válido." });
            return false;
        }

        error = null;
        return true;
    }

    private bool TryCreatePeriod(
        DateOnly? from,
        DateOnly? to,
        out ReportPeriod period,
        out IActionResult? error,
        int defaultMonths = 1
    )
    {
        if (from is null && to is null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentMonth = new DateOnly(today.Year, today.Month, 1);
            from = currentMonth.AddMonths(1 - defaultMonths);
            to = currentMonth.AddMonths(1).AddDays(-1);
        }
        else if (from is null || to is null)
        {
            period = default!;
            error = BadRequest(new { error = "Debes enviar ambos parámetros: from y to." });
            return false;
        }

        if (to!.Value < from!.Value)
        {
            period = default!;
            error = BadRequest(new { error = "La fecha to no puede ser anterior a from." });
            return false;
        }

        if (to.Value.DayNumber - from.Value.DayNumber >= MaximumPeriodDays)
        {
            period = default!;
            error = BadRequest(
                new { error = $"El período no puede superar {MaximumPeriodDays} días." }
            );
            return false;
        }

        period = new ReportPeriod(from.Value, to.Value);
        error = null;
        return true;
    }

    private bool TryGetMonth(
        int? year,
        int? month,
        out int selectedYear,
        out int selectedMonth,
        out IActionResult? error
    )
    {
        if (year is null && month is null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            selectedYear = today.Year;
            selectedMonth = today.Month;
        }
        else if (year is null || month is null)
        {
            selectedYear = 0;
            selectedMonth = 0;
            error = BadRequest(new { error = "Debes enviar juntos los parámetros year y month." });
            return false;
        }
        else
        {
            selectedYear = year.Value;
            selectedMonth = month.Value;
        }

        if (
            selectedYear < 1
            || selectedYear > 9999
            || selectedMonth < 1
            || selectedMonth > 12
        )
        {
            error = BadRequest(
                new
                {
                    error = "El año o mes no es válido.",
                    formato = "year=2026&month=10",
                }
            );
            return false;
        }

        error = null;
        return true;
    }
}
