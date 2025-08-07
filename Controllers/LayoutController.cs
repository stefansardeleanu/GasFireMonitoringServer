// File: Controllers/LayoutController.cs
// Complete layout controller with CRUD operations

using Microsoft.AspNetCore.Mvc;
using GasFireMonitoringServer.Services.Business.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for layout management with full CRUD operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LayoutController : ControllerBase
    {
        private readonly ILayoutService _layoutService;
        private readonly ILogger<LayoutController> _logger;

        public LayoutController(ILayoutService layoutService, ILogger<LayoutController> logger)
        {
            _layoutService = layoutService;
            _logger = logger;
        }

        /// <summary>
        /// Get layout information for a site
        /// Returns either SVG layout or grid layout data
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Complete layout information</returns>
        [HttpGet("site/{siteId}")]
        public async Task<IActionResult> GetSiteLayout(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting layout for site {SiteId}", siteId);

                var layoutInfo = await _layoutService.GetSiteLayoutAsync(siteId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        siteId = layoutInfo.SiteId,
                        siteName = layoutInfo.SiteName,
                        layoutType = layoutInfo.LayoutType.ToString(),
                        hasCustomLayout = layoutInfo.HasCustomLayout,
                        svgContent = layoutInfo.SvgContent,
                        gridConfig = layoutInfo.GridConfig,
                        sensorPositions = layoutInfo.SensorPositions.Select(sp => new
                        {
                            sensorKey = sp.SensorKey,
                            channelId = sp.ChannelId,
                            x = sp.X,
                            y = sp.Y,
                            displayName = sp.DisplayName,
                            detectorType = sp.DetectorType,
                            status = sp.Status,
                            statusText = sp.StatusText,
                            isOnline = sp.IsOnline
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting layout for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error retrieving layout" });
            }
        }

        /// <summary>
        /// Get SVG layout content only
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>SVG content or 404 if not found</returns>
        [HttpGet("site/{siteId}/svg")]
        public async Task<IActionResult> GetSvgLayout(int siteId)
        {
            try
            {
                var svgContent = await _layoutService.GetSvgLayoutAsync(siteId);

                if (svgContent == null)
                {
                    return NotFound(new { success = false, message = "SVG layout not found" });
                }

                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SVG layout for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error retrieving SVG layout" });
            }
        }

        /// <summary>
        /// Get sensor positions for a site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>List of sensor positions with real-time status</returns>
        [HttpGet("site/{siteId}/sensors")]
        public async Task<IActionResult> GetSensorPositions(int siteId)
        {
            try
            {
                var positions = await _layoutService.GetSensorPositionsAsync(siteId);

                return Ok(new
                {
                    success = true,
                    siteId = siteId,
                    sensorCount = positions.Count,
                    sensors = positions.Select(sp => new
                    {
                        sensorKey = sp.SensorKey,
                        channelId = sp.ChannelId,
                        x = sp.X,
                        y = sp.Y,
                        displayName = sp.DisplayName,
                        detectorType = sp.DetectorType,
                        status = sp.Status,
                        statusText = sp.StatusText,
                        isOnline = sp.IsOnline
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor positions for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error retrieving sensor positions" });
            }
        }

        /// <summary>
        /// Update sensor positions on layout
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="request">Sensor position updates</param>
        /// <returns>Success indicator</returns>
        [HttpPut("site/{siteId}/sensors")]
        public async Task<IActionResult> UpdateSensorPositions(int siteId, [FromBody] SensorPositionUpdateRequest request)
        {
            try
            {
                _logger.LogDebug("Updating sensor positions for site {SiteId}", siteId);

                // Validate request
                if (request?.SensorUpdates == null || !request.SensorUpdates.Any())
                {
                    return BadRequest(new { success = false, message = "No sensor updates provided" });
                }

                var results = new List<object>();
                var successCount = 0;

                foreach (var update in request.SensorUpdates)
                {
                    // Validate coordinates
                    if (update.X < 0 || update.X > 100 || update.Y < 0 || update.Y > 100)
                    {
                        results.Add(new
                        {
                            channelId = update.ChannelId,
                            success = false,
                            message = "Coordinates must be between 0 and 100"
                        });
                        continue;
                    }

                    var success = await _layoutService.UpdateSensorPositionAsync(siteId, update.ChannelId, update.X, update.Y);
                    if (success) successCount++;

                    results.Add(new
                    {
                        channelId = update.ChannelId,
                        success = success,
                        message = success ? "Position updated successfully" : "Failed to update position"
                    });
                }

                return Ok(new
                {
                    success = true,
                    siteId = siteId,
                    totalUpdates = request.SensorUpdates.Count(),
                    successfulUpdates = successCount,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sensor positions for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error updating sensor positions" });
            }
        }

        /// <summary>
        /// Upload new SVG layout file for a site
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="file">SVG file</param>
        /// <returns>Success indicator</returns>
        [HttpPost("site/{siteId}")]
        public async Task<IActionResult> UploadSvgLayout(int siteId, IFormFile file)
        {
            try
            {
                _logger.LogDebug("Uploading SVG layout for site {SiteId}", siteId);

                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                // Validate file type
                if (!file.ContentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase) &&
                    !file.FileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "File must be an SVG file" });
                }

                // Validate file size (max 5MB for SVG)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "File size must be less than 5MB" });
                }

                // Read file content
                using var reader = new StreamReader(file.OpenReadStream());
                var svgContent = await reader.ReadToEndAsync();

                // Save using layout service (includes validation)
                var success = await _layoutService.SaveSvgLayoutAsync(siteId, svgContent);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "SVG layout uploaded successfully",
                        siteId = siteId,
                        fileName = file.FileName,
                        fileSize = file.Length
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid SVG content" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading SVG layout for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error uploading SVG layout" });
            }
        }

        /// <summary>
        /// Upload SVG layout from raw content
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <param name="request">SVG content request</param>
        /// <returns>Success indicator</returns>
        [HttpPost("site/{siteId}/content")]
        public async Task<IActionResult> UploadSvgContent(int siteId, [FromBody] SvgContentRequest request)
        {
            try
            {
                _logger.LogDebug("Uploading SVG content for site {SiteId}", siteId);

                // Validate request
                if (request?.SvgContent == null)
                {
                    return BadRequest(new { success = false, message = "No SVG content provided" });
                }

                // Save using layout service (includes validation)
                var success = await _layoutService.SaveSvgLayoutAsync(siteId, request.SvgContent);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "SVG layout saved successfully",
                        siteId = siteId,
                        contentLength = request.SvgContent.Length
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid SVG content" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving SVG content for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error saving SVG content" });
            }
        }

        /// <summary>
        /// Delete SVG layout file for a site (will fallback to grid layout)
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Success indicator</returns>
        [HttpDelete("site/{siteId}")]
        public async Task<IActionResult> DeleteSvgLayout(int siteId)
        {
            try
            {
                _logger.LogDebug("Deleting SVG layout for site {SiteId}", siteId);

                var success = await _layoutService.DeleteSvgLayoutAsync(siteId);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "SVG layout deleted successfully (backup created)",
                        siteId = siteId,
                        fallbackMode = "Grid layout will be used automatically"
                    });
                }
                else
                {
                    return NotFound(new { success = false, message = "SVG layout not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SVG layout for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error deleting SVG layout" });
            }
        }

        /// <summary>
        /// Check if site has custom SVG layout
        /// </summary>
        /// <param name="siteId">Site ID</param>
        /// <returns>Boolean indicating if custom layout exists</returns>
        [HttpGet("site/{siteId}/has-custom")]
        public async Task<IActionResult> HasCustomLayout(int siteId)
        {
            try
            {
                var hasCustom = await _layoutService.HasCustomLayoutAsync(siteId);

                return Ok(new
                {
                    success = true,
                    siteId = siteId,
                    hasCustomLayout = hasCustom
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking custom layout for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error checking layout" });
            }
        }
    }

    #region Request Models

    /// <summary>
    /// Request model for updating sensor positions
    /// </summary>
    public class SensorPositionUpdateRequest
    {
        [Required]
        public IEnumerable<SensorPositionUpdate> SensorUpdates { get; set; } = new List<SensorPositionUpdate>();
    }

    /// <summary>
    /// Individual sensor position update
    /// </summary>
    public class SensorPositionUpdate
    {
        [Required]
        public string ChannelId { get; set; } = "";

        [Range(0, 100)]
        public double X { get; set; }

        [Range(0, 100)]
        public double Y { get; set; }
    }

    /// <summary>
    /// Request model for SVG content upload
    /// </summary>
    public class SvgContentRequest
    {
        [Required]
        public string SvgContent { get; set; } = "";
    }

    #endregion
}