using Dapper;
using LanguagePractice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Windows;

namespace LanguagePractice.Services
{
    // DTO
    public class RouteStepDto
    {
        [JsonPropertyName("StepNumber")]
        public int StepNumber { get; set; }

        [JsonPropertyName("Title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("Operation")]
        public string Operation { get; set; } = "";

        [JsonPropertyName("InputBindings")]
        public Dictionary<string, string> InputBindings { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("FixedTone")]
        public string? FixedTone { get; set; }

        [JsonPropertyName("FixedLengthVal")]
        public int? FixedLengthVal { get; set; }
    }

    public class CustomRouteEntity
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string StepsJson { get; set; } = ""; // DBカラム: steps_json
        public string UpdatedAt { get; set; } = ""; // DBカラム: updated_at
    }

    public class CustomRouteService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public CustomRouteService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public List<RouteDefinition> GetAllRoutes()
        {
            var routes = new List<RouteDefinition>();
            try
            {
                using var conn = DatabaseService.GetConnection();

                // 【重要】ASを使ってプロパティ名とカラム名を強制一致させる
                string sql = @"
                    SELECT 
                        id AS Id, 
                        title AS Title, 
                        description AS Description, 
                        steps_json AS StepsJson, 
                        updated_at AS UpdatedAt 
                    FROM custom_route 
                    ORDER BY updated_at DESC";

                var entities = conn.Query<CustomRouteEntity>(sql).ToList();

                foreach (var entity in entities)
                {
                    // 空チェック
                    if (string.IsNullOrWhiteSpace(entity.StepsJson) || entity.StepsJson.Trim() == "[]")
                    {
                        routes.Add(new RouteDefinition
                        {
                            Id = entity.Id,
                            Title = entity.Title + " (DB空)",
                            Description = entity.Description,
                            Steps = new List<RouteStep>()
                        });
                        continue;
                    }

                    try
                    {
                        var dtos = JsonSerializer.Deserialize<List<RouteStepDto>>(entity.StepsJson, _jsonOptions);

                        var modelSteps = new List<RouteStep>();
                        if (dtos != null)
                        {
                            foreach (var dto in dtos)
                            {
                                Enum.TryParse<Helpers.OperationKind>(dto.Operation, out var op);
                                Helpers.LengthProfile? len = null;
                                if (dto.FixedLengthVal.HasValue) len = (Helpers.LengthProfile)dto.FixedLengthVal.Value;

                                modelSteps.Add(new RouteStep
                                {
                                    StepNumber = dto.StepNumber,
                                    Title = dto.Title,
                                    Operation = op,
                                    InputBindings = dto.InputBindings ?? new Dictionary<string, string>(),
                                    FixedTone = dto.FixedTone,
                                    FixedLength = len
                                });
                            }
                        }

                        routes.Add(new RouteDefinition
                        {
                            Id = entity.Id,
                            Title = entity.Title,
                            Description = entity.Description,
                            Steps = modelSteps
                        });
                    }
                    catch (Exception ex)
                    {
                        routes.Add(new RouteDefinition
                        {
                            Id = entity.Id,
                            Title = entity.Title + " [読込エラー]",
                            Description = ex.Message,
                            Steps = new List<RouteStep>()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Error: {ex.Message}");
            }
            return routes;
        }

        public void SaveRoute(RouteDefinition route)
        {
            try
            {
                if (string.IsNullOrEmpty(route.Id)) route.Id = Guid.NewGuid().ToString();

                var dtos = new List<RouteStepDto>();
                foreach (var s in route.Steps)
                {
                    dtos.Add(new RouteStepDto
                    {
                        StepNumber = s.StepNumber,
                        Title = s.Title,
                        Operation = s.Operation.ToString(),
                        InputBindings = s.InputBindings,
                        FixedTone = s.FixedTone,
                        FixedLengthVal = s.FixedLength.HasValue ? (int)s.FixedLength.Value : null
                    });
                }

                string json = JsonSerializer.Serialize(dtos, _jsonOptions);

                var entity = new CustomRouteEntity
                {
                    Id = route.Id,
                    Title = route.Title,
                    Description = route.Description,
                    StepsJson = json,
                    UpdatedAt = DateTime.Now.ToString("o")
                };

                using var conn = DatabaseService.GetConnection();
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS custom_route (
                        id TEXT PRIMARY KEY,
                        title TEXT NOT NULL,
                        description TEXT,
                        steps_json TEXT NOT NULL,
                        updated_at TEXT
                    );
                ");

                // 【重要】パラメータ名をプロパティ名に合わせる
                string sql = @"
                    INSERT INTO custom_route (id, title, description, steps_json, updated_at)
                    VALUES (@Id, @Title, @Description, @StepsJson, @UpdatedAt)
                    ON CONFLICT(id) DO UPDATE SET
                        title = excluded.title,
                        description = excluded.description,
                        steps_json = excluded.steps_json,
                        updated_at = excluded.updated_at;";

                conn.Execute(sql, entity);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存処理失敗: {ex.Message}", ex);
            }
        }

        public void DeleteRoute(string id)
        {
            using var conn = DatabaseService.GetConnection();
            conn.Execute("DELETE FROM custom_route WHERE id = @Id", new { Id = id });
        }
    }
}
