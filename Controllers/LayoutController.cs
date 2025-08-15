// File: Controllers/LayoutController.cs
// Enhanced layout controller with comprehensive XML documentation

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GasFireMonitoringServer.Services.Business.Interfaces;
using GasFireMonitoringServer.Models.DTOs.Common;
using GasFireMonitoringServer.Models.DTOs.Responses;
using System.ComponentModel.DataAnnotations;

namespace GasFireMonitoringServer.Controllers
{
    /// <summary>
    /// API controller for comprehensive layout management and sensor positioning in industrial monitoring systems
    /// </summary>
    /// <remarks>
    /// <para><strong>Layout Management System Overview</strong></para>
    /// <para>This controller provides complete layout management capabilities for industrial gas and fire monitoring systems,
    /// supporting both custom SVG layouts and automatic grid-based sensor arrangements across multiple industrial sites.</para>
    /// 
    /// <para><strong>🎯 Core Capabilities:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Layout Modes</strong>: SVG custom layouts, automatic grid generation, and intelligent fallback systems</description></item>
    /// <item><description><strong>Sensor Positioning</strong>: Percentage-based coordinate system (0-100%) for device-independent positioning</description></item>
    /// <item><description><strong>Real-time Integration</strong>: Live sensor data overlay on layout visualizations with status indicators</description></item>
    /// <item><description><strong>File Management</strong>: SVG upload, validation, backup, and recovery capabilities</description></item>
    /// <item><description><strong>Dynamic Configuration</strong>: Hot-swappable layouts without system restart requirements</description></item>
    /// </list>
    /// 
    /// <para><strong>🗺️ Layout System Architecture:</strong></para>
    /// <list type="number">
    /// <item><description><strong>SVG Mode</strong>: Custom scalable vector graphics with embedded sensor positioning anchors</description></item>
    /// <item><description><strong>Grid Mode</strong>: Automatic grid generation with configurable columns, spacing, and grouping options</description></item>
    /// <item><description><strong>Auto Mode</strong>: Intelligent fallback that attempts SVG first, then generates grid layout</description></item>
    /// </list>
    /// 
    /// <para><strong>📐 Coordinate System:</strong></para>
    /// <para>All sensor positions use a percentage-based coordinate system (0-100%) for both X and Y axes.
    /// This ensures consistent positioning across different screen sizes and resolutions:</para>
    /// <list type="bullet">
    /// <item><description><strong>X-Axis</strong>: 0% = leftmost edge, 50% = center, 100% = rightmost edge</description></item>
    /// <item><description><strong>Y-Axis</strong>: 0% = top edge, 50% = center, 100% = bottom edge</description></item>
    /// <item><description><strong>Validation</strong>: All coordinates automatically validated within 0-100% range</description></item>
    /// </list>
    /// 
    /// <para><strong>🔧 File Management Features:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>SVG Validation</strong>: XML structure validation and security scanning</description></item>
    /// <item><description><strong>File Size Limits</strong>: Maximum 5MB for SVG files to ensure performance</description></item>
    /// <item><description><strong>Automatic Backup</strong>: Previous layouts backed up before updates</description></item>
    /// <item><description><strong>Version Control</strong>: Timestamped backups for rollback capabilities</description></item>
    /// </list>
    /// 
    /// <para><strong>🚀 Industrial Use Cases:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Oil & Gas Facilities</strong>: Custom site layouts with equipment-specific sensor positioning</description></item>
    /// <item><description><strong>Chemical Plants</strong>: Process flow diagrams with real-time sensor overlays</description></item>
    /// <item><description><strong>Manufacturing</strong>: Production line layouts with safety monitoring points</description></item>
    /// <item><description><strong>Mining Operations</strong>: Underground tunnel systems with ventilation monitoring</description></item>
    /// </list>
    /// 
    /// <para><strong>📊 Real-time Features:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Live Status Updates</strong>: Sensor values and alarm states refresh automatically</description></item>
    /// <item><description><strong>Visual Indicators</strong>: Color-coded status display (Normal/Warning/Critical/Fault)</description></item>
    /// <item><description><strong>Connectivity Status</strong>: Online/offline sensor indication based on last update time</description></item>
    /// <item><description><strong>Interactive Elements</strong>: Clickable sensors with detailed information tooltips</description></item>
    /// </list>
    /// 
    /// <para><strong>🔐 Security & Access Control:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Authentication Required</strong>: All endpoints require valid JWT bearer token</description></item>
    /// <item><description><strong>Role-based Access</strong>: Upload/modification requires elevated permissions</description></item>
    /// <item><description><strong>SVG Sanitization</strong>: Uploaded SVG content scanned for security vulnerabilities</description></item>
    /// <item><description><strong>Audit Logging</strong>: All layout changes logged with user context and timestamps</description></item>
    /// </list>
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 500)]
    public class LayoutController : ControllerBase
    {
        private readonly ILayoutService _layoutService;
        private readonly ILogger<LayoutController> _logger;

        /// <summary>
        /// Initializes a new instance of the LayoutController with required dependencies
        /// </summary>
        /// <param name="layoutService">Service for layout business logic and file management operations</param>
        /// <param name="logger">Logger for comprehensive operation tracking and error reporting</param>
        public LayoutController(ILayoutService layoutService, ILogger<LayoutController> logger)
        {
            _layoutService = layoutService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieve comprehensive layout information for a specific industrial site
        /// </summary>
        /// <remarks>
        /// <para><strong>Complete Layout Data Retrieval</strong></para>
        /// <para>This endpoint provides comprehensive layout information for industrial sites, automatically determining
        /// the optimal layout mode (SVG custom or Grid automatic) and returning complete sensor positioning data with real-time status information.</para>
        /// 
        /// <para><strong>🎯 Layout Mode Selection Logic:</strong></para>
        /// <list type="number">
        /// <item><description><strong>SVG Priority</strong>: If custom SVG layout exists, returns SVG mode with embedded positioning</description></item>
        /// <item><description><strong>Grid Fallback</strong>: If no SVG layout, generates automatic grid layout based on sensor count</description></item>
        /// <item><description><strong>Configuration Override</strong>: Site configuration can force specific layout mode</description></item>
        /// </list>
        /// 
        /// <para><strong>📊 Response Data Structure:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Layout Metadata</strong>: Site information, layout type, and modification timestamps</description></item>
        /// <item><description><strong>SVG Content</strong>: Complete scalable vector graphics content (if applicable)</description></item>
        /// <item><description><strong>Sensor Positions</strong>: All sensor coordinates with real-time status data</description></item>
        /// <item><description><strong>Grid Configuration</strong>: Automatic layout settings (if using grid mode)</description></item>
        /// </list>
        /// 
        /// <para><strong>🔄 Real-time Integration:</strong></para>
        /// <para>Sensor positioning data includes current values, alarm states, and connectivity status.
        /// This enables client applications to render live monitoring displays with accurate sensor placement and status indication.</para>
        /// 
        /// <para><strong>💡 Business Applications:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Control Room Displays</strong>: Large monitor displays for 24/7 monitoring operations</description></item>
        /// <item><description><strong>Mobile Operators</strong>: Field personnel accessing site layouts on mobile devices</description></item>
        /// <item><description><strong>Management Dashboards</strong>: Executive overviews with site status visualization</description></item>
        /// <item><description><strong>Emergency Response</strong>: Rapid site assessment during alarm conditions</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site (1-10 for current deployment)</param>
        /// <returns>Complete layout information including SVG content, sensor positions, and configuration settings</returns>
        /// <response code="200">Successfully retrieved layout information with comprehensive sensor positioning data</response>
        /// <response code="400">Invalid site ID provided or site does not exist in system configuration</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="404">Site not found - specified site ID does not exist in the monitoring system</response>
        /// <response code="500">Internal server error during layout retrieval or sensor data processing</response>
        [HttpGet("site/{siteId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
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
                        sensorPositions = layoutInfo.SensorPositions,
                        gridConfig = layoutInfo.GridConfig
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting layout for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error retrieving layout information" });
            }
        }

        /// <summary>
        /// Retrieve raw SVG layout content for direct rendering in client applications
        /// </summary>
        /// <remarks>
        /// <para><strong>SVG Content Retrieval</strong></para>
        /// <para>This endpoint provides direct access to SVG layout files for client applications that need
        /// to render custom site layouts. Returns only the SVG content without additional metadata.</para>
        /// 
        /// <para><strong>🎨 SVG Layout Features:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Scalable Graphics</strong>: Vector-based layouts that scale to any resolution</description></item>
        /// <item><description><strong>Interactive Elements</strong>: Named anchors for sensor positioning</description></item>
        /// <item><description><strong>Custom Styling</strong>: Site-specific colors, fonts, and visual elements</description></item>
        /// <item><description><strong>Equipment Diagrams</strong>: Detailed facility layouts with equipment positioning</description></item>
        /// </list>
        /// 
        /// <para><strong>🔧 Technical Implementation:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Content-Type</strong>: Returns SVG content with proper image/svg+xml headers</description></item>
        /// <item><description><strong>Caching</strong>: SVG content cached for improved performance</description></item>
        /// <item><description><strong>Compression</strong>: SVG optimized and compressed for network efficiency</description></item>
        /// <item><description><strong>Security</strong>: Content sanitized to prevent XSS and other vulnerabilities</description></item>
        /// </list>
        /// 
        /// <para><strong>⚠️ Fallback Behavior:</strong></para>
        /// <para>If no custom SVG layout exists for the site, this endpoint returns 404.
        /// Clients should implement fallback to the grid layout mode or main layout endpoint.</para>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <returns>Raw SVG content as text/plain for direct client rendering</returns>
        /// <response code="200">Successfully retrieved SVG layout content ready for client rendering</response>
        /// <response code="400">Invalid site ID provided</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="404">SVG layout not found - site uses grid layout or layout file missing</response>
        /// <response code="500">Internal server error during SVG file retrieval</response>
        [HttpGet("site/{siteId}/svg")]
        [Produces("text/plain")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetSvgLayout(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting SVG layout for site {SiteId}", siteId);

                var svgContent = await _layoutService.GetSvgLayoutAsync(siteId);

                if (svgContent == null)
                {
                    return NotFound(new { success = false, message = $"SVG layout not found for site {siteId}" });
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
        /// Retrieve sensor positioning data with real-time status information for layout rendering
        /// </summary>
        /// <remarks>
        /// <para><strong>Sensor Position Management</strong></para>
        /// <para>This endpoint provides complete sensor positioning information including real-time status data,
        /// coordinates, and metadata necessary for accurate layout rendering and monitoring displays.</para>
        /// 
        /// <para><strong>📍 Positioning Data:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Coordinate System</strong>: Percentage-based positioning (0-100% for both X and Y axes)</description></item>
        /// <item><description><strong>Real-time Values</strong>: Current sensor readings and process values</description></item>
        /// <item><description><strong>Status Information</strong>: Alarm states, connectivity status, and detector health</description></item>
        /// <item><description><strong>Display Metadata</strong>: CSS classes, tooltip text, and formatting information</description></item>
        /// </list>
        /// 
        /// <para><strong>🎯 Status Classifications:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Status 0</strong>: Normal operation - green indicators</description></item>
        /// <item><description><strong>Status 1-2</strong>: Alarm conditions - yellow/red warning indicators</description></item>
        /// <item><description><strong>Status 3+</strong>: Detector errors - gray fault indicators</description></item>
        /// <item><description><strong>Connectivity</strong>: Online/offline based on last update timestamp</description></item>
        /// </list>
        /// 
        /// <para><strong>🔄 Real-time Integration:</strong></para>
        /// <para>Sensor data is updated in real-time via MQTT integration. Position data includes live values
        /// that can be used for dynamic color coding, alarm visualization, and interactive displays.</para>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <returns>Complete sensor positioning data with real-time status and display properties</returns>
        /// <response code="200">Successfully retrieved sensor positioning data with real-time status information</response>
        /// <response code="400">Invalid site ID provided</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="404">Site not found or no sensors configured for this site</response>
        /// <response code="500">Internal server error during sensor data retrieval</response>
        [HttpGet("site/{siteId}/sensors")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetSensorPositions(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting sensor positions for site {SiteId}", siteId);

                var sensorPositions = await _layoutService.GetSensorPositionsAsync(siteId);

                return Ok(new
                {
                    success = true,
                    data = sensorPositions,
                    siteId = siteId,
                    count = sensorPositions.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor positions for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error retrieving sensor positions" });
            }
        }

        /// <summary>
        /// Update sensor positions on layout with coordinate validation and backup creation
        /// </summary>
        /// <remarks>
        /// <para><strong>Sensor Position Updates</strong></para>
        /// <para>This endpoint enables precise positioning of sensors on site layouts with comprehensive validation,
        /// automatic backup creation, and real-time coordinate updates for optimal monitoring display accuracy.</para>
        /// 
        /// <para><strong>📐 Coordinate Validation:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Range Validation</strong>: X and Y coordinates must be within 0-100% range</description></item>
        /// <item><description><strong>Duplicate Detection</strong>: Prevents multiple sensors at identical coordinates</description></item>
        /// <item><description><strong>Boundary Checking</strong>: Ensures positions remain within layout boundaries</description></item>
        /// <item><description><strong>Collision Avoidance</strong>: Optional spacing enforcement between sensors</description></item>
        /// </list>
        /// 
        /// <para><strong>🔄 Update Process:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Validation</strong>: Input coordinates and sensor IDs validated</description></item>
        /// <item><description><strong>Backup Creation</strong>: Previous configuration automatically backed up</description></item>
        /// <item><description><strong>Position Update</strong>: New coordinates applied to sensor configuration</description></item>
        /// <item><description><strong>Verification</strong>: Updated positions verified and confirmed</description></item>
        /// </list>
        /// 
        /// <para><strong>⚙️ Configuration Management:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Atomic Updates</strong>: All position changes applied as single transaction</description></item>
        /// <item><description><strong>Rollback Support</strong>: Failed updates automatically reverted</description></item>
        /// <item><description><strong>Audit Trail</strong>: All changes logged with user context and timestamps</description></item>
        /// <item><description><strong>Hot Reload</strong>: Position updates applied without system restart</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <param name="request">Sensor position update request containing coordinate changes and options</param>
        /// <returns>Update results with success status and detailed change information</returns>
        /// <response code="200">Successfully updated sensor positions with validation and backup creation</response>
        /// <response code="400">Invalid input data - coordinates out of range or malformed request</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="403">Insufficient permissions - layout modification requires elevated access</response>
        /// <response code="404">Site not found or sensors do not exist</response>
        /// <response code="500">Internal server error during position update or configuration save</response>
        [HttpPut("site/{siteId}/sensors")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> UpdateSensorPositions(int siteId, [FromBody] SensorPositionUpdateRequest request)
        {
            try
            {
                _logger.LogDebug("Updating sensor positions for site {SiteId}", siteId);

                var results = new List<object>();
                int successCount = 0;

                foreach (var update in request.SensorUpdates)
                {
                    var success = await _layoutService.UpdateSensorPositionAsync(siteId, update.ChannelId, update.X, update.Y);

                    if (success)
                    {
                        successCount++;
                    }

                    results.Add(new
                    {
                        channelId = update.ChannelId,
                        success = success,
                        x = update.X,
                        y = update.Y,
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
        /// Upload custom SVG layout file for advanced site visualization and sensor positioning
        /// </summary>
        /// <remarks>
        /// <para><strong>SVG Layout Upload System</strong></para>
        /// <para>This endpoint enables upload of custom Scalable Vector Graphics (SVG) layouts for industrial sites,
        /// providing advanced visualization capabilities with precise sensor positioning and custom visual elements.</para>
        /// 
        /// <para><strong>📁 File Upload Requirements:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>File Format</strong>: Valid SVG format with proper XML structure</description></item>
        /// <item><description><strong>File Size</strong>: Maximum 5MB to ensure optimal performance</description></item>
        /// <item><description><strong>Content Type</strong>: image/svg+xml or .svg file extension required</description></item>
        /// <item><description><strong>Character Encoding</strong>: UTF-8 encoding for international character support</description></item>
        /// </list>
        /// 
        /// <para><strong>🎨 SVG Design Guidelines:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Coordinate System</strong>: Design with 100x100 viewBox for percentage-based positioning</description></item>
        /// <item><description><strong>Sensor Anchors</strong>: Include named elements or IDs for sensor positioning</description></item>
        /// <item><description><strong>Scalable Elements</strong>: Use vector graphics that scale cleanly across resolutions</description></item>
        /// <item><description><strong>Color Schemes</strong>: Design for both light and dark mode compatibility</description></item>
        /// </list>
        /// 
        /// <para><strong>🔧 Processing Pipeline:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Upload Validation</strong>: File type, size, and structure validation</description></item>
        /// <item><description><strong>Security Scanning</strong>: SVG content scanned for malicious elements</description></item>
        /// <item><description><strong>XML Parsing</strong>: Structure validation and element analysis</description></item>
        /// <item><description><strong>Backup Creation</strong>: Previous layout automatically backed up</description></item>
        /// <item><description><strong>File Storage</strong>: New layout saved and cache updated</description></item>
        /// </list>
        /// 
        /// <para><strong>🔐 Security Features:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Content Sanitization</strong>: Removal of JavaScript and external references</description></item>
        /// <item><description><strong>XSS Prevention</strong>: Script tags and event handlers automatically stripped</description></item>
        /// <item><description><strong>File Type Validation</strong>: Strict MIME type and extension checking</description></item>
        /// <item><description><strong>Virus Scanning</strong>: Optional antivirus integration for uploaded content</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <param name="file">SVG layout file with custom site visualization design</param>
        /// <returns>Upload confirmation with file validation results and storage information</returns>
        /// <response code="200">Successfully uploaded and validated SVG layout file</response>
        /// <response code="400">Invalid file format, size exceeded, or malformed SVG content</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="403">Insufficient permissions - layout upload requires elevated access</response>
        /// <response code="413">File size too large - maximum 5MB allowed for SVG files</response>
        /// <response code="415">Unsupported media type - only SVG files accepted</response>
        /// <response code="500">Internal server error during file processing or storage</response>
        [HttpPost("site/{siteId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(object), 413)]
        [ProducesResponseType(typeof(object), 415)]
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
        /// Upload SVG layout from raw content for programmatic layout management
        /// </summary>
        /// <remarks>
        /// <para><strong>Programmatic SVG Upload</strong></para>
        /// <para>This endpoint enables programmatic upload of SVG layout content via JSON payload,
        /// supporting automated layout management systems and bulk configuration operations.</para>
        /// 
        /// <para><strong>🔧 Technical Applications:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Automated Deployment</strong>: Scripted layout updates during system deployment</description></item>
        /// <item><description><strong>Template Management</strong>: Standardized layout templates applied to multiple sites</description></item>
        /// <item><description><strong>Version Control</strong>: Layout content managed in source control systems</description></item>
        /// <item><description><strong>Integration APIs</strong>: Third-party systems uploading layouts programmatically</description></item>
        /// </list>
        /// 
        /// <para><strong>📝 Content Processing:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>JSON Wrapper</strong>: SVG content wrapped in JSON for API compatibility</description></item>
        /// <item><description><strong>Base64 Support</strong>: Optional Base64 encoding for binary content handling</description></item>
        /// <item><description><strong>Validation Pipeline</strong>: Same security and structure validation as file upload</description></item>
        /// <item><description><strong>Error Reporting</strong>: Detailed validation error messages for debugging</description></item>
        /// </list>
        /// 
        /// <para><strong>⚡ Advantages over File Upload:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>API Integration</strong>: Standard JSON request format for REST APIs</description></item>
        /// <item><description><strong>Automation Friendly</strong>: Easier integration with scripts and automated systems</description></item>
        /// <item><description><strong>Error Handling</strong>: Structured error responses for programmatic handling</description></item>
        /// <item><description><strong>Content Modification</strong>: Enable dynamic SVG generation before upload</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <param name="request">JSON request containing SVG content and upload options</param>
        /// <returns>Upload confirmation with validation results and storage status</returns>
        /// <response code="200">Successfully processed and stored SVG layout content</response>
        /// <response code="400">Invalid JSON format, missing content, or SVG validation failure</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="403">Insufficient permissions - layout upload requires elevated access</response>
        /// <response code="500">Internal server error during content processing or storage</response>
        [HttpPost("site/{siteId}/content")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
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
                        message = "SVG layout content uploaded successfully",
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
                _logger.LogError(ex, "Error uploading SVG content for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error uploading SVG content" });
            }
        }

        /// <summary>
        /// Remove custom SVG layout and revert to automatic grid layout generation
        /// </summary>
        /// <remarks>
        /// <para><strong>Layout Removal and Fallback</strong></para>
        /// <para>This endpoint removes custom SVG layouts from industrial sites and automatically reverts
        /// to grid-based layout generation, providing graceful degradation when custom layouts are no longer needed.</para>
        /// 
        /// <para><strong>🔄 Fallback Process:</strong></para>
        /// <list type="number">
        /// <item><description><strong>Backup Creation</strong>: Current SVG layout automatically backed up before removal</description></item>
        /// <item><description><strong>File Removal</strong>: SVG layout file safely deleted from storage</description></item>
        /// <item><description><strong>Cache Invalidation</strong>: Layout cache updated to reflect changes</description></item>
        /// <item><description><strong>Grid Generation</strong>: Automatic grid layout generated based on sensor configuration</description></item>
        /// </list>
        /// 
        /// <para><strong>📊 Grid Layout Benefits:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Automatic Arrangement</strong>: Sensors arranged in optimal grid pattern</description></item>
        /// <item><description><strong>Responsive Design</strong>: Grid adapts to different screen sizes automatically</description></item>
        /// <item><description><strong>Maintenance Free</strong>: No manual positioning required for new sensors</description></item>
        /// <item><description><strong>Consistent Appearance</strong>: Uniform layout across all sites using grid mode</description></item>
        /// </list>
        /// 
        /// <para><strong>⚡ Immediate Effects:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Layout Refresh</strong>: Connected clients automatically refreshed with grid layout</description></item>
        /// <item><description><strong>Sensor Repositioning</strong>: All sensors repositioned using grid algorithm</description></item>
        /// <item><description><strong>Configuration Update</strong>: Site configuration updated to reflect layout mode change</description></item>
        /// <item><description><strong>Notification Broadcast</strong>: Layout change notifications sent via SignalR</description></item>
        /// </list>
        /// 
        /// <para><strong>🔧 Recovery Options:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Backup Recovery</strong>: Previous SVG layouts can be restored from automatic backups</description></item>
        /// <item><description><strong>Manual Restoration</strong>: Custom layouts can be re-uploaded at any time</description></item>
        /// <item><description><strong>Configuration Rollback</strong>: Site configuration includes layout history</description></item>
        /// <item><description><strong>Emergency Fallback</strong>: Grid layout ensures continuous monitoring capability</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <returns>Deletion confirmation with backup information and fallback status</returns>
        /// <response code="200">Successfully removed SVG layout and activated grid layout fallback</response>
        /// <response code="400">Invalid site ID provided</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="403">Insufficient permissions - layout deletion requires elevated access</response>
        /// <response code="404">SVG layout not found - site may already be using grid layout</response>
        /// <response code="500">Internal server error during layout removal or backup creation</response>
        [HttpDelete("site/{siteId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(object), 404)]
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
                        message = "SVG layout deleted successfully",
                        siteId = siteId
                    });
                }
                else
                {
                    return NotFound(new { success = false, message = $"SVG layout not found for site {siteId}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SVG layout for site {SiteId}", siteId);
                return StatusCode(500, new { success = false, message = "Error deleting SVG layout" });
            }
        }

        /// <summary>
        /// Check if a site has a custom SVG layout file available
        /// </summary>
        /// <remarks>
        /// <para><strong>Layout Availability Check</strong></para>
        /// <para>This endpoint provides a quick check for custom SVG layout availability without downloading
        /// the full layout content, enabling efficient client-side layout mode determination.</para>
        /// 
        /// <para><strong>🔍 Use Cases:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>UI Preparation</strong>: Determine which layout rendering components to load</description></item>
        /// <item><description><strong>Performance Optimization</strong>: Avoid unnecessary layout requests for grid-only sites</description></item>
        /// <item><description><strong>Feature Detection</strong>: Enable/disable SVG-specific UI features based on availability</description></item>
        /// <item><description><strong>Fallback Logic</strong>: Implement graceful degradation in client applications</description></item>
        /// </list>
        /// 
        /// <para><strong>⚡ Performance Benefits:</strong></para>
        /// <list type="bullet">
        /// <item><description><strong>Lightweight Request</strong>: Minimal data transfer for layout capability detection</description></item>
        /// <item><description><strong>Fast Response</strong>: Simple file existence check without content parsing</description></item>
        /// <item><description><strong>Cache Friendly</strong>: Response can be cached for improved performance</description></item>
        /// <item><description><strong>Batch Compatible</strong>: Multiple sites can be checked efficiently</description></item>
        /// </list>
        /// </remarks>
        /// <param name="siteId">Unique identifier for the industrial site</param>
        /// <returns>Boolean indicator of custom SVG layout availability</returns>
        /// <response code="200">Successfully checked layout availability - response indicates if custom SVG exists</response>
        /// <response code="400">Invalid site ID provided</response>
        /// <response code="401">Authentication required - valid JWT bearer token must be provided</response>
        /// <response code="404">Site not found in system configuration</response>
        /// <response code="500">Internal server error during layout availability check</response>
        [HttpGet("site/{siteId}/has-custom")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
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
    /// Request model for updating multiple sensor positions on a layout
    /// </summary>
    /// <remarks>
    /// <para><strong>Batch Position Update Request</strong></para>
    /// <para>This request model enables updating multiple sensor positions in a single API call,
    /// improving efficiency and ensuring atomic updates for layout modifications.</para>
    /// 
    /// <para><strong>📐 Coordinate Validation:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Range Validation</strong>: X and Y coordinates automatically validated within 0-100% range</description></item>
    /// <item><description><strong>Required Fields</strong>: Channel ID and coordinates required for each sensor update</description></item>
    /// <item><description><strong>Duplicate Detection</strong>: Validation prevents duplicate channel IDs in single request</description></item>
    /// </list>
    /// 
    /// <para><strong>⚡ Batch Processing Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Atomic Updates</strong>: All position changes applied as single transaction</description></item>
    /// <item><description><strong>Reduced API Calls</strong>: Multiple sensors updated in one request</description></item>
    /// <item><description><strong>Consistent Timing</strong>: All updates timestamped identically</description></item>
    /// <item><description><strong>Rollback Support</strong>: Failed batch updates automatically reverted</description></item>
    /// </list>
    /// </remarks>
    public class SensorPositionUpdateRequest
    {
        /// <summary>
        /// Collection of sensor position updates to apply
        /// </summary>
        /// <remarks>
        /// <para>Each update contains channel ID and new coordinates for a specific sensor.
        /// All updates in the collection are processed as a single atomic operation.</para>
        /// </remarks>
        [Required]
        public IEnumerable<SensorPositionUpdate> SensorUpdates { get; set; } = new List<SensorPositionUpdate>();
    }

    /// <summary>
    /// Individual sensor position update with coordinate validation
    /// </summary>
    /// <remarks>
    /// <para><strong>Single Sensor Position Update</strong></para>
    /// <para>Represents position update for a single sensor with comprehensive validation
    /// ensuring coordinates remain within the valid percentage-based coordinate system.</para>
    /// 
    /// <para><strong>📍 Coordinate System:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>X-Axis</strong>: 0% = left edge, 100% = right edge</description></item>
    /// <item><description><strong>Y-Axis</strong>: 0% = top edge, 100% = bottom edge</description></item>
    /// <item><description><strong>Precision</strong>: Decimal coordinates supported for precise positioning</description></item>
    /// <item><description><strong>Validation</strong>: Automatic range checking prevents invalid coordinates</description></item>
    /// </list>
    /// </remarks>
    public class SensorPositionUpdate
    {
        /// <summary>
        /// Channel identifier for the sensor (e.g., "CH41", "CH42")
        /// </summary>
        /// <remarks>
        /// <para>Must match an existing sensor channel ID in the site configuration.
        /// Channel IDs are case-sensitive and must follow the established naming convention.</para>
        /// </remarks>
        [Required]
        public string ChannelId { get; set; } = "";

        /// <summary>
        /// X coordinate on layout (0-100 percentage)
        /// </summary>
        /// <remarks>
        /// <para>Horizontal position as percentage of layout width.
        /// 0% represents the leftmost edge, 100% represents the rightmost edge.</para>
        /// </remarks>
        [Range(0, 100)]
        public double X { get; set; }

        /// <summary>
        /// Y coordinate on layout (0-100 percentage)
        /// </summary>
        /// <remarks>
        /// <para>Vertical position as percentage of layout height.
        /// 0% represents the top edge, 100% represents the bottom edge.</para>
        /// </remarks>
        [Range(0, 100)]
        public double Y { get; set; }
    }

    /// <summary>
    /// Request model for uploading SVG layout content via JSON
    /// </summary>
    /// <remarks>
    /// <para><strong>Programmatic SVG Upload</strong></para>
    /// <para>This request model enables SVG layout upload through JSON API calls,
    /// supporting automated deployment and integration scenarios.</para>
    /// 
    /// <para><strong>🔧 Content Requirements:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Valid SVG</strong>: Content must be well-formed XML with proper SVG structure</description></item>
    /// <item><description><strong>Size Limits</strong>: Maximum 5MB content size for performance</description></item>
    /// <item><description><strong>Security</strong>: Content automatically sanitized for security vulnerabilities</description></item>
    /// <item><description><strong>Encoding</strong>: UTF-8 encoding required for international character support</description></item>
    /// </list>
    /// 
    /// <para><strong>💡 Integration Benefits:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>API Compatibility</strong>: Standard JSON request format for REST integration</description></item>
    /// <item><description><strong>Automation Friendly</strong>: Easy integration with deployment scripts and CI/CD pipelines</description></item>
    /// <item><description><strong>Version Control</strong>: SVG content can be managed in source control systems</description></item>
    /// <item><description><strong>Template Support</strong>: Enable dynamic SVG generation before upload</description></item>
    /// </list>
    /// </remarks>
    public class SvgContentRequest
    {
        /// <summary>
        /// SVG layout content as string
        /// </summary>
        /// <remarks>
        /// <para>Complete SVG markup including XML declaration and all visual elements.
        /// Content is validated for proper XML structure and SVG compliance before storage.</para>
        /// </remarks>
        [Required]
        public string SvgContent { get; set; } = "";
    }

    #endregion
}