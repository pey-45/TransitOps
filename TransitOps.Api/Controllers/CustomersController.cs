using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Common;
using TransitOps.Api.Features.Customers;
using TransitOps.Api.Security;

namespace TransitOps.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Policy = Policies.Operational)]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerResponse>>>> GetAll(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<CustomerResponse>>.Success(await service.GetAllAsync(cancellationToken), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(id, cancellationToken)
            ?? throw new ApiException(404, "customer_not_found", "El cliente no existe o está dado de baja.");
        return Ok(ApiResponse<CustomerResponse>.Success(item, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create(UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<CustomerResponse>.Success(item, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Update(Guid id, UpsertCustomerRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CustomerResponse>.Success(await service.UpdateAsync(id, request, cancellationToken), HttpContext.TraceIdentifier));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { deactivated = true }, HttpContext.TraceIdentifier));
    }
}
