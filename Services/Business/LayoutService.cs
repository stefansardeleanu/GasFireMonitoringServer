// File: Services/Business/LayoutService.cs
// Layout management service implementation

using Microsoft.Extensions.Logging;
using GasFireMonitoringServer.Models.Configuration;
using GasFireMonitoringServer.Models.Entities;
using GasFireMonitoringServer.Repositories.Interfaces;
using GasFireMonitoringServer.Services.Business.Interfaces;
using System.Text.RegularExpressions;
using System.Xml;

namespace GasFireMonitoringServer.Services.Business
{
    /// <summary>
    /// Layout management service implementation
    /// Handles both SVG layouts and automatic grid layouts
    /// </summary>
    public class LayoutService : ILayoutService
    {
        private readonly IConfigurationService _configurationService;
        private readonly ISensorRepository _sensorRepository;
        private readonly ILogger<LayoutService> _logger;
        private readonly string _layoutsPath;

        public LayoutService(
            IConfigurationService configurationService,
            ISensorRepository sensorRepository,
            ILogger<LayoutService> logger)
        {
            _configurationService = configurationService;
            _sensorRepository = sensorRepository;
            _logger = logger;

            // Get layouts path from configuration service
            var paths = _configurationService.GetConfigurationPaths();
            _layoutsPath = paths.LayoutsPath;

            // Ensure layouts directory exists
            EnsureLayoutsDirectoryExists();
        }

        #region Public Interface Methods

        /// <summary>
        /// Get layout information for a site
        /// Automatically determines whether to use SVG or grid layout
        /// </summary>
        public async Task<SiteLayoutInfo> GetSiteLayoutAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting layout information for site {SiteId}", siteId);

                // Get site configuration for name
                var siteConfig = await _configurationService.GetSiteAsync(siteId);
                var siteName = siteConfig?.Name ?? $"Site {siteId}";

                // Check if custom SVG layout exists
                var hasCustomLayout = await HasCustomLayoutAsync(siteId);
                var siteLayoutInfo = new SiteLayoutInfo
                {
                    SiteId = siteId,
                    SiteName = siteName,
                    HasCustomLayout = hasCustomLayout
                };

                if (hasCustomLayout)
                {
                    // Load SVG layout
                    siteLayoutInfo.LayoutType = LayoutType.SVG;
                    siteLayoutInfo.SvgContent = await GetSvgLayoutAsync(siteId);

                    // Get sensor positions from configuration
                    siteLayoutInfo.SensorPositions = await GetSensorPositionsAsync(siteId);
                }
                else
                {
                    // Generate grid layout
                    siteLayoutInfo.LayoutType = LayoutType.Grid;
                    var gridResult = await GenerateGridLayoutAsync(siteId);
                    siteLayoutInfo.GridConfig = gridResult.Config;
                    siteLayoutInfo.SensorPositions = gridResult.Positions;
                }

                _logger.LogInformation("Layout information retrieved for site {SiteId}: {LayoutType}, {SensorCount} sensors",
                    siteId, siteLayoutInfo.LayoutType, siteLayoutInfo.SensorPositions.Count);

                return siteLayoutInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting layout for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Check if a site has a custom SVG layout file
        /// </summary>
        public async Task<bool> HasCustomLayoutAsync(int siteId)
        {
            try
            {
                var layoutPath = GetSvgLayoutPath(siteId);
                var exists = File.Exists(layoutPath);

                _logger.LogDebug("Checked SVG layout for site {SiteId}: {Exists} at {Path}",
                    siteId, exists, layoutPath);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking layout existence for site {SiteId}", siteId);
                return false;
            }
        }

        /// <summary>
        /// Get SVG layout content for a site
        /// </summary>
        public async Task<string?> GetSvgLayoutAsync(int siteId)
        {
            try
            {
                var layoutPath = GetSvgLayoutPath(siteId);

                if (!File.Exists(layoutPath))
                {
                    _logger.LogWarning("SVG layout file not found for site {SiteId} at {Path}", siteId, layoutPath);
                    return null;
                }

                var svgContent = await File.ReadAllTextAsync(layoutPath);

                // Validate SVG content
                if (!IsValidSvg(svgContent))
                {
                    _logger.LogWarning("Invalid SVG content in layout file for site {SiteId}", siteId);
                    return null;
                }

                _logger.LogDebug("SVG layout loaded for site {SiteId}, size: {Size} characters",
                    siteId, svgContent.Length);

                return svgContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading SVG layout for site {SiteId}", siteId);
                return null;
            }
        }

        /// <summary>
        /// Generate automatic grid layout for a site
        /// Used when no SVG layout is available
        /// </summary>
        public async Task<GridLayoutResult> GenerateGridLayoutAsync(int siteId, GridLayoutConfig? customConfig = null)
        {
            try
            {
                _logger.LogDebug("Generating grid layout for site {SiteId}", siteId);

                // Get sensors for the site
                var sensors = await _sensorRepository.GetBySiteIdAsync(siteId);
                var sensorList = sensors.ToList();

                // Generate optimal grid configuration
                var config = customConfig ?? GenerateOptimalGridConfig(sensorList.Count);

                // Calculate actual rows needed based on sensor count and columns
                var actualRows = (int)Math.Ceiling((double)sensorList.Count / config.Columns);

                // Calculate sensor positions in grid
                var positions = new List<SensorLayoutPosition>();
                var marginX = 15.0; // Fixed margin
                var marginY = 15.0; // Fixed margin

                for (int i = 0; i < sensorList.Count; i++)
                {
                    var sensor = sensorList[i];
                    var row = i / config.Columns;
                    var col = i % config.Columns;

                    // Calculate percentage positions (0-100%)
                    var x = marginX + (col * (100.0 - 2 * marginX) / Math.Max(1, config.Columns - 1));
                    var y = marginY + (row * (100.0 - 2 * marginY) / Math.Max(1, actualRows - 1));

                    // Ensure coordinates are within bounds
                    x = Math.Max(marginX, Math.Min(100.0 - marginX, x));
                    y = Math.Max(marginY, Math.Min(100.0 - marginY, y));

                    var position = new SensorLayoutPosition
                    {
                        SensorKey = $"{sensor.SiteId}_{sensor.ChannelId}",
                        SiteId = sensor.SiteId,
                        ChannelId = sensor.ChannelId,
                        X = x,
                        Y = y,
                        DisplayName = sensor.TagName ?? $"CH{sensor.ChannelId}",
                        DetectorType = sensor.DetectorType,
                        Status = sensor.Status,
                        StatusText = sensor.StatusText ?? "",
                        IsOnline = sensor.LastUpdated > DateTime.UtcNow.AddMinutes(-5)
                    };

                    positions.Add(position);
                }

                var result = new GridLayoutResult
                {
                    Positions = positions,
                    Columns = config.Columns,
                    Rows = actualRows,
                    Config = config
                };

                _logger.LogInformation("Generated {Columns}x{Rows} grid layout for site {SiteId} with {SensorCount} sensors",
                    config.Columns, actualRows, siteId, positions.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating grid layout for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Get sensor positions for a site (from configuration or generated grid)
        /// </summary>
        public async Task<List<SensorLayoutPosition>> GetSensorPositionsAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Getting sensor positions for site {SiteId}", siteId);

                // Get sensors from database with current status
                var sensors = await _sensorRepository.GetBySiteIdAsync(siteId);
                var sensorList = sensors.ToList();

                // Check if we have custom SVG layout
                if (await HasCustomLayoutAsync(siteId))
                {
                    // Get positions from configuration
                    var configuredSensors = await _configurationService.GetSensorsBySiteAsync(siteId);
                    var positions = new List<SensorLayoutPosition>();

                    foreach (var sensor in sensorList)
                    {
                        // Find configured position
                        var configuredSensor = configuredSensors.FirstOrDefault(cs =>
                            cs.SiteId == sensor.SiteId && cs.ChannelId == sensor.ChannelId);

                        var position = new SensorLayoutPosition
                        {
                            SensorKey = $"{sensor.SiteId}_{sensor.ChannelId}",
                            SiteId = sensor.SiteId,
                            ChannelId = sensor.ChannelId,
                            X = configuredSensor?.LayoutX ?? 50.0, // Default to center if not configured
                            Y = configuredSensor?.LayoutY ?? 50.0,
                            DisplayName = sensor.TagName ?? $"CH{sensor.ChannelId}",
                            DetectorType = sensor.DetectorType,
                            Status = sensor.Status,
                            StatusText = sensor.StatusText ?? "",
                            IsOnline = sensor.LastUpdated > DateTime.UtcNow.AddMinutes(-5)
                        };

                        positions.Add(position);
                    }

                    return positions;
                }
                else
                {
                    // Generate grid positions
                    var gridResult = await GenerateGridLayoutAsync(siteId);
                    return gridResult.Positions;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor positions for site {SiteId}", siteId);
                throw;
            }
        }

        /// <summary>
        /// Update sensor position in configuration
        /// </summary>
        public async Task<bool> UpdateSensorPositionAsync(int siteId, string channelId, double x, double y)
        {
            try
            {
                _logger.LogDebug("Updating sensor position for site {SiteId}, channel {ChannelId} to ({X}, {Y})",
                    siteId, channelId, x, y);

                // Validate coordinates
                if (x < 0 || x > 100 || y < 0 || y > 100)
                {
                    _logger.LogWarning("Invalid coordinates for sensor {SiteId}/{ChannelId}: ({X}, {Y})",
                        siteId, channelId, x, y);
                    return false;
                }

                // Update configuration service
                var success = await _configurationService.UpdateSensorPositionAsync(siteId, channelId, x, y);

                if (success)
                {
                    _logger.LogInformation("Updated sensor position for {SiteId}/{ChannelId} to ({X}, {Y})",
                        siteId, channelId, x, y);
                }
                else
                {
                    _logger.LogWarning("Failed to update sensor position for {SiteId}/{ChannelId}", siteId, channelId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sensor position for site {SiteId}, channel {ChannelId}",
                    siteId, channelId);
                return false;
            }
        }

        /// <summary>
        /// Upload or update SVG layout file for a site
        /// </summary>
        public async Task<bool> SaveSvgLayoutAsync(int siteId, string svgContent)
        {
            try
            {
                _logger.LogDebug("Saving SVG layout for site {SiteId}", siteId);

                // Validate SVG content
                if (!IsValidSvg(svgContent))
                {
                    _logger.LogWarning("Invalid SVG content provided for site {SiteId}", siteId);
                    return false;
                }

                var layoutPath = GetSvgLayoutPath(siteId);

                // Create backup if file exists
                if (File.Exists(layoutPath))
                {
                    var backupPath = $"{layoutPath}.backup.{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                    File.Copy(layoutPath, backupPath);
                    _logger.LogInformation("Created backup of existing layout: {BackupPath}", backupPath);
                }

                // Save new SVG content
                await File.WriteAllTextAsync(layoutPath, svgContent);

                _logger.LogInformation("SVG layout saved for site {SiteId} at {Path}", siteId, layoutPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving SVG layout for site {SiteId}", siteId);
                return false;
            }
        }

        /// <summary>
        /// Delete SVG layout file for a site (will fallback to grid)
        /// </summary>
        public async Task<bool> DeleteSvgLayoutAsync(int siteId)
        {
            try
            {
                _logger.LogDebug("Deleting SVG layout for site {SiteId}", siteId);

                var layoutPath = GetSvgLayoutPath(siteId);

                if (!File.Exists(layoutPath))
                {
                    _logger.LogWarning("SVG layout file not found for deletion: site {SiteId}", siteId);
                    return false;
                }

                // Create backup before deletion
                var backupPath = $"{layoutPath}.deleted.{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                File.Move(layoutPath, backupPath);

                _logger.LogInformation("SVG layout deleted for site {SiteId}, backup created at {BackupPath}",
                    siteId, backupPath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SVG layout for site {SiteId}", siteId);
                return false;
            }
        }

        /// <summary>
        /// Generate optimal grid configuration based on sensor count and types
        /// </summary>
        public GridLayoutConfig GenerateOptimalGridConfig(int sensorCount, double aspectRatio = 1.0)
        {
            try
            {
                _logger.LogDebug("Generating optimal grid config for {SensorCount} sensors, aspect ratio {AspectRatio}",
                    sensorCount, aspectRatio);

                if (sensorCount <= 0)
                {
                    return new GridLayoutConfig
                    {
                        Columns = 1,
                        Style = "square",
                        Spacing = 10.0,
                        GroupByType = false,
                        ShowLabels = true
                    };
                }

                // Calculate optimal grid dimensions
                int columns;

                if (sensorCount <= 4)
                {
                    // Small grids: prefer 2x2 or smaller
                    columns = Math.Min(2, sensorCount);
                }
                else if (sensorCount <= 9)
                {
                    // Medium grids: prefer 3x3
                    columns = 3;
                }
                else if (sensorCount <= 16)
                {
                    // Larger grids: prefer 4x4
                    columns = 4;
                }
                else
                {
                    // Very large grids: calculate based on aspect ratio
                    var idealColumns = Math.Sqrt(sensorCount * aspectRatio);
                    columns = Math.Max(1, (int)Math.Round(idealColumns));
                }

                var config = new GridLayoutConfig
                {
                    Columns = columns,
                    Style = "square",
                    Spacing = 10.0,
                    GroupByType = true,
                    ShowLabels = true
                };

                _logger.LogDebug("Generated grid config: {Columns} columns for {SensorCount} sensors",
                    columns, sensorCount);

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating optimal grid config for {SensorCount} sensors", sensorCount);

                // Return safe default
                return new GridLayoutConfig
                {
                    Columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(sensorCount))),
                    Style = "square",
                    Spacing = 10.0,
                    GroupByType = false,
                    ShowLabels = true
                };
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Get the file path for a site's SVG layout
        /// </summary>
        private string GetSvgLayoutPath(int siteId)
        {
            return Path.Combine(_layoutsPath, $"site_{siteId}_layout.svg");
        }

        /// <summary>
        /// Ensure the layouts directory exists
        /// </summary>
        private void EnsureLayoutsDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_layoutsPath))
                {
                    Directory.CreateDirectory(_layoutsPath);
                    _logger.LogInformation("Created layouts directory: {LayoutsPath}", _layoutsPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating layouts directory: {LayoutsPath}", _layoutsPath);
            }
        }

        /// <summary>
        /// Validate SVG content
        /// </summary>
        private bool IsValidSvg(string svgContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(svgContent))
                    return false;

                // Check for SVG tag
                if (!svgContent.Contains("<svg", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Try to parse as XML to validate structure
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(svgContent);

                // Check root element is SVG
                return xmlDoc.DocumentElement?.Name.Equals("svg", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}