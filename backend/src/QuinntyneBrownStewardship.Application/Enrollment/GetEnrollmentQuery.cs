using MediatR;
namespace QuinntyneBrownStewardship.Application.Enrollment;

public sealed record GetEnrollmentQuery : IRequest<EnrollmentResponse>;
