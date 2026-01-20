using LanguagePractice.Helpers;
using LanguagePractice.Models;
using System.Collections.Generic;
using System.Linq;

namespace LanguagePractice.Services
{
    public class RouteService
    {
        // プリセットルートのリスト
        public List<RouteDefinition> GetPresets()
        {
            return new List<RouteDefinition>
            {
                // E-1. R01：例文→学習カード（短文）
                new RouteDefinition
                {
                    Id = "R01",
                    Title = "R01: 例文学習 (Study Basic)",
                    Description = "AIに短い例文を作らせ、即座に学習カードに分解します。",
                    Steps = new List<RouteStep>
                    {
                        // Step 1: TEXT_GEN
                        new RouteStep
                        {
                            StepNumber = 1,
                            Title = "1. 例文生成",
                            Operation = OperationKind.TEXT_GEN,
                            FixedLength = LengthProfile.STUDY_SHORT,
                            InputBindings = new Dictionary<string, string>
                            {
                                // 入力は手動(MANUAL)またはお任せ
                            }
                        },
                        // Step 2: STUDY_CARD
                        new RouteStep
                        {
                            StepNumber = 2,
                            Title = "2. カード化",
                            Operation = OperationKind.STUDY_CARD,
                            InputBindings = new Dictionary<string, string>
                            {
                                // Step 1の "TEXT" 出力を "SOURCE_TEXT" 入力に渡す
                                { "SOURCE_TEXT", "PREV_OUTPUT:TEXT" },
                                // Step 1の "READER" 出力を "READER" 入力に渡す
                                { "READER", "PREV_OUTPUT:READER" }
                            }
                        }
                    }
                },

                // E-3. R03：画像→例文→学習カード
                new RouteDefinition
                {
                    Id = "R03",
                    Title = "R03: 画像描写 (Image to Study)",
                    Description = "画像URLから観察ノートを作り、それを元に例文→カード化します。",
                    Steps = new List<RouteStep>
                    {
                        // Step 1: OBSERVE_IMAGE (※OBSERVEの実装はまだですが定義だけ先に)
                        new RouteStep
                        {
                            StepNumber = 1,
                            Title = "1. 画像観察",
                            Operation = OperationKind.OBSERVE_IMAGE,
                            InputBindings = new Dictionary<string, string> { { "IMAGE_URL", "MANUAL" } }
                        },
                        // Step 2: TEXT_GEN
                        new RouteStep
                        {
                            StepNumber = 2,
                            Title = "2. 例文生成",
                            Operation = OperationKind.TEXT_GEN,
                            FixedLength = LengthProfile.STUDY_SHORT,
                            InputBindings = new Dictionary<string, string>
                            {
                                // 観察結果のトピック等を引き継ぐ設定（今回は簡易）
                                { "TOPIC", "PREV_OUTPUT:IMAGE_MOTIF" }
                            }
                        },
                        // Step 3: STUDY_CARD
                        new RouteStep
                        {
                            StepNumber = 3,
                            Title = "3. カード化",
                            Operation = OperationKind.STUDY_CARD,
                            InputBindings = new Dictionary<string, string>
                            {
                                { "SOURCE_TEXT", "PREV_OUTPUT:TEXT" }
                            }
                        }
                    }
                }
            };
        }

        public RouteDefinition? GetRouteById(string id)
        {
            return GetPresets().FirstOrDefault(r => r.Id == id);
        }
    }
}
