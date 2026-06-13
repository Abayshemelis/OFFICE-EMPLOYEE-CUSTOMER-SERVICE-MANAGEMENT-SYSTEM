using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OECSMS.Application.DTOs;
using OECSMS.Application.Interfaces;
using OECSMS.Domain.Entities;
using OECSMS.Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace OECSMS.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAssistantConductScoreRepository _conductScoreRepository;
        private readonly INotificationService _notificationService;
        private readonly INotificationHubContext _notificationHubContext;

        public CustomerService(
            ICustomerRepository customerRepository,
            IServiceRequestRepository serviceRequestRepository,
            IUserRepository userRepository,
            IAssistantConductScoreRepository conductScoreRepository,
            INotificationService notificationService,
            INotificationHubContext notificationHubContext)
        {
            _customerRepository = customerRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _userRepository = userRepository;
            _conductScoreRepository = conductScoreRepository;
            _notificationService = notificationService;
            _notificationHubContext = notificationHubContext;
        }

        public async Task<RegisterCustomerResponse> RegisterCustomerArrivalAsync(RegisterCustomerRequest request)
        {
            var date = DateTime.UtcNow.Date;
            var queueNo = await _customerRepository.GetNextQueueNumberAsync(date);

            var customer = new Customer
            {
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                VisitDate = date,
                ArrivalTime = DateTime.UtcNow,
                QueueNumber = queueNo
            };

            await _customerRepository.AddAsync(customer);

            // Compute Estimated Wait Time: Count currently waiting requests
            var activeQueue = await _serviceRequestRepository.GetAllAsync(RequestStatus.Waiting);
            var waitingCount = activeQueue.Count();
            var estWaitTime = waitingCount * 10; // 10 minutes per person

            var serviceRequest = new ServiceRequest
            {
                CustomerId = customer.CustomerId,
                AssistantId = request.AssignedAssistantId,
                ServiceDescription = request.ServiceDescription,
                Status = RequestStatus.Waiting,
                CreatedAt = DateTime.UtcNow
            };

            await _serviceRequestRepository.AddAsync(serviceRequest);

            // Notify Assistant via SignalR and in-app Notification
            var assistant = await _userRepository.GetByIdAsync(request.AssignedAssistantId);
            var title = "Customer Registered";
            var message = $"Customer {customer.FullName} has joined the queue. Queue No: {queueNo}.";
            
            await _notificationService.CreateNotificationAsync(request.AssignedAssistantId, title, message, NotificationType.CustomerArrival, serviceRequest.RequestId);
            await _notificationHubContext.SendNotificationToUserAsync(request.AssignedAssistantId, title, message, "CustomerArrival", serviceRequest.RequestId);
            await _notificationHubContext.SendQueueUpdateAsync();

            return new RegisterCustomerResponse
            {
                CustomerId = customer.CustomerId,
                QueueNumber = queueNo,
                EstimatedWaitTimeMinutes = estWaitTime,
                RequestId = serviceRequest.RequestId
            };
        }

        public async Task<IEnumerable<ServiceRequestResponse>> GetActiveQueueAsync()
        {
            var activeWaiting = await _serviceRequestRepository.GetAllAsync(RequestStatus.Waiting);
            var activeServing = await _serviceRequestRepository.GetAllAsync(RequestStatus.InService);

            var combined = activeWaiting.Concat(activeServing).OrderBy(r => r.Customer.QueueNumber);
            return combined.Select(MapToResponse);
        }

        public async Task<ServiceRequestResponse?> GetServiceRequestByIdAsync(int requestId)
        {
            var request = await _serviceRequestRepository.GetByIdAsync(requestId);
            return request != null ? MapToResponse(request) : null;
        }

        public async Task<IEnumerable<ServiceRequestResponse>> GetAllServiceRequestsAsync(RequestStatus? status = null, int? assistantId = null)
        {
            var requests = await _serviceRequestRepository.GetAllAsync(status, assistantId);
            return requests.Select(MapToResponse);
        }

        public async Task<ServiceRequestResponse> UpdateServiceRequestStatusAsync(int requestId, UpdateServiceRequestStatusRequest request, int assistantId)
        {
            var serviceRequest = await _serviceRequestRepository.GetByIdAsync(requestId);
            if (serviceRequest == null) throw new ArgumentException("Service request not found");

            if (serviceRequest.AssistantId != assistantId)
                throw new UnauthorizedAccessException("Not authorized to update this service request");

            serviceRequest.Status = request.Status;
            
            if (request.Status == RequestStatus.InService)
            {
                serviceRequest.ServiceStartTime = DateTime.UtcNow;
            }
            else if (request.Status == RequestStatus.Completed || request.Status == RequestStatus.Referred || request.Status == RequestStatus.Unresolved)
            {
                serviceRequest.ServiceEndTime = DateTime.UtcNow;
                serviceRequest.ResolutionNote = request.ResolutionNote;
            }

            await _serviceRequestRepository.UpdateAsync(serviceRequest);

            // Notify clients of queue change
            await _notificationHubContext.SendQueueUpdateAsync();

            var dbRequest = await _serviceRequestRepository.GetByIdAsync(requestId);
            return MapToResponse(dbRequest!);
        }

        public async Task SubmitFeedbackAsync(int requestId, SubmitFeedbackRequest request)
        {
            var serviceRequest = await _serviceRequestRepository.GetByIdAsync(requestId);
            if (serviceRequest == null) throw new ArgumentException("Service request not found");

            serviceRequest.CustomerRating = request.Rating;
            serviceRequest.CustomerFeedback = request.Feedback;
            await _serviceRequestRepository.UpdateAsync(serviceRequest);

            // Log score
            var score = new AssistantConductScore
            {
                AssistantId = serviceRequest.AssistantId,
                RequestId = requestId,
                Rating = request.Rating,
                RecordedAt = DateTime.UtcNow
            };
            await _conductScoreRepository.AddAsync(score);

            // Alert manager if rating is below 3 stars
            if (request.Rating < 3)
            {
                var assistant = await _userRepository.GetByIdAsync(serviceRequest.AssistantId);
                var managerId = assistant?.ManagerId;
                if (managerId.HasValue)
                {
                    var title = "Low Service Rating Alert";
                    var message = $"Assistant {assistant?.FullName} received a low rating of {request.Rating} stars from customer {serviceRequest.Customer?.FullName}. Feedback: {request.Feedback ?? "None"}";
                    await _notificationService.CreateNotificationAsync(managerId.Value, title, message, NotificationType.SystemAlert, requestId);
                    await _notificationHubContext.SendNotificationToUserAsync(managerId.Value, title, message, "SystemAlert", requestId);
                }
            }
        }

        private ServiceRequestResponse MapToResponse(ServiceRequest request)
        {
            return new ServiceRequestResponse
            {
                RequestId = request.RequestId,
                CustomerId = request.CustomerId,
                CustomerName = request.Customer?.FullName ?? "Unknown",
                QueueNumber = request.Customer?.QueueNumber ?? 0,
                AssistantId = request.AssistantId,
                AssistantName = request.Assistant?.FullName ?? "Unknown",
                ServiceDescription = request.ServiceDescription,
                Status = request.Status,
                ServiceStartTime = request.ServiceStartTime,
                ServiceEndTime = request.ServiceEndTime,
                ResolutionNote = request.ResolutionNote,
                CustomerRating = request.CustomerRating,
                CustomerFeedback = request.CustomerFeedback,
                CreatedAt = request.CreatedAt
            };
        }
    }
}
