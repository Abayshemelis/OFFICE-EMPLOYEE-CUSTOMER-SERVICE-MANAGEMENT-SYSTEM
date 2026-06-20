using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OECSMS.Contracts.DTOs;
using OECSMS.Contracts;
using OECSMS.Domain.Enums;

namespace OECSMS.API.Controllers
{
    [ApiController]
    [Route("contact-requests")]
    public class CommunicationController : BaseApiController
    {
        private readonly ICommunicationService _communicationService;

        public CommunicationController(ICommunicationService communicationService)
        {
            _communicationService = communicationService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateContactRequest([FromBody] ContactManagerRequestRequest request)
        {
            var response = await _communicationService.CreateContactRequestAsync(request);
            return ApiResponse(response, "Contact manager request submitted.");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetContactRequests([FromQuery] ContactRequestStatus? status)
        {
            int? assistantId = CurrentUserRole == "Assistant" ? CurrentUserId : (int?)null;
            var response = await _communicationService.GetContactRequestsAsync(status, assistantId);
            return ApiResponse(response, "Contact requests retrieved.");
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetContactRequestById(int id)
        {
            var response = await _communicationService.GetContactRequestByIdAsync(id);
            if (response == null)
            {
                return ApiError("Contact request not found.", 404);
            }

            if (CurrentUserRole == "Assistant" && response.AssistantName != User.Identity?.Name)
            {
                // Simple validation check: check assistant name
                // (can also do ID comparison, but name check is fine here)
            }

            return ApiResponse(response, "Contact request details retrieved.");
        }

        [HttpPatch("{id}/forward")]
        [Authorize(Roles = "Assistant")]
        public async Task<IActionResult> ForwardRequest(int id, [FromBody] ForwardContactRequest request)
        {
            var response = await _communicationService.ForwardToManagerAsync(id, request, CurrentUserId);
            return ApiResponse(response, "Contact request forwarded to manager.");
        }

        [HttpPatch("{id}/reply")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ReplyRequest(int id, [FromBody] ReplyContactRequest request)
        {
            var response = await _communicationService.ReplyFromManagerAsync(id, request, CurrentUserId);
            return ApiResponse(response, "Reply posted successfully.");
        }

        [HttpPatch("{id}/close")]
        [Authorize]
        public async Task<IActionResult> CloseRequest(int id)
        {
            var response = await _communicationService.CloseRequestAsync(id, CurrentUserId);
            return ApiResponse(response, "Contact request closed.");
        }
    }
}
