using LanguagePractice.Helpers;
using System;
using System.Text;

namespace LanguagePractice.Services
{
    public class PromptBuilder
    {
        // ==========================================
        // 共通ヘッダ
        // ==========================================
        private static string GetCommonHeader()
        {
            return @"
【絶対禁止（出力）】
- Web検索結果の表示、URL/リンク/出典の列挙は禁止。
- Markdownは禁止。
- 前置き、断り書き、メタ発言（「〜します」「AIとして」等）は禁止。
- マーカー外への出力は禁止（成果物は必ず指定マーカー内のみ）。

【共通原則】
- 指示された出力フォーマット（キー名、順序、区切り）を厳守する。
- 不明な場合は推測で断定せず、指定フォーマットの中で「UNCLEAR」等の許容表現を用いる。
- 出力の最後に必ず次の終端文字列を、そのまま出力する：⟦LP_DONE_9F3A2C⟧
";
        }

        private static string GetLengthInstruction(LengthProfile profile, out int min, out int max)
        {
            switch (profile)
            {
                case LengthProfile.STUDY_SHORT: min = 90; max = 200; break;
                case LengthProfile.REVISION_LONG: min = 450; max = 800; break;
                case LengthProfile.PRACTICE_MIDDLE:
                default: min = 250; max = 450; break;
            }
            return $@"
【文字量（改行除く文字数）】
- 本文は指定レンジに収めること（±10〜15%の軽微な誤差は許容）。
- 文字数調整のために内容の核を薄めないこと。
RANGE: {min}〜{max}字
";
        }

        // ==========================================
        // K-1. READER_AUTO_GEN
        // ==========================================
        public string BuildReaderAutoPrompt(string contextKind)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.READER_AUTO_GEN];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.READER_AUTO_GEN];

            sb.AppendLine($@"
あなたは「読者像（READER）を1つだけ決める担当」です。

【入力】
- 作品種別：{contextKind}

【生成ルール】
- 読者像は必ず1つだけ。
- 1〜2文で、人物像＋読む状況（時間・気分）＋読書体力（平易/普通/文学寄りのいずれかのニュアンス）を含める。
- 汎用的すぎる「一般の読者」だけは避け、状況を具体化する。

【出力フォーマット（厳守）】
{markerBegin}
READER：<1〜2文。1つだけ>
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-2. TOPIC_GEN
        // ==========================================
        public string BuildTopicGenPrompt(string imageUrl)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.TOPIC_GEN];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.TOPIC_GEN];

            string imageRef = string.IsNullOrWhiteSpace(imageUrl)
                ? "【画像参照】なし（AIの独創性にお任せ。季節感のある具体的な情景を想定）"
                : $"【画像参照（最優先）】\n次のURL先の画像を見て、雰囲気、時間帯、場所、モチーフを反映したお題を生成すること。\nURL: {imageUrl}";

            sb.AppendLine($@"
あなたは「詳細お題ジェネレーター」です。以下の仕様で詳細お題を生成してください。

{imageRef}

【目的】
- 作品生成・学習に使うため、余白が入りすぎないよう物理条件（FIX）を固定する。

【出力件数】
- 可能なら3〜5件（ただしフォーマット崩れを起こさないことを最優先）

【出力フォーマット（厳守）】
{markerBegin}
@@@TOPIC|1@@@
TITLE：<1行。余白のない具体。抽象語禁止>
EMOTION：<1〜3個>
SCENE：<STATIC/DYNAMIC/MIX>
TAGS：<カンマ区切りで3〜8個>
FIX：
- PLACE：<具体地>
- TIME：<季節＋時刻>
- WEATHER：<雨/風/気温など固定>
- LIGHT：<光源/強さ>
- SOUND：<音>
- OBJECTS：<登場物>
@@@TOPIC_END@@@
（以下繰り返し）
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-3. PERSONA_GEN
        // ==========================================
        public string BuildPersonaGenPrompt(string genre)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.PERSONA_GEN];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.PERSONA_GEN];
            string genreInstruction = string.IsNullOrWhiteSpace(genre) ? "指定なし（適度に分散）" : genre;

            sb.AppendLine($@"
あなたは文学・人物データベースの構築者です。指定された分野の人物について詳細情報を生成してください。

【指定分野】
{genreInstruction}

【生成ルール】
- 実在人物（日本・海外を含む）を扱うこと。
- ただし、確実でない事実を断定しない（曖昧ならその旨をBIOに含める）。
- 人数は可能なら5名。

【出力フォーマット（厳守：マーカー内のみ）】
{markerBegin}
@@@PERSONA|1@@@
NAME：<実名（よみ/通称があれば併記）>
LOCATION：<国籍、または主な活動場所/ゆかりの地>
BIO：<経歴・人物像・エピソードを100〜200字目安>
STYLE：<文体・特徴・癖・好むモチーフなどを具体的に>
TAGS：<カンマ区切り。検索用。3〜8個>
@@@PERSONA_END@@@
（以下繰り返し）
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-4. OBSERVE_IMAGE
        // ==========================================
        public string BuildObserveImagePrompt(string imageUrl)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.OBSERVE_IMAGE];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.OBSERVE_IMAGE];

            sb.AppendLine($@"
あなたは「観察ノート生成者」です。画像から読み取れる情報をもとに、五感描写・比喩・心象変換の素材を作成してください。

【画像参照（最優先）】
URL: {imageUrl}
※出力にURLは書かない。

【目的】
- 五感描写・比喩の素材集め（観察ノート中心）。
- 最後に「核の一文候補」を3つ出す。

【出力フォーマット（厳守）】
{markerBegin}
IMAGE_MOTIF：<見えるモチーフを5〜12語>
VISUAL：<視覚の箇条書き 5〜10>
SOUND：<音の仮説 2〜5>
SMELL_AIR：<匂い/空気感の仮説 2〜5>
TOUCH_TEMP：<触覚/温度の仮説 2〜5>
WEIGHT_BODY：<重さ/身体感覚の仮説 1〜3>

METAPHORS：
- <比喩1（感覚→比喩）>
- <比喩2>
- <比喩3>
- <比喩4>
- <比喩5>

TRANSFORM：
- <比喩→感情/状況へ変容1>
- <変容2>
- <変容3>

CORE_SENTENCE_CANDIDATES：
- <核の一文候補1>
- <候補2>
- <候補3>

TAGS：<カンマ区切りで3〜8個>
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-5. TEXT_GEN（★TOPIC空白時：AIが自由に自作＋SEEDで多様化）
        // ==========================================
        public string BuildTextGenPrompt(string writer, string topic, string readerResolved, string tone, LengthProfile lengthProfile)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            sb.AppendLine(GetLengthInstruction(lengthProfile, out int min, out int max));

            if (!string.IsNullOrWhiteSpace(readerResolved))
            {
                sb.AppendLine("【読者像（READER）】\n- 指定された読者像に合わせて、語彙の難度、比喩の密度、説明量、余韻の長さを調整する。\n- 読者像を勝手に変更しない（READERは入力を尊重）。");
            }

            string markerBegin = LpConstants.MarkerBegin[OperationKind.TEXT_GEN];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.TEXT_GEN];
            string toneInstruction = string.IsNullOrWhiteSpace(tone) ? "指定なし（通常の現代日本語）" : tone;
            string writerInstruction = string.IsNullOrWhiteSpace(writer) ? "AIにお任せ" : writer;

            bool isTopicEmpty = string.IsNullOrWhiteSpace(topic);

            // 毎回変わるseed（プロンプトの中に残るので、AIはこれを乱数源にできる）
            string seed = Guid.NewGuid().ToString("N")[..10];

            sb.AppendLine($@"
あなたは文章生成者です。指定された読者像に合わせて、作品（短文〜長文）を生成してください。

【入力】
WRITER：{writerInstruction}
TOPIC：{(isTopicEmpty ? "(AUTO_CREATE)" : topic)}
READER：{readerResolved}
TONE：{toneInstruction}
LENGTH_PROFILE：{lengthProfile}
LENGTH_RANGE：{min}〜{max}字（改行除く）
");

            if (isTopicEmpty)
            {
                sb.AppendLine($@"
【TOPICが空のとき（重要）】
- TOPICはAIが「新規に自作」する（こちらから候補は与えない）。
- ただし毎回似た発想に寄らないように、下のSEEDを乱数種として使い、発想を意図的に散らす。
- 手順（出力には書かないでよい）：
  1) SEEDからランダム性を作り、まずTOPIC候補を最低3案、互いに違う方向性で作る（場所/時間/出来事/小物/感情を変える）。
  2) その中で「最も具体的で、最も新鮮」な1案を選び、TOPICを1行で確定する。
  3) 本文は確定したTOPICに一致させる。
SEED：{seed}

【TOPICの要件】
- 1行で具体的（抽象語だけで終わらない）。
- 場所・時間帯・小さな具体物（小物）・出来事（動き）・感情のどれか2つ以上が想像できる内容にする。
- 既視感のある定番の方向性に寄りすぎないようにする（多様性を優先）。
");
            }

            sb.AppendLine($@"
【ルール】
- 読者像（READER）に合わせて、語彙難度・比喩密度・説明量・余韻の長さを調整する。
- TOPICやWRITERが空欄の場合は上記ルールに従う（TOPICは必ず確定してから書く）。
- TONEが「なし」なら通常の現代日本語。指定があれば“軽く”寄せる（厳密な文体変換はしない）。
- 出力はマーカー内のみ。

【出力フォーマット（厳守）】
{markerBegin}
WRITER：<実際に用いた書き手（指定なしなら「指定なし」）>
READER：{readerResolved}
TOPIC：<実際に用いたお題（空欄時は自作した1行）>
LENGTH_PROFILE：{lengthProfile}
TEXT：
<本文（{min}〜{max}字 目安。改行可）>

TECHNIQUE_MEMO：
- <五感>
- <比喩>
- <音調>
- <省略/余韻>
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-6. STUDY_CARD
        // ==========================================
        public string BuildStudyCardPrompt(string readerResolved, string tone, string sourceText)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.STUDY_CARD];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.STUDY_CARD];
            string toneInstruction = string.IsNullOrWhiteSpace(tone) ? "なし" : tone;

            sb.AppendLine($@"
あなたは「学習カード作成者」です。入力本文を分析し、学習可能なカードに分解してください。

【入力】
READER：{readerResolved}
TONE：{toneInstruction}
SOURCE_TEXT：
{sourceText}

【ルール】
- BEST_EXPRESSIONSは原文からの引用（改変禁止）。
- METAPHOR_CHAINSは「感覚→比喩→感情/問い」の形で3本。
- DO_NEXTは必ず3つ、短く具体的に（実行可能な指示にする）。
- URL/Markdown/前置き禁止。

【出力フォーマット（厳守）】
{markerBegin}
META：
- READER：{readerResolved}
- TONE：{toneInstruction}
- FOCUS：<効きの要約（短く）>
- LEVEL：<EASY|NORMAL|LITERARY>

BEST_EXPRESSIONS：
- <一文1>
- <一文2>
- <一文3>

SENSORY_MAP：
- VISUAL：<3〜8個>
- SOUND：<0〜5個>
- SMELL_AIR：<0〜5個>
- TOUCH_TEMP：<0〜5個>
- BODY_WEIGHT：<0〜5個>

METAPHOR_CHAINS：
- CHAIN1：<感覚語> → <比喩> → <感情/問い>
- CHAIN2：...
- CHAIN3：...

RHYTHM_NOTES：
- <音調所見1>
- <音調所見2>
- <音調所見3>

STRUCTURE_NOTES：
- CORE_GUESS：<主題/感情/問い/読者像の推定を1〜2文>
- OPENING_MOVE：<導入の型>
- ENDING_MOVE：<終わり方の型>

DO_NEXT：
- TASK1：<90〜150字など具体条件を含める>
- TASK2：<差し替え/書き換えなど>
- TASK3：<100→50→25など>

AVOID：
- <悪手1>
- <悪手2>

TAGS：<カンマ区切り3〜10個>
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-7. CORE_EXTRACT
        // ==========================================
        public string BuildCoreExtractPrompt(string readerResolved, string sourceText)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.CORE_EXTRACT];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.CORE_EXTRACT];

            sb.AppendLine($@"
あなたは「文章の核（Core）を抽出する担当」です。本文から核を明確化してください。

【入力】
READER（固定）：{readerResolved}
TEXT：
{sourceText}

【ルール】
- READERは入力のまま維持し、勝手に変更しない。
- CORE_SENTENCEは1文でまとめる。
- 不明な点があればQUESTIONSに1〜3個の確認質問を出す。

【出力フォーマット（厳守）】
{markerBegin}
THEME：...
EMOTION：...
TAKEAWAY：...
READER：{readerResolved}
CORE_SENTENCE：...
MUST_KEEP：...（任意）
CAN_CUT：...（任意）
QUESTIONS：...（任意：必要な場合のみ）
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-8. REVISION_FULL
        // ==========================================
        public string BuildRevisionFullPrompt(string sourceText, string coreTheme, string coreEmotion, string coreTakeaway, string coreReader, string coreSentence)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.REVISION_FULL];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.REVISION_FULL];

            sb.AppendLine($@"
あなたは「推敲者」です。核（Core）を不変条件として維持し、本文を3案の方針で全文改稿してください。

【不変条件（絶対に変えない）】
THEME：{coreTheme}
EMOTION：{coreEmotion}
TAKEAWAY：{coreTakeaway}
READER：{coreReader}
CORE_SENTENCE：{coreSentence}

【対象本文】
{sourceText}

【3案の方針】
- A：凝縮。核が最短で刺さる構成。
- B：五感・比喩・音調を増幅（描写の密度と響き）。
- C：余韻・問いの残し方を設計（終わりの効果）。

【ルール】
- 各案で「GLOBAL_CHANGES（CUT/MOVE/ADD）」を必ず書く。
- 各案で本文（REVISED_TEXT）を必ず全文出す。
- URL/Markdown/前置き禁止。

【出力フォーマット（厳守）】
{markerBegin}

@@@REVISION|A@@@
INTENT：凝縮。核が最短で刺さる構成。
GLOBAL_CHANGES：
- CUT：...
- MOVE：...
- ADD：...
LOCAL_CHANGES：
- ...
REVISED_TEXT：
...

@@@REVISION|B@@@
INTENT：五感・比喩・音調を増幅（描写の密度と響き）。
GLOBAL_CHANGES：
- CUT：...
- MOVE：...
- ADD：...
LOCAL_CHANGES：
- ...
REVISED_TEXT：
...

@@@REVISION|C@@@
INTENT：余韻・問いの残し方を設計（終わりの効果）。
GLOBAL_CHANGES：
- CUT：...
- MOVE：...
- ADD：...
LOCAL_CHANGES：
- ...
REVISED_TEXT：
...

{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-9. GIKO
        // ==========================================
        public string BuildGikoPrompt(string toneLabel, string toneRuleText, string readerOrEmpty, string topicLineOrEmpty, string inputBody)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.GIKO];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.GIKO];

            sb.AppendLine($@"
あなたは古典文体の専門家です。入力本文の意味や情景を変えず、指定文調に書き換えてください。

【入力】
TONE（固定）：{toneLabel}
READER（任意）：{readerOrEmpty}
お題（任意）：{topicLineOrEmpty}

【入力本文（現代文）】
{inputBody}

【文調ルール（必ず適用）】
{toneRuleText}

【共通ルール】
- 意味や情景を変えず、文体だけを変える。
- ルビは難読語のみ最小限（歴史的仮名遣いを意識）。
- 最後に語彙変換表（現代語/擬古文/理由）を付ける。

【出力形式（厳守）】
{markerBegin}
TONE：{toneLabel}
READER：{readerOrEmpty}
GIKO_TEXT：
...

VOCAB_TABLE：
| 現代語 | 擬古文 | 理由 |
|---|---|---|
...
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }

        // ==========================================
        // K-10. PERSONA_VERIFY_ASSIST
        // ==========================================
        public string BuildPersonaVerifyPrompt(string personaName, string personaBio, string e1, string e2, string e3)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetCommonHeader());
            string markerBegin = LpConstants.MarkerBegin[OperationKind.PERSONA_VERIFY_ASSIST];
            string markerEnd = LpConstants.MarkerEnd[OperationKind.PERSONA_VERIFY_ASSIST];

            sb.AppendLine($@"
あなたは「人物プロフィールの検証支援者」です。Web検索は行わず、貼付された根拠テキスト（E1〜E3）の範囲でのみ判定してください。

【対象Persona（現在の記述）】
NAME：{personaName}
BIO：{personaBio}

【根拠テキスト（Evidence）】
E1：
{e1}

E2：
{e2}

E3：
{e3}

【絶対ルール】
- 根拠テキスト（E1〜E3）に書かれていないことは断定しない。
- 判定は次の4値のみ：SUPPORTED / NOT_SUPPORTED / CONTRADICTED / UNCLEAR
- 出力にURL/リンク/出典列挙は書かない。

【出力フォーマット（厳守）】
{markerBegin}
TARGET：
NAME：{personaName}

CLAIMS：
@@@CLAIM|1@@@
FIELD：<BIO|STYLE|LOCATION|DATES|WORKS|TAGS|OTHER>
CLAIM：<検証対象の主張を1文>
VERDICT：SUPPORTED|NOT_SUPPORTED|CONTRADICTED|UNCLEAR
EVIDENCE_QUOTE：<短い抜粋（該当なしなら（該当なし））>
REASON：<1〜2文。根拠外断定禁止>
SUGGESTED_EDIT：<どう直すべきか短く>
@@@CLAIM_END@@@
（以下繰り返し）

REVISED_PERSONA_DRAFT：
BIO：<根拠に基づき必要なら修正。根拠がなければ現状維持/保留>

STATUS_RECOMMENDATION：
SUGGESTED_STATUS：UNVERIFIED|PARTIALLY_VERIFIED|VERIFIED|DISPUTED
WHY：<2〜3行>
{markerEnd}

（最後に必ず {LpConstants.DONE_SENTINEL} を出力）
");
            return sb.ToString();
        }
    }
}
