using LanguagePractice.Models;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LanguagePractice.Services
{
    /// <summary>
    /// PoetryLab用プロンプト生成
    /// </summary>
    public class PoetryPromptBuilder
    {
        private const string DONE_SENTINEL = "⟦LP_DONE_9F3A2C⟧";

        /// <summary>
        /// ステップに応じたプロンプトを生成
        /// </summary>
        public string Build(string stepName, Dictionary<string, string> inputs, string styleType)
        {
            return stepName switch
            {
                "POEM_TOPIC_GEN" => BuildTopicGenPrompt(styleType),
                "POEM_DRAFT_GEN" => BuildDraftGenPrompt(inputs, styleType),
                "POEM_CORE_EXTRACT" => BuildCoreExtractPrompt(inputs),
                "POEM_LINE_MAP" => BuildLineMapPrompt(inputs),
                "POEM_ISSUE_GEN" => BuildIssueGenPrompt(inputs),
                "POEM_DIAGNOSE_GEN" => BuildDiagnoseGenPrompt(inputs),
                "POEM_REVISION_GEN" => BuildRevisionGenPrompt(inputs, styleType),
                _ => string.Empty
            };
        }

        /// <summary>
        /// POEM_TOPIC_GEN: お題生成
        /// </summary>
        private string BuildTopicGenPrompt(string styleType)
        {
            var styleDesc = styleType switch
            {
                "KOU" => "口語自由詩",
                "BU" => "文語自由詩",
                "MIX" => "口語・文語混合",
                _ => "自由詩"
            };

            return $@"あなたは詩の創作指導者です。
{styleDesc}のためのお題を1つ生成してください。

【要件】
- 抽象的すぎず、具体的なシード（像、場面、感覚）を含む
- 解釈の余白を残す（一義的に決まらない）
- 季節・時間・場所などの手がかりがあると良い

【出力形式】
以下の形式で出力してください。

⟦BEGIN_SECTION:TOPIC⟧
THEME: （テーマを一言で）
CONSTRAINT: （制約や条件があれば）
SEED_IMAGE: （具体的なイメージ、像、場面）
⟦END_SECTION:TOPIC⟧

{DONE_SENTINEL}";
        }

        /// <summary>
        /// POEM_DRAFT_GEN: 初稿生成
        /// </summary>
        private string BuildDraftGenPrompt(Dictionary<string, string> inputs, string styleType)
        {
            var topic = inputs.GetValueOrDefault("TOPIC", "");
            var styleDesc = styleType switch
            {
                "KOU" => "口語（現代語）",
                "BU" => "文語（古語調）",
                "MIX" => "口語と文語の混合",
                _ => "自由"
            };

            return $@"あなたは詩人です。
以下のお題に基づいて、自由詩の初稿を生成してください。

【お題】
{topic}

【スタイル】
{styleDesc}

【要件】
- 10〜30行程度
- 行分け・連分けを意識する
- 完成度より「核となる感情・問い」を大切に

【出力形式】
以下の形式で出力してください。

⟦BEGIN_SECTION:DRAFT⟧
STYLE: {styleType}
BODY:
（ここに詩の本文）
⟦END_SECTION:DRAFT⟧

{DONE_SENTINEL}";
        }

        /// <summary>
        /// POEM_CORE_EXTRACT: 核抽出
        /// </summary>
        private string BuildCoreExtractPrompt(Dictionary<string, string> inputs)
        {
            var draft = inputs.GetValueOrDefault("DRAFT", "");

            return $@"あなたは詩の批評家です。
以下の詩から「核」を抽出し、3つの候補を提示してください。

【詩】
{draft}

【核の定義】
核は以下の3要素で構成されます：
1. 主題（SUBJECT）：何について書いているか
2. 中心感情・態度（EMOTION）：それにどう反応しているか（矛盾・複雑さを含むと強い）
3. 問い/変化（QUESTION）：読者に渡したい問いや余韻

【出力形式】
以下の形式で3つの候補を出力してください。

⟦BEGIN_SECTION:CORE_CANDIDATES⟧
CANDIDATE_1_SUBJECT: （主題）
CANDIDATE_1_EMOTION: （中心感情・態度）
CANDIDATE_1_QUESTION: （問い/変化）
ONELINE_1: （核を一文で要約）

CANDIDATE_2_SUBJECT: （主題）
CANDIDATE_2_EMOTION: （中心感情・態度）
CANDIDATE_2_QUESTION: （問い/変化）
ONELINE_2: （核を一文で要約）

CANDIDATE_3_SUBJECT: （主題）
CANDIDATE_3_EMOTION: （中心感情・態度）
CANDIDATE_3_QUESTION: （問い/変化）
ONELINE_3: （核を一文で要約）
⟦END_SECTION:CORE_CANDIDATES⟧

{DONE_SENTINEL}";
        }

        /// <summary>
        /// POEM_LINE_MAP: 行分析
        /// </summary>
        private string BuildLineMapPrompt(Dictionary<string, string> inputs)
        {
            var draft = inputs.GetValueOrDefault("DRAFT", "");
            var core = inputs.GetValueOrDefault("CORE", "");

            return $@"あなたは詩の構造分析者です。
以下の詩を行ごとに分析してください。

【詩】
{draft}

【確定した核】
{core}

【分析項目】
各行について以下を分析：
- ROLE: 役割（導入/展開/転回/結末/余韻/補助）
- IMAGE: 中心イメージ
- VOICE: 声（距離、語り口、人称）
- TURN: 転回点か（YES/NO + 説明）

【出力形式】
⟦BEGIN_SECTION:LINE_MAP⟧
LINE_1_TEXT: （行のテキスト）
LINE_1_ROLE: （役割）
LINE_1_IMAGE: （イメージ）
LINE_1_VOICE: （声）
LINE_1_TURN: （転回か）

LINE_2_TEXT: （行のテキスト）
LINE_2_ROLE: （役割）
LINE_2_IMAGE: （イメージ）
LINE_2_VOICE: （声）
LINE_2_TURN: （転回か）

（以下、全行について続ける）
⟦END_SECTION:LINE_MAP⟧

{DONE_SENTINEL}";
        }

        /// <summary>
        /// POEM_ISSUE_GEN: Issue検出
        /// </summary>
        private string BuildIssueGenPrompt(Dictionary<string, string> inputs)
        {
            var draft = inputs.GetValueOrDefault("DRAFT", "");
            var core = inputs.GetValueOrDefault("CORE", "");
            var lineMap = inputs.GetValueOrDefault("LINE_MAP", "");

            return $@"あなたは詩の推敲指導者です。
以下の詩の問題点（Issue）を検出してください。

【詩】
{draft}

【確定した核】
{core}

【行分析】
{lineMap}

【検出すべき問題のレベル】
- CORE: 核（主題/感情/問い）からの逸脱
- STRUCTURE: 構成・配置の問題
- VOICE: 声の不統一、距離感のブレ
- IMAGE: 像の散乱、支柱の不在
- READER_EFFECT: 読者効果、余韻の問題
- SOUND: 音調、リズムの問題（優先度低）
- SURFACE: 表記、誤字（優先度最低）

【重要度（Severity）】
- S: 致命的（核を壊している）
- A: 重大（構造・像・声に大きな問題）
- B: 改善推奨
- C: 細かい磨き

【出力形式】
⟦BEGIN_SECTION:ISSUES⟧
ISSUE_1_TARGET: （LINE/n または STANZA/n または ALL）
ISSUE_1_LEVEL: （CORE/STRUCTURE/VOICE/IMAGE/READER_EFFECT/SOUND/SURFACE）
ISSUE_1_SYMPTOM: （症状の説明）
ISSUE_1_SEVERITY: （S/A/B/C）
ISSUE_1_EVIDENCE: （根拠）

ISSUE_2_TARGET: ...
（以下、検出した問題の数だけ続ける。S/Aを優先的に）
⟦END_SECTION:ISSUES⟧

{DONE_SENTINEL}";
        }

        /// <summary>
        /// POEM_DIAGNOSE_GEN: 診断生成
        /// </summary>
        private string BuildDiagnoseGenPrompt(Dictionary<string, string> inputs)
        {
            var issues = inputs.GetValueOrDefault("ISSUES", "");

            return $@"あなたは詩の推敲指導者です。
以下のIssue（問題点）に対して、診断と修正方針を提示してください。

【検出されたIssue】
{issues}

【修正タイプ（FIX_TYPE）】
- CUT: 削除
- MOVE: 移動
- ADD: 追加
- REPLACE: 置換
- CONDENSE: 凝縮
- AMPLIFY: 増幅
- RESTRUCTURE: 構造変更

【出力形式】
⟦BEGIN_SECTION:DIAGNOSES⟧
DIAG_1_ISSUE_REF: ISSUE_1
DIAG_1_CAUSE: （原因の分析）
DIAG_1_FIX_TYPE: （修正タイプ）
DIAG_1_PLAN: （具体的な修正方針）

DIAG_2_ISSUE_REF: ISSUE_2
DIAG_2_CAUSE: （原因の分析）
DIAG_2_FIX_TYPE: （修正タイプ）
DIAG_2_PLAN: （具体的な修正方針）

（以下、各Issueについて続ける。S/Aを優先）
⟦END_SECTION:DIAGNOSES⟧

{DONE_SENTINEL}";
        }

        /// <summary>
        /// POEM_REVISION_GEN: 改稿案生成
        /// </summary>
        private string BuildRevisionGenPrompt(Dictionary<string, string> inputs, string styleType)
        {
            var draft = inputs.GetValueOrDefault("DRAFT", "");
            var core = inputs.GetValueOrDefault("CORE", "");
            var diagnoses = inputs.GetValueOrDefault("DIAGNOSES", "");

            return $@"あなたは詩人です。
以下の初稿を、診断に基づいて改稿してください。
3つの異なるアプローチで改稿案を生成してください。

【初稿】
{draft}

【核（不変条件）】
{core}
※核は変えてはいけません。核を維持しながら改稿してください。

【診断と修正方針】
{diagnoses}

【3つのアプローチ例】
- 凝縮型: 行数を減らし密度を上げる
- 余韻設計型: 結末を開き、余韻を残す
- 像統制型: 支柱となる像を強化し、散乱を抑える
（上記は例です。診断に応じて適切なアプローチを選んでください）

【出力形式】
⟦BEGIN_SECTION:REVISIONS⟧
REV_A_APPROACH: （アプローチの説明）
REV_A_BODY:
（改稿案Aの本文）

REV_B_APPROACH: （アプローチの説明）
REV_B_BODY:
（改稿案Bの本文）

REV_C_APPROACH: （アプローチの説明）
REV_C_BODY:
（改稿案Cの本文）
⟦END_SECTION:REVISIONS⟧

{DONE_SENTINEL}";
        }
    }
}
