using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OECSMS.Contracts.DTOs;
using OECSMS.Contracts;
using OECSMS.Domain.Entities;
using OECSMS.Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace OECSMS.Application.Services
{
    public class CommunicationService : ICommunicationService
    {
        private readonly IContactManagerRequestRepository _contactRequestRepository;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly INotificationHubContext _notificationHubContext;

        public CommunicationService(
            IContactManagerRequestRepository contactRequestRepository,
            IServiceRequestRepository serviceRequestRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            INotificationHubContext notificationHubContext)
        {
            _contactRequestRepository = contactRequestRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _notificationHubContext = notificationHubContext;
        }

        public async Task<ContactManagerResponse> CreateContactRequestAsync(ContactManagerRequestRequest request)
        {
            var serviceReq = await _serviceRequestRepository.GetByIdAsync(request.RequestId);
            if (serviceReq == null) throw new ArgumentException("Service request not found");

            var contactReq = new ContactManagerRequest
            {
                RequestId = request.RequestId,
                CustomerMessage = request.CustomerMessage,
                Status = ContactRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _contactRequestRepository.AddAsync(contactReq);

            // Alert Assistant via SignalR and Notification
            var title = "Contact Manager Request";
            var message = $"Customer {serviceReq.Customer?.FullName} has requested to contact the manager.";
            await _notificationService.CreateNotificationAsync(serviceReq.AssistantId, title, message, NotificationType.ContactRequest, contactReq.ContactRequestId);
            await _notificationHubContext.SendNotificationToUserAsync(serviceReq.AssistantId, title, message, "ContactRequest", contactReq.ContactRequestId);

            var dbRequest = await _contactRequestRepository.GetByIdAsync(contactReq.ContactRequestId);
            return MapToResponse(dbRequest!);
        }

        public async Task<IEnumerable<ContactManagerResponse>> GetContactRequestsAsync(ContactRequestStatus? status = null, int? assistantId = null)
        {
            var requests = await _contactRequestRepository.GetAllAsync(status, assistantId);
            return requests.Select(MapToResponse);
        }

        public async Task<ContactManagerResponse?> GetContactRequestByIdAsync(int id)
        {
            var request = await _contactRequestRepository.GetByIdAsync(id);
            return request != null ? MapToResponse(request) : null;
        }

        public async Task<ContactManagerResponse> ForwardToManagerAsync(int id, ForwardContactRequest request, int assistantId)
        {
            var contactReq = await _contactRequestRepository.GetByIdAsync(id);
            if (contactReq == null) throw new ArgumentException("Contact request not found");

            if (contactReq.ServiceRequest.AssistantId != assistantId)
                throw new UnauthorizedAccessException("Not authorized to forward this request");

            contactReq.AssistantNote = request.AssistantNote;
            contactReq.Status = ContactRequestStatus.Forwarded;
            contactReq.ForwardedAt = DateTime.UtcNow;

            await _contactRequestRepository.UpdateAsync(contactReq);

            // Notify Manager
            var assistant = await _userRepository.GetByIdAsync(assistantId);
            var managerId = assistant?.ManagerId;
            if (managerId.HasValue)
            {
                var title = "Forwarded Contact Request";
                var message = $"Assistant {assistant?.FullName} forwarded a contact request from {contactReq.ServiceRequest.Customer?.FullName}.";
                await _notificationService.CreateNotificationAsync(managerId.Value, title, message, NotificationType.ContactRequest, contactReq.ContactRequestId);
                await _notificationHubContext.SendNotificationToUserAsync(managerId.Value, title, message, "ContactRequest", contactReq.ContactRequestId);
            }

            return MapToResponse(contactReq);
        }

        public async Task<ContactManagerResponse> ReplyFromManagerAsync(int id, ReplyContactRequest request, int managerId)
        {
            var contactReq = await _contactRequestRepository.GetByIdAsync(id);
            if (contactReq == null) throw new ArgumentException("Contact request not found");

            contactReq.ManagerReply = request.ReplyMessage;
            contactReq.Status = ContactRequestStatus.Replied;
            contactReq.RepliedAt = DateTime.UtcNow;

            await _contactRequestRepository.UpdateAsync(contactReq);

            // Notify Assistant
            var assistantId = contactReq.ServiceRequest.AssistantId;
            var title = "Manager Replied to Contact Request";
            var message = $"Manager has replied to the contact request from {contactReq.ServiceRequest.Customer?.FullName}.";
            await _notificationService.CreateNotificationAsync(assistantId, title, message, NotificationType.ContactRequest, contactReq.ContactRequestId);
            await _notificationHubContext.SendNotificationToUserAsync(assistantId, title, message, "ContactRequest", contactReq.ContactRequestId);

            return MapToResponse(contactReq);
        }

        public async Task<ContactManagerResponse> CloseRequestAsync(int id, int userId)
        {
            var contactReq = await _contactRequestRepository.GetByIdAsync(id);
            if (contactReq == null) throw new ArgumentException("Contact request not found");

            contactReq.Status = ContactRequestStatus.Closed;
            await _contactRequestRepository.UpdateAsync(contactReq);

            return MapToResponse(contactReq);
        }

        private ContactManagerResponse MapToResponse(ContactManagerRequest request)
        {
            return new ContactManagerResponse
            {
                ContactRequestId = request.ContactRequestId,
                RequestId = request.RequestId,
                CustomerName = request.ServiceRequest?.Customer?.FullName ?? "Unknown",
                AssistantName = request.ServiceRequest?.Assistant?.FullName ?? "Unknown",
                CustomerMessage = request.CustomerMessage,
                AssistantNote = request.AssistantNote,
                ForwardedAt = request.ForwardedAt,
                ManagerReply = request.ManagerReply,
                RepliedAt = request.RepliedAt,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            };
        }
    }
}
