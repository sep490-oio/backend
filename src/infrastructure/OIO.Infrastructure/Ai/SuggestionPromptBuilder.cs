using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.Services.AiSuggestion;

namespace OIO.Infrastructure.Ai;

internal static class SuggestionPromptBuilder
{
    private const string SystemPromptVietnamese = """
        Ban la tro ly viet mo ta san pham cho mot nen tang dau gia tai Viet Nam.
        Tra ve DUY NHAT mot doi tuong JSON khop voi schema sau, khong them markdown, khong them giai thich:
        {
          "descriptionHtml": "HTML fragment (60-600 ky tu noi dung), CHI dung cac the: <p>, <br>, <strong>, <em>, <ul>, <ol>, <li>",
          "suggestedCategoryId": "guid hoac null",
          "suggestedCategoryConfidence": 0.0,
          "alternatives": [{ "id": "guid", "confidence": 0.0 }],
          "warnings": ["string codes from a fixed set"]
        }
        Quy tac noi dung HTML:
        - Cau truc goi y: 1 doan mo dau ngan <p>...</p>, 1 danh sach dac diem <ul><li>...</li></ul>, 1 doan tinh trang/ghi chu trung lap <p>...</p> (chi neu co du du lieu tu anh/title/condition).
        - CHI duoc dung the: <p>, <br>, <strong>, <em>, <ul>, <ol>, <li>. Khong duoc co attribute (khong style, class, id, href, src, on*).
        - CAM tuyet doi: <script>, <style>, <iframe>, <img>, <a>, <form>, <input>, markdown (** __ # > -), emoji, URL, email, so dien thoai, hashtag, loi keu goi hanh dong (CTA).
        - Khong khang dinh xuat xu, hang chinh hang, giay chung nhan, so seri, gia tri, gia ban, chat lieu hay tinh trang kiem dinh neu khong co bang chung tu input (anh/title/condition).
        - Locale "vi": viet bang tieng Viet. Locale "en": viet bang tieng Anh. Khong dung emoji, khong viet hoa toan bo dong.
        - suggestedCategoryId BUOC PHAI nam trong danh sach activeCategories duoc cap. Neu khong co lua chon phu hop, dat suggestedCategoryId = null va them warning "category.unmatched".
        - Tra ve confidence trong khoang [0.0, 1.0].
        """;

    private const string SystemPromptEnglish = """
        You are an assistant that drafts product descriptions for an online auction marketplace.
        Return ONLY a JSON object matching this schema, no markdown, no narration:
        {
          "descriptionHtml": "HTML fragment (60-600 chars of visible text). Use ONLY tags: <p>, <br>, <strong>, <em>, <ul>, <ol>, <li>.",
          "suggestedCategoryId": "guid or null",
          "suggestedCategoryConfidence": 0.0,
          "alternatives": [{ "id": "guid", "confidence": 0.0 }],
          "warnings": ["string codes from a fixed set"]
        }
        HTML rules:
        - Suggested shape: short opening <p>...</p>, a feature list <ul><li>...</li></ul>, optional closing <p>...</p> for condition note (only if input gives enough evidence).
        - ONLY allowed tags: <p>, <br>, <strong>, <em>, <ul>, <ol>, <li>. NO attributes (no style, class, id, href, src, on*).
        - STRICTLY forbidden: <script>, <style>, <iframe>, <img>, <a>, <form>, <input>, markdown (** __ # > -), emoji, URLs, emails, phone numbers, hashtags, calls to action.
        - Do NOT claim provenance, authenticity, certification, serial numbers, valuation, pricing, materials, or inspection status without evidence from input (image/title/condition).
        - If locale="vi" write Vietnamese; if "en" write English. No emoji, no all-caps lines.
        - suggestedCategoryId MUST be one of the supplied activeCategories. If none fit, set suggestedCategoryId = null and add warning "category.unmatched".
        - Confidence values must be in [0.0, 1.0].
        """;

    public static IList<ChatMessage> Build(SuggestItemDescriptionInput input, AiOptions opts)
    {
        var locale = string.Equals(input.Locale, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";
        var systemPrompt = locale == "en" ? SystemPromptEnglish : SystemPromptVietnamese;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
        };

        var userText = BuildUserText(input, opts, locale);

        var userContents = new List<AIContent>
        {
            new TextContent(userText),
        };

        foreach (var image in input.Images)
        {
            // Pass remote image URLs as UriContent so providers that support
            // multimodal URLs (Google Gemini does) can fetch them directly.
            // The actual URL string is never logged anywhere.
            if (Uri.TryCreate(image.SecureUrl, UriKind.Absolute, out var uri))
            {
                var mediaType = ResolveMediaType(image.Format);
                userContents.Add(new UriContent(uri, mediaType));
            }
        }

        messages.Add(new ChatMessage(ChatRole.User, userContents));
        return messages;
    }

    private static string BuildUserText(SuggestItemDescriptionInput input, AiOptions opts, string locale)
    {
        var sb = new StringBuilder();
        sb.Append("locale=").Append(locale).Append('\n');
        sb.Append("title=").Append(input.Title).Append('\n');
        sb.Append("condition=").Append(input.Condition).Append('\n');
        sb.Append("maxDescriptionChars=").Append(opts.MaxDescriptionChars).Append('\n');

        // Compact JSON for category list to keep the prompt short.
        var cats = input.ActiveCategories.Select(c => new { id = c.Id, name = c.Name, path = c.Path });
        sb.Append("activeCategories=");
        sb.Append(JsonSerializer.Serialize(cats));
        return sb.ToString();
    }

    private static string ResolveMediaType(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return "image/jpeg";
        }

        return format.Trim().ToLowerInvariant() switch
        {
            "jpg" => "image/jpeg",
            "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "gif" => "image/gif",
            _ => "image/jpeg",
        };
    }
}
