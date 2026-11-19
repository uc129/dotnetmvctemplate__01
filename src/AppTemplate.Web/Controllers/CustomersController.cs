using AppTemplate.Application.Common;
using AppTemplate.Application.Customers;
using AppTemplate.Web.Common.DataTables;
using AppTemplate.Web.Common.Errors;
using AppTemplate.Web.Models.Customers;
using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.Web.Controllers;

/// <summary>
/// Thin by rule: bind input, call the service, choose a response. No SQL, no business rules,
/// no try/catch (GlobalExceptionHandler deals with the unexpected).
/// </summary>
public sealed class CustomersController(CustomerService customers, BusinessClock clock) : Controller
{
    public IActionResult Index() => View();

    /// <summary>Server-side DataTables endpoint. POST because DataTables sends a JSON body.</summary>
    [HttpPost]
    public async Task<IActionResult> List([FromBody] DataTableRequest? request, CancellationToken cancellationToken)
    {
        // A malformed body leaves request null and ModelState invalid: that is the caller's mistake (400), not ours (500).
        if (request is null || !ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var page = await customers.GetPagedAsync(request.ToPageRequest(), cancellationToken);
        return Json(DataTableResponse<CustomerListItem>.From(request.Draw, page));
    }

    public IActionResult Create() => View(new CustomerFormModel { CustomerSince = clock.Today });

    [HttpPost]
    public async Task<IActionResult> Create(CustomerFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await customers.CreateAsync(model.ToCreateInput(), cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddErrors(result.Errors);
            return View(model);
        }

        TempData["Success"] = "Customer created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await customers.GetAsync(id, cancellationToken);
        return result.IsSuccess ? View(CustomerFormModel.From(result.Value)) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CustomerFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await customers.UpdateAsync(id, model.ToUpdateInput(), cancellationToken);
        if (result.IsNotFound())
        {
            return NotFound();
        }

        if (result.IsFailure)
        {
            ModelState.AddErrors(result.Errors);
            return View(model);
        }

        TempData["Success"] = "Customer saved.";
        return RedirectToAction(nameof(Index));
    }
}
