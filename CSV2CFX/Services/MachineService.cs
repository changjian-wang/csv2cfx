using CSV2CFX.AppSettings;
using CSV2CFX.Extensions;
using CSV2CFX.Interfaces;
using CSV2CFX.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CSV2CFX.Services
{
    public class MachineService : IMachineService
    {
        private readonly ILogger _logger;
        private readonly IOptions<MachineStatusSetting> _machineStatusOptions;
        private readonly IOptions<MachineMetadataSetting> _machineMetadataOptions;
        private readonly IOptions<CsvFilePathSetting> _csvFolderPathOptions;
        private readonly IOptions<RabbitMQPublisherSettings> _rabbitMQPublisherOptions;
        private readonly IRabbitMQService _rabbitMQService;
        private const string QUEUE_SUFFIX = "queue";
        private const string EXCHANGE_SUFFIX = "exchange";
        private const string ROUTINGKEY_SUFFIX = "routing-key";
        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = false, 
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public MachineService(
            ILogger<MachineService> logger,
            IOptions<MachineStatusSetting> machineStatusOptions,
            IOptions<MachineMetadataSetting> machineMetadataOptions,
            IOptions<CsvFilePathSetting> csvFolderPathOptions,
            IOptions<RabbitMQPublisherSettings> rabbitMQPublisherOptions,
            IRabbitMQService rabbitMQService)
        {
            _logger = logger;
            _machineStatusOptions = machineStatusOptions;
            _machineMetadataOptions = machineMetadataOptions;
            _csvFolderPathOptions = csvFolderPathOptions;
            _rabbitMQService = rabbitMQService;
            _rabbitMQPublisherOptions = rabbitMQPublisherOptions;
        }

        /// <summary>
        /// Create RabbitMQ queues, exchanges, and bindings for CFX messages.
        /// </summary>
        /// <returns></returns>
        public async Task CreateRabbitmqAsync(string uniqueId)
        {
            var keyValues = new Dictionary<string, string>
            {
                ["heartbeat"] = $"{_rabbitMQPublisherOptions.Value.Prefix}.heartbeat",
                ["workstarted"] = $"{_rabbitMQPublisherOptions.Value.Prefix}.workstarted",
                ["workcompleted"] = $"{_rabbitMQPublisherOptions.Value.Prefix}.workcompleted",
                ["unitsprocessed"] = $"{_rabbitMQPublisherOptions.Value.Prefix}.unitsprocessed",
                ["stationstatechanged"] = $"{_rabbitMQPublisherOptions.Value.Prefix}.stationstatechanged",
                ["faultoccurred"] = $"{_rabbitMQPublisherOptions.Value.Prefix}.faultoccurred",
                ["faultcleared"] = $"{_rabbitMQPublisherOptions.Value.Prefix}.faultcleared",
            };

            var exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
            await _rabbitMQService.CreateExchangeAsync(exchangeName);

            foreach (var item in keyValues)
            {
                var queueName = $"{item.Value}.{QUEUE_SUFFIX}";
                var routingKey = $"{item.Value}.{ROUTINGKEY_SUFFIX}";

                await _rabbitMQService.CreateQueueAsync(queueName);
                await _rabbitMQService.BindQueueAsync(queueName, exchangeName, routingKey);
            }
        }

        /// <summary>
        /// Publishes a heartbeat message to a RabbitMQ exchange, indicating the current status of the machine.
        /// </summary>
        /// <remarks>This method constructs a heartbeat message containing metadata about the machine,
        /// such as its location, name, and process type, along with other status information. The message is serialized
        /// to JSON and published to a RabbitMQ exchange. The method also includes a delay based on the configured
        /// heartbeat frequency to ensure periodic message publishing.</remarks>
        /// <param name="uniqueId">A unique identifier for the machine, used to construct the RabbitMQ exchange, queue, and routing key names.</param>
        /// <returns></returns>
        public async Task PublishHeartbeatAsync(string uniqueId)
        {
            var body = new Dictionary<string, dynamic?>
            {
                ["$type"] = $"{_machineStatusOptions.Value.Heartbeat}, CFX",
                ["CFXHandle"] = Guid.NewGuid().ToString(),
                ["HeartbeatFrequency"] = _machineStatusOptions.Value.HeartbeatFrequency,
                ["ActiveFaults"] = 0,
                ["ActiveRecipes"] = Array.Empty<object>(),
                ["Metadata"] = new Dictionary<string, string>
                {
                    ["building"] = _machineMetadataOptions.Value.Building ?? "",
                    ["device"] = _machineMetadataOptions.Value.Device ?? "",
                    ["area_name"] = _machineMetadataOptions.Value.AreaName ?? "",
                    ["org"] = _machineMetadataOptions.Value.Organization ?? "",
                    ["line_name"] = _machineMetadataOptions.Value.LineName ?? "",
                    ["site_name"] = _machineMetadataOptions.Value.SiteName ?? "",
                    ["station_name"] = _machineMetadataOptions.Value.StationName ?? "",
                    ["Process_type"] = _machineMetadataOptions.Value.ProcessType ?? "",
                    ["machine_name"] = _machineMetadataOptions.Value.MachineName ?? "",
                    ["Created_by"] = _machineMetadataOptions.Value.CreatedBy ?? "",
                }
            };

            var json = new CFXJsonModel
            {
                MessageName = _machineStatusOptions.Value.Heartbeat,
                Version = _machineStatusOptions.Value.Version,
                TimeStamp = DateTime.UtcNow.FormatDateTimeToIso8601(0),
                UniqueID = _machineStatusOptions.Value.UniqueId,
                Source = _machineStatusOptions.Value.UniqueId,
                Target = null,
                RequestID = Guid.NewGuid().ToString(),
                MessageBody = body
            };

            var exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
            var routingKey = $"{_rabbitMQPublisherOptions.Value.Prefix}.heartbeat.{ROUTINGKEY_SUFFIX}";
            var queueName = $"{_rabbitMQPublisherOptions.Value.Prefix}.heartbeat.{QUEUE_SUFFIX}";
            var message = JsonSerializer.Serialize(json, options);
            
            await _rabbitMQService.PublishMessageAsync(exchangeName, routingKey, message);

            // 兼容字符串类型的心跳消息
            await Task.Delay(Convert.ToInt32(_machineStatusOptions.Value.HeartbeatFrequency) * 1000).ConfigureAwait(false);
        }

        /// <summary>
        /// Publishes a "Work Started" message for each production record in the specified CSV file.
        /// </summary>
        /// <remarks>This method reads production data from a CSV file located in the configured process
        /// data folder. If the file does not exist or the folder path is invalid, the method exits without performing
        /// any action.  For each production record in the file, the method constructs a message containing production
        /// details and metadata, and publishes it to a RabbitMQ exchange. The method ensures that the original file is
        /// backed up and deleted after processing.  Exceptions during message publishing are logged, and the file is
        /// deleted in the <c>finally</c> block to ensure cleanup.</remarks>
        /// <param name="uniqueId">A unique identifier used to construct the RabbitMQ exchange, queue, and routing key names.</param>
        /// <returns></returns>
        public async Task PublishWorkProcessAsync(string uniqueId)
        {
            var filePath = _csvFolderPathOptions.Value.ProductionInformationFilePath ?? "";
            var copyFilePath = $"{filePath}.backup.csv";
            
            if (!File.Exists(filePath) && !File.Exists(copyFilePath))
            {
                return;
            }

            if (!File.Exists(copyFilePath))
            {
                File.Copy(filePath, copyFilePath, true);
                File.Delete(filePath);
            }

            filePath = copyFilePath;

            var lines = await File.ReadAllLinesAsync(filePath);

            try
            {
                foreach (var line in lines.Skip(1))
                {
                    var columns = line.Split(',');

                    ProductionInfo production = new ProductionInfo
                    {
                        ProductModel = columns[0].Trim(),
                        SN = columns[1].Trim(),
                        PartNum = columns[2].Trim(),
                        CT = columns[3].Trim(),
                        Result = columns[4].Trim(),
                        StartTime = columns[5].Trim(),
                        EndTime = columns[6].Trim()
                    };

                    // workstarted
                    await PublishWorkStartedAsync(uniqueId, production).ConfigureAwait(false);

                    // unitsprocessed
                    await PublishUnitsProcessedAsync(uniqueId, production).ConfigureAwait(false);

                    // workcompleted
                    await PublishWorkCompletedAsync(uniqueId, production).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing WorkStarted messages.");
                return;
            }
            finally
            {
                File.Delete(filePath);
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        // workstarted
        private async Task PublishWorkStartedAsync(string uniqueId, ProductionInfo production)
        {
            var body = new Dictionary<string, dynamic?>
            {
                ["$type"] = $"{_machineStatusOptions.Value.WorkStarted}, CFX",
                ["PrimaryIdentifier"] = production.SN,
                ["HermesIdentifier"] = null,
                ["TransactionID"] = Guid.NewGuid().ToString(),
                ["Line"] = 1,
                ["UnitCount"] = null,
                ["Units"] = Array.Empty<object>(),
                ["Metadata"] = new Dictionary<string, string>
                {
                    ["building"] = _machineMetadataOptions.Value.Building ?? "",
                    ["device"] = _machineMetadataOptions.Value.Device ?? "",
                    ["area_name"] = _machineMetadataOptions.Value.AreaName ?? "",
                    ["org"] = _machineMetadataOptions.Value.Organization ?? "",
                    ["line_name"] = _machineMetadataOptions.Value.LineName ?? "",
                    ["site_name"] = _machineMetadataOptions.Value.SiteName ?? "",
                    ["station_name"] = _machineMetadataOptions.Value.StationName ?? "",
                    ["Process_type"] = _machineMetadataOptions.Value.ProcessType ?? "",
                    ["machine_name"] = _machineMetadataOptions.Value.MachineName ?? "",
                    ["Created_by"] = _machineMetadataOptions.Value.CreatedBy ?? "",
                }
            };

            var json = new CFXJsonModel
            {
                MessageName = _machineStatusOptions.Value.WorkStarted,
                Version = _machineStatusOptions.Value.Version,
                TimeStamp = Convert.ToDateTime(production.StartTime).FormatDateTimeToIso8601(8),
                UniqueID = _machineStatusOptions.Value.UniqueId,
                Source = _machineStatusOptions.Value.UniqueId,
                Target = null,
                RequestID = Guid.NewGuid().ToString(),
                MessageBody = body
            };

            var exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
            var routingKey = $"{_rabbitMQPublisherOptions.Value.Prefix}.workstarted.{ROUTINGKEY_SUFFIX}";
            var queueName = $"{_rabbitMQPublisherOptions.Value.Prefix}.workstarted.{QUEUE_SUFFIX}";
            var message = JsonSerializer.Serialize(json, options);

            // workstarted
            await _rabbitMQService.PublishMessageAsync(exchangeName, routingKey, message);
        }

        // unitsprocessed
        private async Task PublishUnitsProcessedAsync(string uniqueId, ProductionInfo production)
        {
            var directoryPath = _csvFolderPathOptions.Value.ProcessDataFilesFilePath ?? "";
            var files = Directory.GetFiles(directoryPath, "*.csv");
            var filePath = files.Where(s => Path.GetFileNameWithoutExtension(s).StartsWith(production.SN ?? "")).FirstOrDefault();

            var copyFilePath = $"{filePath}.backup.csv";

            if (!File.Exists(filePath) && !File.Exists(copyFilePath))
            {
                return;
            }

            if (!File.Exists(copyFilePath))
            {
                File.Copy(filePath, copyFilePath, true);
                File.Delete(filePath);
            }

            filePath = copyFilePath;

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath, encoding: System.Text.Encoding.UTF8);
                var list = lines.Where(s => IsValidDateTime(s.Split(',')[0]));
                var personalizedUnits = new List<PersonalizedUnit>();
                var names = lines[1].Split(',');
                var count = 1;

                foreach (var item in list)
                {
                    var columns = item.Split(',');

                    personalizedUnits.Add(new PersonalizedUnit
                    {
                        Name = $"{names[1]}{count}",
                        Unit = "Nm",
                        Value = Convert.ToDecimal(columns[1]),
                        Hilim = "",
                        Lolim = "",
                        Status = columns[3],
                        Rule = "",
                        Target = ""
                    });

                    personalizedUnits.Add(new PersonalizedUnit
                    {
                        Name = $"{names[2]}{count++}",
                        Unit = "degree",
                        Value = Convert.ToDecimal(columns[2]),
                        Hilim = "",
                        Lolim = "",
                        Status = columns[3],
                        Rule = "",
                        Target = ""
                    });
                }

                var body = new Dictionary<string, dynamic?>
                {
                    ["$type"] = $"CFX.Structures.SolderReflow.ReflowProcessData, CFX",
                    ["TransactionID"] = Guid.NewGuid().ToString(),
                    ["OverallResult"] = production.Result,
                    ["CommonProcessData"] = new Dictionary<string, dynamic>
                    {
                        ["$type"] = "CFX.Structures.ProccessData, CFX",
                        ["PersonalizedUnits"] = personalizedUnits
                    },
                    ["Metadata"] = new Dictionary<string, string>
                    {
                        ["building"] = _machineMetadataOptions.Value.Building ?? "",
                        ["device"] = _machineMetadataOptions.Value.Device ?? "",
                        ["area_name"] = _machineMetadataOptions.Value.AreaName ?? "",
                        ["org"] = _machineMetadataOptions.Value.Organization ?? "",
                        ["line_name"] = _machineMetadataOptions.Value.LineName ?? "",
                        ["site_name"] = _machineMetadataOptions.Value.SiteName ?? "",
                        ["station_name"] = _machineMetadataOptions.Value.StationName ?? "",
                        ["Process_type"] = _machineMetadataOptions.Value.ProcessType ?? "",
                        ["machine_name"] = _machineMetadataOptions.Value.MachineName ?? "",
                        ["Created_by"] = _machineMetadataOptions.Value.CreatedBy ?? "",
                    },
                    ["UnitProcessData"] = Array.Empty<object>()
                };
                var json = new Dictionary<string, dynamic?>
                {
                    ["MessageName"] = _machineStatusOptions.Value.UnitsProcessed ?? "",
                    ["Version"] = _machineStatusOptions.Value.Version ?? "",
                    ["TimeStamp"] = Convert.ToDateTime(production.EndTime).FormatDateTimeToIso8601(8),
                    ["UniqueID"] = _machineStatusOptions.Value.UniqueId ?? "",
                    ["Source"] = _machineStatusOptions.Value.UniqueId ?? "",
                    ["Target"] = null,
                    ["RequestID"] = null,
                    ["RecipeName"] = null,
                    ["MessageBody"] = body
                };
                var exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
                var routingKey = $"{_rabbitMQPublisherOptions.Value.Prefix}.unitsprocessed.{ROUTINGKEY_SUFFIX}";
                var queueName = $"{_rabbitMQPublisherOptions.Value.Prefix}.unitsprocessed.{QUEUE_SUFFIX}";
                var message = JsonSerializer.Serialize(json, options);

                // unitsprocessed
                await _rabbitMQService.PublishMessageAsync(exchangeName, routingKey, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing UnitsProcessed messages.");
                return;
            }
            finally
            {
                File.Delete(filePath);
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        // workcompleted
        private async Task PublishWorkCompletedAsync(string uniqueId, ProductionInfo production)
        {
            var body = new Dictionary<string, dynamic?>
            {
                ["$type"] = $"{_machineStatusOptions.Value.WorkCompleted}, CFX",
                ["PrimaryIdentifier"] = production.SN,
                ["HermesIdentifier"] = null,
                ["TransactionID"] = Guid.NewGuid().ToString(),
                ["Result"] = production.Result,
                ["UnitCount"] = null,
                ["Units"] = Array.Empty<object>(),
                ["PerformanceImpacts"] = Array.Empty<object>(),
                ["Metadata"] = new Dictionary<string, string>
                {
                    ["building"] = _machineMetadataOptions.Value.Building ?? "",
                    ["device"] = _machineMetadataOptions.Value.Device ?? "",
                    ["area_name"] = _machineMetadataOptions.Value.AreaName ?? "",
                    ["org"] = _machineMetadataOptions.Value.Organization ?? "",
                    ["line_name"] = _machineMetadataOptions.Value.LineName ?? "",
                    ["site_name"] = _machineMetadataOptions.Value.SiteName ?? "",
                    ["station_name"] = _machineMetadataOptions.Value.StationName ?? "",
                    ["Process_type"] = _machineMetadataOptions.Value.ProcessType ?? "",
                    ["machine_name"] = _machineMetadataOptions.Value.MachineName ??  "",
                    ["Created_by"] = _machineMetadataOptions.Value.CreatedBy ?? "",
                }
            };
            var json = new CFXJsonModel
            {
                MessageName = _machineStatusOptions.Value.WorkCompleted,
                Version = _machineStatusOptions.Value.Version,
                TimeStamp = Convert.ToDateTime(production.EndTime).FormatDateTimeToIso8601(8),
                UniqueID = _machineStatusOptions.Value.UniqueId,
                Source = _machineStatusOptions.Value.UniqueId,
                Target = null,
                RequestID = null,
                MessageBody = body
            };
            var exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
            var routingKey = $"{_rabbitMQPublisherOptions.Value.Prefix}.workcompleted.{ROUTINGKEY_SUFFIX}";
            var queueName = $"{_rabbitMQPublisherOptions.Value.Prefix}.workcompleted.{QUEUE_SUFFIX}";
            var message = JsonSerializer.Serialize(json, options);

            // workcompleted
            await _rabbitMQService.PublishMessageAsync(exchangeName, routingKey, message);
        }

        /// <summary>
        /// Publishes the machine state information from a CSV file.
        /// </summary>
        /// <returns></returns>
        public async Task PublishMachineStateAsync(string uniqueId)
        {
            var filePath = _csvFolderPathOptions.Value.MachineStatusInformationFilePath ?? "";

            var copyFilePath = $"{filePath}.backup.csv";

            if (!File.Exists(filePath) && !File.Exists(copyFilePath))
            {
                return;
            }

            if (!File.Exists(copyFilePath))
            {
                File.Copy(filePath, copyFilePath, true);
                File.Delete(filePath);
            }

            filePath = copyFilePath;

            var lines = await File.ReadAllLinesAsync(filePath);
            var list = new List<MachineStatusInfo>();

            try
            {
                foreach (var line in lines.Skip(1))
                {
                    var columns = line.Split(',');

                    list.Add(new MachineStatusInfo
                    {
                        OPTime = columns[0],
                        Status = string.IsNullOrWhiteSpace(columns[1]) ? null : Convert.ToInt32(columns[1]),
                        ErrorID = string.IsNullOrWhiteSpace(columns[2]) ? null : Convert.ToInt32(columns[2]),
                        ErrorMsg = columns[3]
                    });
                }

                // faultoccurred
                var lastErrorIndex = list.FindLastIndex(s => s.Status == (int)MAPBasicStatusCode.Error);
                var lastError = list[lastErrorIndex];
                
                var guid = Guid.NewGuid().ToString();
                var faultOccurredJson = new Dictionary<string, dynamic?>
                {
                    ["MessageName"] = _machineStatusOptions.Value.FaultOccurred,
                    ["Version"] = _machineStatusOptions.Value.Version,
                    ["TimeStamp"] = Convert.ToDateTime(lastError.OPTime).FormatDateTimeToIso8601(8),
                    ["UniqueID"] = uniqueId,
                    ["Source"] = uniqueId,
                    ["Target"] = null,
                    ["RequestID"] = null,
                    ["MessageBody"] = new Dictionary<string, dynamic?>
                    {
                        ["$type"] = $"{_machineStatusOptions.Value.FaultOccurred}, CFX",
                        ["Fault"] = new Dictionary<string, dynamic?>
                        {
                            ["TransactionID"] = guid,
                            ["Cause"] = lastError.ErrorMsg,
                            ["Severity"] = "Information",
                            ["FaultCode"] = lastError.ErrorID,
                            ["FaultOccurrenceId"] = guid,
                            ["Lane"] = 1,
                            ["Stage"] = new Dictionary<string, dynamic>
                            {
                                ["StageSequence"] = 4,
                                ["StageName"] = "Map_Inspection_4",
                                ["StageType"] = "Inspection"
                            },
                            ["SiteLocation"] = "Unknown",
                            ["AccessType"] = "Unknown",
                            ["Description"] = "",
                            ["DescriptionTranslation"] = new Dictionary<string, dynamic>
                            {
                                ["bool"] = false
                            },
                            ["OccurredAt"] = Convert.ToDateTime(lastError.OPTime).FormatDateTimeToIso8601(8),
                            ["DueDateTime"] = null
                        },
                        ["Metadata"] = new Dictionary<string, string>
                        {
                            ["building"] = _machineMetadataOptions.Value.Building ?? "",
                            ["device"] = _machineMetadataOptions.Value.Device ?? "",
                            ["area_name"] = _machineMetadataOptions.Value.AreaName ?? "",
                            ["org"] = _machineMetadataOptions.Value.Organization ?? "",
                            ["line_name"] = _machineMetadataOptions.Value.LineName ?? "",
                            ["site_name"] = _machineMetadataOptions.Value.SiteName ?? "",
                            ["station_name"] = _machineMetadataOptions.Value.StationName ?? "",
                            ["Process_type"] = _machineMetadataOptions.Value.ProcessType ?? "",
                            ["machine_name"] = _machineMetadataOptions.Value.MachineName ?? "",
                            ["Created_by"] = _machineMetadataOptions.Value.CreatedBy ?? "",
                        }
                    }
                };

                var faultoccurred_exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
                var faultoccurred_routingKey = $"{_rabbitMQPublisherOptions.Value.Prefix}.faultoccurred.{ROUTINGKEY_SUFFIX}";
                var faultoccurred_queueName = $"{_rabbitMQPublisherOptions.Value.Prefix}.faultoccurred.{QUEUE_SUFFIX}";
                var faultoccurred_message = JsonSerializer.Serialize(faultOccurredJson, options);

                await _rabbitMQService.PublishMessageAsync(faultoccurred_exchangeName, faultoccurred_routingKey, faultoccurred_message);

                // faultcleared
                if (list.Count-1 == lastErrorIndex)
                {
                    return;
                }

                var lastClearErrorOPTime = list[lastErrorIndex + 1].OPTime;
                var faultClearedJson = new Dictionary<string, dynamic?>
                {
                    ["MessageName"] = _machineStatusOptions.Value.FaultCleared,
                    ["Version"] = _machineStatusOptions.Value.Version,
                    ["TimeStamp"] = Convert.ToDateTime(lastClearErrorOPTime).FormatDateTimeToIso8601(8),
                    ["UniqueID"] = uniqueId,
                    ["Source"] = uniqueId,
                    ["Target"] = "Arch",
                    ["RequestID"] = null,
                    ["MessageBody"] = new Dictionary<string, dynamic?>
                    {
                        ["$type"] = "CFX.ResourcePerformance.FaultCleared, CFX",
                        ["FaultOccurrenceId"] = guid,
                        ["Operator"] = new Dictionary<string, string>
                        {
                            ["OperatorIdentifier"] = "",
                            ["ActorType"] = "",
                            ["LastName"] = "",
                            ["FirstName"] = "",
                            ["LogingName"] = ""
                        },
                        ["Metadata"] = new Dictionary<string, string>
                        {
                            ["building"] = _machineMetadataOptions.Value.Building ?? "",
                            ["device"] = _machineMetadataOptions.Value.Device ?? "",
                            ["area_name"] = _machineMetadataOptions.Value.AreaName ?? "",
                            ["org"] = _machineMetadataOptions.Value.Organization ?? "",
                            ["line_name"] = _machineMetadataOptions.Value.LineName ?? "",
                            ["site_name"] = _machineMetadataOptions.Value.SiteName ?? "",
                            ["station_name"] = _machineMetadataOptions.Value.StationName ?? "",
                            ["Process_type"] = _machineMetadataOptions.Value.ProcessType ?? "",
                            ["machine_name"] = _machineMetadataOptions.Value.MachineName ?? "",
                            ["Created_by"] = _machineMetadataOptions.Value.CreatedBy ?? "",
                        }
                    }
                };

                var faultcleared_exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
                var faultcleared_routingKey = $"{_rabbitMQPublisherOptions.Value.Prefix}.faultcleared.{ROUTINGKEY_SUFFIX}";
                var faultcleared_queueName = $"{_rabbitMQPublisherOptions.Value.Prefix}.faultcleared.{QUEUE_SUFFIX}";
                var faultcleared_message = JsonSerializer.Serialize(faultClearedJson, options);

                await _rabbitMQService.PublishMessageAsync(faultcleared_exchangeName, faultcleared_routingKey, faultcleared_message);

                // StationStateChanged
                var oldState = list[list.Count - 2].Status.HasValue ? StatusEventType.GetCfxCode((MAPBasicStatusCode)list[list.Count - 2].Status.Value) : -1; ;
                var newState = list.Last().Status.HasValue ? StatusEventType.GetCfxCode((MAPBasicStatusCode)list.Last().Status.Value) : -1;
                var oldStateDuration = "";
                var lastOPTime = list.Last().OPTime;
                var secondToLastOPTime = list[list.Count - 2].OPTime;

                if (!string.IsNullOrWhiteSpace(lastOPTime) && !string.IsNullOrWhiteSpace(secondToLastOPTime))
                {
                    oldStateDuration = DateTimeExtensions.CalculateTimeDifference(secondToLastOPTime, lastOPTime);
                }

                var stationstatechanged_json = new Dictionary<string, dynamic?>
                {
                    ["MessageName"] = _machineStatusOptions.Value.StationStateChanged,
                    ["Version"] = _machineStatusOptions.Value.Version,
                    ["TimeStamp"] = Convert.ToDateTime(lastOPTime).FormatDateTimeToIso8601(8),
                    ["UniqueID"] = uniqueId,
                    ["Source"] = uniqueId,
                    ["Target"] = "ARCH",
                    ["RequestID"] = null,
                    ["MessageBody"] = new Dictionary<string, dynamic?>
                    {
                        ["$type"] = "CFX.ResourcePerformance.StationStateChanged, CFX",
                        ["OldState"] = oldState,
                        ["OldStateDuration"] = oldStateDuration,
                        ["NewState"] = newState,
                        ["RelatedFault"] = null,
                        ["Metadata"] = new Dictionary<string, string>
                        {
                            ["building"] = _machineMetadataOptions.Value.Building ?? "",
                            ["device"] = _machineMetadataOptions.Value.Device ?? "",
                            ["area_name"] = _machineMetadataOptions.Value.AreaName ?? "",
                            ["org"] = _machineMetadataOptions.Value.Organization ?? "",
                            ["line_name"] = _machineMetadataOptions.Value.LineName ?? "",
                            ["site_name"] = _machineMetadataOptions.Value.SiteName ?? "",
                            ["station_name"] = _machineMetadataOptions.Value.StationName ?? "",
                            ["Process_type"] = _machineMetadataOptions.Value.ProcessType ?? "",
                            ["machine_name"] = _machineMetadataOptions.Value.MachineName ?? "",
                            ["Created_by"] = _machineMetadataOptions.Value.CreatedBy ?? "",
                        }
                    }
                };

                var stationstatechanged_exchangeName = $"{_rabbitMQPublisherOptions.Value.Prefix}.{EXCHANGE_SUFFIX}";
                var stationstatechanged_routingKey = $"{_rabbitMQPublisherOptions.Value.Prefix}.stationstatechanged.{ROUTINGKEY_SUFFIX}";
                var stationstatechanged_queueName = $"{_rabbitMQPublisherOptions.Value.Prefix}.stationstatechanged.{QUEUE_SUFFIX}";
                var stationstatechanged_message = JsonSerializer.Serialize(stationstatechanged_json, options);

                await _rabbitMQService.PublishMessageAsync(stationstatechanged_exchangeName, stationstatechanged_routingKey, stationstatechanged_message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing MachineState messages.");
                return;
            }
            finally
            {
                File.Delete(filePath);
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        private bool IsValidDateTime(string dateTimeString)
        {
            // 尝试解析日期时间字符串
            // 支持多种格式，包括 "yyyy/M/d H:mm", "yyyy/M/d H:m", "yyyy/M/d H:mm:ss" 等
            string[] formats = {
            "yyyy/M/d H:mm",
            "yyyy/M/d H:m",
            "yyyy/M/d H:mm:ss",
            "yyyy/M/d H:m:s",
            "yyyy/MM/dd HH:mm",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/M/dd H:mm",
            "yyyy/M/dd H:mm:ss"
        };

            DateTime result;
            return DateTime.TryParseExact(dateTimeString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
    }
}
