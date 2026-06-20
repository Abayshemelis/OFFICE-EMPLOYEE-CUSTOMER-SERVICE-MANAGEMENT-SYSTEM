using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OECSMS.Contracts.DTOs;
using OECSMS.Contracts;
using OECSMS.Domain.Enums;

namespace OECSMS.API.Controllers
{
    [ApiController]
    [Route("customers")]
    public class CustomersController : BaseApiController
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterArrival([FromBody] RegisterCustomerRequest request)
        {
            var response = await _customerService.RegisterCustomerArrivalAsync(request);
            return ApiResponse(response, "Arrival registered successfully.");
        }

        [HttpGet("queue")]
        [Authorize(Roles = "Assistant")]
        public async Task<IActionResult> GetQueue()
        {
            var queue = await _customerService.GetActiveQueueAsync();
            return ApiResponse(queue, "Active queue retrieved successfully.");
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Assistant,Manager")]
        public async Task<IActionResult> GetCustomerDetails(int id)
        {
            var details = await _customerService.GetAllServiceRequestsAsync(assistantId: CurrentUserRole == "Assistant" ? CurrentUserId : (int?)null);
            var customerHistory = details.Where(d => d.CustomerId == id);
            return ApiResponse(customerHistory, "Customer history retrieved.");
        }

        // Service Request Endpoints (routed under api/v1/service-requests)

        [HttpPost("~/service-requests")]
        [Authorize(Roles = "Assistant")]
        public async Task<IActionResult> OpenServiceRequest([FromBody] RegisterCustomerRequest request)
        {
            // If assistant opens request manually
            var response = await _customerService.RegisterCustomerArrivalAsync(request);
            return ApiResponse(response, "Service request opened successfully.");
        }

        [HttpPatch("~/service-requests/{id}/status")]
        [Authorize(Roles = "Assistant")]
        public async Task<IActionResult> UpdateServiceRequestStatus(int id, [FromBody] UpdateServiceRequestStatusRequest request)
        {
            var response = await _customerService.UpdateServiceRequestStatusAsync(id, request, CurrentUserId);
            return ApiResponse(response, "Service request status updated successfully.");
        }

        [HttpPatch("~/service-requests/{id}/rating")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitFeedback(int id, [FromBody] SubmitFeedbackRequest request)
        {
            await _customerService.SubmitFeedbackAsync(id, request);
            return ApiResponse<object?>(null, "Feedback submitted successfully.");
        }

        [HttpGet("~/service-requests")]
        [AllowAnonymous]
        public async Task<IActionResult> ListServiceRequests([FromQuery] RequestStatus? status)
        {
            var list = await _customerService.GetAllServiceRequestsAsync(status);
            return ApiResponse(list, "Service requests list retrieved.");
        }

        [HttpGet("~/service-requests/{id}")]
        [Authorize(Roles = "Manager,Assistant")]
        public async Task<IActionResult> GetServiceRequest(int id)
        {
            var request = await _customerService.GetServiceRequestByIdAsync(id);
            if (request == null)
            {
                return ApiError("Service request not found.", 404);
            }

            if (CurrentUserRole == "Assistant" && request.AssistantId != CurrentUserId)
            {
                return ApiError("Unauthorized to view this service request.", 403);
            }

            return ApiResponse(request, "Service request details retrieved.");
        }
    }
}
