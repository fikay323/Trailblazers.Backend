using System.Net;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public string RenderStaffInvitationEmail(
            string recipientName,
            string role,
            string inviteUrl,
            string invitedByName,
            string? logoUrl = null)
        {
            var cleanRecipientName = WebUtility.HtmlEncode(recipientName);
            var cleanRole = WebUtility.HtmlEncode(role);
            var cleanInvitedByName = WebUtility.HtmlEncode(invitedByName);
            var cleanInviteUrl = WebUtility.HtmlEncode(inviteUrl);
            var resolvedLogoUrl = !string.IsNullOrWhiteSpace(logoUrl)
                ? WebUtility.HtmlEncode(logoUrl)
                : "https://trailblazer-academy.com/trailblazer.jpeg";

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Invitation to join Trailblazers Academy Staff</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #090d16; color: #f1f5f9;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #090d16; padding: 40px 16px;"">
        <tr>
            <td align=""center"">
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 580px; background-color: #0f172a; border: 1px solid #1e293b; border-radius: 16px; overflow: hidden; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);"">
                    <!-- Header with Logo -->
                    <tr>
                        <td align=""center"" style=""padding: 36px 32px 24px; border-bottom: 1px solid #1e293b; background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);"">
                            <img src=""{resolvedLogoUrl}"" alt=""Trailblazers Academy"" width=""64"" height=""64"" style=""border-radius: 50%; display: block; border: 2px solid #f97316; margin-bottom: 14px; object-fit: cover;"">
                            <h1 style=""margin: 0; font-size: 22px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px;"">Trailblazers Academy</h1>
                            <p style=""margin: 4px 0 0; font-size: 13px; color: #f97316; font-weight: 700; text-transform: uppercase; letter-spacing: 1px;"">Faculty & Staff Portal</p>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 36px 32px;"">
                            <h2 style=""margin: 0 0 16px; font-size: 19px; font-weight: 700; color: #ffffff;"">
                                Welcome, {cleanRecipientName}!
                            </h2>
                            <p style=""margin: 0 0 16px; font-size: 14px; line-height: 22px; color: #cbd5e1;"">
                                You have been invited by <strong style=""color: #ffffff;"">{cleanInvitedByName}</strong> to join the <strong>Trailblazers Academy</strong> team as an <strong style=""color: #f97316;"">{cleanRole}</strong>.
                            </p>
                            <p style=""margin: 0 0 28px; font-size: 14px; line-height: 22px; color: #94a3b8;"">
                                To complete your account activation and access the staff management portal, please click the button below to set your account password.
                            </p>

                            <!-- CTA Button -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0 28px;"">
                                        <a href=""{cleanInviteUrl}"" style=""display: inline-block; background-color: #ea580c; color: #ffffff; font-size: 14px; font-weight: 700; text-decoration: none; padding: 14px 32px; border-radius: 10px; box-shadow: 0 4px 14px 0 rgba(234, 88, 12, 0.4); text-align: center;"">
                                            Accept Invitation & Set Password &rarr;
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Direct Link Fallback -->
                            <div style=""background-color: #020617; border: 1px solid #1e293b; border-radius: 8px; padding: 14px 16px; margin-bottom: 24px;"">
                                <p style=""margin: 0 0 6px; font-size: 12px; color: #94a3b8;"">
                                    If the button above does not work, copy and paste this link into your browser:
                                </p>
                                <a href=""{cleanInviteUrl}"" style=""font-size: 12px; color: #f97316; word-break: break-all; text-decoration: none;"">
                                    {cleanInviteUrl}
                                </a>
                            </div>

                            <!-- Expiration & Security Notice -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-top: 1px solid #1e293b; padding-top: 20px;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0 0 8px; font-size: 12px; color: #64748b; line-height: 18px;"">
                                            &bull; <strong>Expiration</strong>: This invitation link will expire in <strong>48 hours</strong>.
                                        </p>
                                        <p style=""margin: 0; font-size: 12px; color: #64748b; line-height: 18px;"">
                                            &bull; <strong>Security</strong>: If you did not expect this invitation, you can safely disregard this email or notify academy leadership.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding: 24px 32px; background-color: #020617; border-top: 1px solid #1e293b;"">
                            <p style=""margin: 0 0 4px; font-size: 12px; color: #94a3b8; font-weight: 600;"">
                                Trailblazers Academy
                            </p>
                            <p style=""margin: 0; font-size: 11px; color: #475569;"">
                                Academic Excellence, JAMB CBT Preparation & Student Mentorship &bull; Port Harcourt, Nigeria
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public string RenderAdminNewRegistrationAlertEmail(
            string studentName,
            string studentEmail,
            string studentPhone,
            string targetExam,
            string? guardianName,
            string? guardianPhone,
            string? guardianEmail,
            string? guardianRelationship,
            string reviewUrl,
            string? logoUrl = null)
        {
            var cleanStudentName = WebUtility.HtmlEncode(studentName);
            var cleanStudentEmail = WebUtility.HtmlEncode(studentEmail);
            var cleanStudentPhone = WebUtility.HtmlEncode(studentPhone);
            var cleanTargetExam = WebUtility.HtmlEncode(targetExam);
            var cleanGuardianName = !string.IsNullOrWhiteSpace(guardianName) ? WebUtility.HtmlEncode(guardianName) : "Not provided";
            var cleanGuardianPhone = !string.IsNullOrWhiteSpace(guardianPhone) ? WebUtility.HtmlEncode(guardianPhone) : "Not provided";
            var cleanGuardianEmail = !string.IsNullOrWhiteSpace(guardianEmail) ? WebUtility.HtmlEncode(guardianEmail) : "Not provided";
            var cleanGuardianRelationship = !string.IsNullOrWhiteSpace(guardianRelationship) ? WebUtility.HtmlEncode(guardianRelationship) : "Guardian";
            var cleanReviewUrl = WebUtility.HtmlEncode(reviewUrl);
            var resolvedLogoUrl = !string.IsNullOrWhiteSpace(logoUrl)
                ? WebUtility.HtmlEncode(logoUrl)
                : "https://trailblazer-academy.com/trailblazer.jpeg";

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>New Student Registration Alert</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #090d16; color: #f1f5f9;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #090d16; padding: 40px 16px;"">
        <tr>
            <td align=""center"">
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; background-color: #0f172a; border: 1px solid #1e293b; border-radius: 16px; overflow: hidden; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);"">
                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""padding: 32px 28px 20px; border-bottom: 1px solid #1e293b; background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);"">
                            <img src=""{resolvedLogoUrl}"" alt=""Trailblazers Academy"" width=""56"" height=""56"" style=""border-radius: 50%; display: block; border: 2px solid #f97316; margin-bottom: 12px; object-fit: cover;"">
                            <h1 style=""margin: 0; font-size: 20px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px;"">Trailblazers Admin Alert</h1>
                            <p style=""margin: 4px 0 0; font-size: 12px; color: #f97316; font-weight: 700; text-transform: uppercase; letter-spacing: 1px;"">New Student Registration</p>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 32px;"">
                            <p style=""margin: 0 0 20px; font-size: 15px; line-height: 22px; color: #e2e8f0;"">
                                A prospective student has just submitted a registration form on the public portal. Please review their details and follow up.
                            </p>

                            <!-- Student Details Section -->
                            <div style=""background-color: #020617; border: 1px solid #1e293b; border-radius: 10px; padding: 18px 20px; margin-bottom: 20px;"">
                                <h3 style=""margin: 0 0 12px; font-size: 13px; font-weight: 700; color: #f97316; text-transform: uppercase; letter-spacing: 0.5px;"">
                                    Student Information
                                </h3>
                                <table width=""100%"" cellpadding=""4"" cellspacing=""0"" style=""font-size: 13px; color: #cbd5e1;"">
                                    <tr>
                                        <td width=""35%"" style=""color: #64748b; font-weight: 600;"">Name:</td>
                                        <td style=""color: #ffffff; font-weight: 700;"">{cleanStudentName}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Email:</td>
                                        <td><a href=""mailto:{cleanStudentEmail}"" style=""color: #38bdf8; text-decoration: none;"">{cleanStudentEmail}</a></td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Phone:</td>
                                        <td><a href=""tel:{cleanStudentPhone}"" style=""color: #38bdf8; text-decoration: none;"">{cleanStudentPhone}</a></td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Target Exam:</td>
                                        <td><span style=""display: inline-block; background-color: #ea580c; color: #ffffff; font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 6px;"">{cleanTargetExam}</span></td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Guardian Details Section -->
                            <div style=""background-color: #020617; border: 1px solid #1e293b; border-radius: 10px; padding: 18px 20px; margin-bottom: 24px;"">
                                <h3 style=""margin: 0 0 12px; font-size: 13px; font-weight: 700; color: #38bdf8; text-transform: uppercase; letter-spacing: 0.5px;"">
                                    Parent / Guardian Information
                                </h3>
                                <table width=""100%"" cellpadding=""4"" cellspacing=""0"" style=""font-size: 13px; color: #cbd5e1;"">
                                    <tr>
                                        <td width=""35%"" style=""color: #64748b; font-weight: 600;"">Name:</td>
                                        <td style=""color: #ffffff; font-weight: 600;"">{cleanGuardianName} ({cleanGuardianRelationship})</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Phone:</td>
                                        <td>{cleanGuardianPhone}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #64748b; font-weight: 600;"">Email:</td>
                                        <td>{cleanGuardianEmail}</td>
                                    </tr>
                                </table>
                            </div>

                            <!-- CTA Button -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 6px 0 20px;"">
                                        <a href=""{cleanReviewUrl}"" style=""display: inline-block; background-color: #ea580c; color: #ffffff; font-size: 14px; font-weight: 700; text-decoration: none; padding: 12px 28px; border-radius: 8px; box-shadow: 0 4px 14px 0 rgba(234, 88, 12, 0.4); text-align: center;"">
                                            Review in Admin Portal &rarr;
                                        </a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding: 20px 28px; background-color: #020617; border-top: 1px solid #1e293b;"">
                            <p style=""margin: 0; font-size: 11px; color: #475569;"">
                                Trailblazers Academy Automated Notification System &bull; Port Harcourt, Nigeria
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public string RenderStudentAccountActivationEmail(
            string studentName,
            string targetExam,
            string activationUrl,
            string? logoUrl = null)
        {
            var cleanStudentName = WebUtility.HtmlEncode(studentName);
            var cleanTargetExam = WebUtility.HtmlEncode(targetExam);
            var cleanActivationUrl = WebUtility.HtmlEncode(activationUrl);
            var resolvedLogoUrl = !string.IsNullOrWhiteSpace(logoUrl)
                ? WebUtility.HtmlEncode(logoUrl)
                : "https://trailblazer-academy.com/trailblazer.jpeg";

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Activate Your Trailblazers Student Account</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #090d16; color: #f1f5f9;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #090d16; padding: 40px 16px;"">
        <tr>
            <td align=""center"">
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 580px; background-color: #0f172a; border: 1px solid #1e293b; border-radius: 16px; overflow: hidden; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);"">
                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""padding: 36px 32px 24px; border-bottom: 1px solid #1e293b; background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);"">
                            <img src=""{resolvedLogoUrl}"" alt=""Trailblazers Academy"" width=""64"" height=""64"" style=""border-radius: 50%; display: block; border: 2px solid #f97316; margin-bottom: 14px; object-fit: cover;"">
                            <h1 style=""margin: 0; font-size: 22px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px;"">Trailblazers Academy</h1>
                            <p style=""margin: 4px 0 0; font-size: 13px; color: #f97316; font-weight: 700; text-transform: uppercase; letter-spacing: 1px;"">Student Learning Portal</p>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 36px 32px;"">
                            <h2 style=""margin: 0 0 16px; font-size: 19px; font-weight: 700; color: #ffffff;"">
                                Welcome, {cleanStudentName}!
                            </h2>
                            <p style=""margin: 0 0 16px; font-size: 14px; line-height: 22px; color: #cbd5e1;"">
                                Your enrollment in the <strong style=""color: #f97316;"">{cleanTargetExam}</strong> program at <strong>Trailblazers Academy</strong> has been confirmed by administration.
                            </p>
                            <p style=""margin: 0 0 28px; font-size: 14px; line-height: 22px; color: #94a3b8;"">
                                To access your student portal, take CBT practice exams, view course materials, and track your attendance, please click the button below to set your account password.
                            </p>

                            <!-- CTA Button -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0 28px;"">
                                        <a href=""{cleanActivationUrl}"" style=""display: inline-block; background-color: #ea580c; color: #ffffff; font-size: 14px; font-weight: 700; text-decoration: none; padding: 14px 32px; border-radius: 10px; box-shadow: 0 4px 14px 0 rgba(234, 88, 12, 0.4); text-align: center;"">
                                            Activate Student Account &rarr;
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Fallback Link -->
                            <div style=""background-color: #020617; border: 1px solid #1e293b; border-radius: 8px; padding: 14px 16px; margin-bottom: 24px;"">
                                <p style=""margin: 0 0 6px; font-size: 12px; color: #94a3b8;"">
                                    If the button above does not work, copy and paste this link into your browser:
                                </p>
                                <a href=""{cleanActivationUrl}"" style=""font-size: 12px; color: #f97316; word-break: break-all; text-decoration: none;"">
                                    {cleanActivationUrl}
                                </a>
                            </div>

                            <!-- Notice -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-top: 1px solid #1e293b; padding-top: 20px;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0; font-size: 12px; color: #64748b; line-height: 18px;"">
                                            &bull; This activation link will expire in <strong>48 hours</strong>. If you need assistance, please contact your academy instructor or administrator.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding: 24px 32px; background-color: #020617; border-top: 1px solid #1e293b;"">
                            <p style=""margin: 0 0 4px; font-size: 12px; color: #94a3b8; font-weight: 600;"">
                                Trailblazers Academy
                            </p>
                            <p style=""margin: 0; font-size: 11px; color: #475569;"">
                                Academic Excellence, JAMB CBT Preparation & Student Mentorship &bull; Port Harcourt, Nigeria
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
