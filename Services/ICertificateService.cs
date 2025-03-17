using EducationPlatformN.Models;

namespace EducationPlatformN.Services
{
     public interface ICertificateService
    {
        Task<Certificate> IssueCertificateAsync(int userId, int courseId);
        Task<List<Certificate>> GetUserCertificatesAsync(int userId);
        Task<bool> IsEligibleForCertificateAsync(int userId, int courseId);
    }
}