using Microsoft.AspNetCore.Mvc;
using NEOUnlocker.Server.Models;
using NEOUnlocker.Server.Services;

namespace NEOUnlocker.Server.Controllers;

/// <summary>
/// API controller for flash session management.
/// </summary>
[ApiController]
[Route("api/flash")]
public class FlashController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IFirmwareService _firmwareService;
    private readonly ILogger<FlashController> _logger;

    public FlashController(
        ISessionService sessionService,
        IFirmwareService firmwareService,
        ILogger<FlashController> logger)
    {
        _sessionService = sessionService;
        _firmwareService = firmwareService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new flash session.
    /// </summary>
    /// <param name="request">Session creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Flash session details.</returns>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(FlashSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FlashSessionResponse>> CreateSession(
        [FromBody] CreateFlashSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating session for HWID {HWID}, device type {DeviceType}",
                request.HWID, request.DeviceType);

            var session = await _sessionService.CreateSessionAsync(
                request.HWID,
                request.DeviceType,
                request.ClientPublicKeyPem,
                cancellationToken);

            var response = new FlashSessionResponse
            {
                SessionId = session.SessionId,
                WrappedSessionKeyBase64 = Convert.ToBase64String(session.WrappedSessionKey),
                ExpiresAt = session.ExpiresAt.ToString("O"), // ISO 8601
                Status = session.Status.ToString(),
                FirmwareFiles = session.RequiredFirmwareFiles,
                CreditCost = session.CreditCost
            };

            return CreatedAtAction(
                nameof(GetSession),
                new { sessionId = session.SessionId },
                response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for session creation");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot create session");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets flash session details.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="hwid">Hardware ID for validation (from query string).</param>
    /// <returns>Flash session details.</returns>
    [HttpGet("sessions/{sessionId}")]
    [ProducesResponseType(typeof(FlashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<FlashSessionResponse> GetSession(
        [FromRoute] string sessionId,
        [FromQuery] string hwid)
    {
        if (string.IsNullOrWhiteSpace(hwid))
        {
            return BadRequest(new { error = "HWID is required" });
        }

        var session = _sessionService.GetSession(sessionId, hwid);
        
        if (session == null)
        {
            _logger.LogWarning("Session not found or HWID mismatch: {SessionId}", sessionId);
            return NotFound(new { error = "Session not found or HWID mismatch" });
        }

        var response = new FlashSessionResponse
        {
            SessionId = session.SessionId,
            WrappedSessionKeyBase64 = Convert.ToBase64String(session.WrappedSessionKey),
            ExpiresAt = session.ExpiresAt.ToString("O"),
            Status = session.Status.ToString(),
            FirmwareFiles = session.RequiredFirmwareFiles,
            CreditCost = session.CreditCost
        };

        return Ok(response);
    }

    /// <summary>
    /// Downloads encrypted firmware file for a session.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="fileName">Firmware file name.</param>
    /// <param name="hwid">Hardware ID for validation (from query string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Encrypted firmware stream.</returns>
    [HttpGet("sessions/{sessionId}/firmware/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFirmware(
        [FromRoute] string sessionId,
        [FromRoute] string fileName,
        [FromQuery] string hwid,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(hwid))
        {
            return BadRequest(new { error = "HWID is required" });
        }

        try
        {
            // Validate session
            var session = _sessionService.ValidateSession(sessionId, hwid);

            // Validate firmware file is required for this session
            if (!session.RequiredFirmwareFiles.Contains(fileName))
            {
                _logger.LogWarning(
                    "Firmware file {FileName} not required for session {SessionId}",
                    fileName, sessionId);
                return BadRequest(new { error = "Firmware file not required for this session" });
            }

            _logger.LogInformation(
                "Streaming firmware {FileName} for session {SessionId}",
                fileName, sessionId);

            // Get re-encrypted firmware stream
            var stream = await _firmwareService.GetReEncryptedFirmwareStreamAsync(
                fileName,
                session.SessionKey,
                cancellationToken);

            return File(stream, "application/octet-stream", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot download firmware for session {SessionId}", sessionId);
            return BadRequest(new { error = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Firmware file not found: {FileName}", fileName);
            return NotFound(new { error = "Firmware file not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download firmware");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Marks a session as completed or failed.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="request">Completion request.</param>
    /// <returns>Completion response.</returns>
    [HttpPost("sessions/{sessionId}/complete")]
    [ProducesResponseType(typeof(CompleteFlashSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CompleteFlashSessionResponse> CompleteSession(
        [FromRoute] string sessionId,
        [FromBody] CompleteFlashSessionRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Completing session {SessionId}, success: {Success}",
                sessionId, request.Success);

            var result = _sessionService.CompleteSession(
                sessionId,
                request.HWID,
                request.Success,
                request.ErrorMessage);

            if (!result)
            {
                return NotFound(new CompleteFlashSessionResponse
                {
                    Success = false,
                    Message = "Session not found or HWID mismatch",
                    CreditsDeducted = false
                });
            }

            var response = new CompleteFlashSessionResponse
            {
                Success = true,
                Message = request.Success ? "Session completed successfully" : "Session marked as failed",
                CreditsDeducted = request.Success
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete session {SessionId}", sessionId);
            return StatusCode(500, new CompleteFlashSessionResponse
            {
                Success = false,
                Message = "Internal server error",
                CreditsDeducted = false
            });
        }
    }
}
