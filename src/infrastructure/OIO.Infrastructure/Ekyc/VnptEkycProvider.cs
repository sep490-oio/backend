using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Ekyc;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Ekyc;

internal sealed class VnptEkycProvider : IEkycProvider
{
    private const string SuccessMessage = "IDG-00000000";

    private readonly HttpClient _httpClient;
    private readonly VnptEkycOptions _options;
    private readonly ILogger<VnptEkycProvider> _logger;

    public VnptEkycProvider(
        HttpClient httpClient,
        IOptions<VnptEkycOptions> options,
        ILogger<VnptEkycProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "VNPT_EKYC";

    public async Task<Result<EkycVerificationResult, Error>> VerifyIdentityAsync(
        EkycVerificationRequest request,
        CancellationToken ct = default)
    {
        var warnings = new List<string>();
        var rawResponses = new Dictionary<string, object>();
        var clientSession = $"SERVER_OIO_{DateTime.UtcNow.Ticks}";
        var token = Guid.NewGuid().ToString("N");

        try
        {
            // 1. Download images from our storage & upload to VNPT
            var frontHash = await UploadImageFromUrlAsync(request.IdFrontImageUrl, "id_front", ct);
            if (frontHash.IsFailure) return frontHash.Error;

            string? backHash = null;
            if (!string.IsNullOrWhiteSpace(request.IdBackImageUrl))
            {
                var backResult = await UploadImageFromUrlAsync(request.IdBackImageUrl, "id_back", ct);
                if (backResult.IsFailure) return backResult.Error;
                backHash = backResult.Value;
            }

            var selfieHash = await UploadImageFromUrlAsync(request.SelfieImageUrl, "selfie", ct);
            if (selfieHash.IsFailure) return selfieHash.Error;

            // 2. OCR - extract ID info
            var ocrResult = await OcrDocumentAsync(frontHash.Value, backHash, clientSession, token, ct);
            if (ocrResult.IsFailure) return ocrResult.Error;
            rawResponses["ocr"] = ocrResult.Value;

            var ocr = ocrResult.Value;
            if (ocr.Object?.Msg != "OK")
                warnings.Add($"OCR front not OK: {ocr.Object?.Msg}");
            if (backHash is not null && ocr.Object?.MsgBack != "OK")
                warnings.Add($"OCR back not OK: {ocr.Object?.MsgBack}");

            // 3. Card liveness - check real document
            var cardLivenessResult = await CheckCardLivenessAsync(frontHash.Value, clientSession, ct);
            if (cardLivenessResult.IsFailure) return cardLivenessResult.Error;
            rawResponses["card_liveness"] = cardLivenessResult.Value;

            var isCardLive = cardLivenessResult.Value.Object?.Liveness == "success";
            if (!isCardLive)
                warnings.Add($"Card liveness failed: {cardLivenessResult.Value.Object?.LivenessMsg}");

            // 4. Face compare - ID photo vs selfie
            var faceCompareResult = await CompareFacesAsync(frontHash.Value, selfieHash.Value, clientSession, token, ct);
            if (faceCompareResult.IsFailure) return faceCompareResult.Error;
            rawResponses["face_compare"] = faceCompareResult.Value;

            var faceMatchScore = (decimal)(faceCompareResult.Value.Object?.Prob ?? 0);
            var isFaceMatch = faceCompareResult.Value.Object?.Msg == "MATCH";

            // 5. Face liveness - check real person
            var faceLivenessResult = await CheckFaceLivenessAsync(selfieHash.Value, clientSession, token, ct);
            if (faceLivenessResult.IsFailure) return faceLivenessResult.Error;
            rawResponses["face_liveness"] = faceLivenessResult.Value;

            var isFaceLive = faceLivenessResult.Value.Object?.Liveness == "success";
            if (!isFaceLive)
                warnings.Add($"Face liveness failed: {faceLivenessResult.Value.Object?.LivenessMsg}");

            // Add OCR warnings
            if (ocr.Object?.Warning is { Count: > 0 })
                warnings.AddRange(ocr.Object.WarningMsg ?? ocr.Object.Warning);

            // Check ID fake
            var isIdFake = ocr.Object?.IdFakeWarning == "yes";
            if (isIdFake)
                warnings.Add($"ID fake warning (prob: {ocr.Object?.IdFakeProb:F4})");

            var isTampered = ocr.Object?.Tampering?.IsLegal != "yes";
            if (isTampered)
                warnings.Add("Document tampering detected");

            // Build OCR data
            var ocrData = new EkycOcrData
            {
                IdNumber = ocr.Object?.Id,
                FullName = ocr.Object?.Name,
                DateOfBirth = ocr.Object?.BirthDay,
                Gender = ocr.Object?.Gender,
                Nationality = ocr.Object?.Nationality,
                Ethnicity = ocr.Object?.Nation,
                Address = ocr.Object?.RecentLocation,
                Hometown = ocr.Object?.OriginLocation,
                IssueDate = ocr.Object?.IssueDate,
                IssuePlace = ocr.Object?.IssuePlace,
                ExpiryDate = ocr.Object?.ValidDate,
                CardType = ocr.Object?.CardType,
                IsIdFake = isIdFake,
                IsTampered = isTampered
            };

            // Evaluate decision
            var overallScore = faceMatchScore;
            var decision = EvaluateDecision(
                faceMatchScore, isCardLive, isFaceLive, isIdFake, isTampered, isFaceMatch);

            string? rejectionReason = null;
            if (decision == EkycDecision.Rejected)
            {
                var reasons = new List<string>();
                if (!isCardLive) reasons.Add("Giấy tờ không thật");
                if (!isFaceLive) reasons.Add("Khuôn mặt không thật");
                if (!isFaceMatch) reasons.Add($"Khuôn mặt không khớp ({faceMatchScore:F1}%)");
                if (isIdFake) reasons.Add("Số ID giả");
                if (isTampered) reasons.Add("Giấy tờ bị chỉnh sửa");
                rejectionReason = string.Join("; ", reasons);
            }

            var rawJson = JsonSerializer.Serialize(rawResponses);

            return new EkycVerificationResult
            {
                Decision = decision,
                OverallScore = overallScore,
                ProviderName = ProviderName,
                OcrData = ocrData,
                FaceMatchScore = faceMatchScore,
                IsCardLive = isCardLive,
                IsFaceLive = isFaceLive,
                RejectionReason = rejectionReason,
                Warnings = warnings,
                RawResponse = rawJson
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "VNPT eKYC HTTP error");
            return Error.Unavailable("Ekyc.HttpError", $"eKYC provider communication error: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "VNPT eKYC timeout");
            return Error.Timeout("Ekyc.Timeout", "eKYC provider request timed out.");
        }
    }

    private EkycDecision EvaluateDecision(
        decimal faceMatchScore,
        bool isCardLive,
        bool isFaceLive,
        bool isIdFake,
        bool isTampered,
        bool isFaceMatch)
    {
        if (isIdFake || isTampered || !isCardLive || !isFaceLive)
            return EkycDecision.Rejected;

        if (!isFaceMatch || faceMatchScore < (decimal)_options.RejectThreshold)
            return EkycDecision.Rejected;

        if (faceMatchScore >= (decimal)_options.ApproveThreshold && isFaceMatch)
            return EkycDecision.Approved;

        return EkycDecision.NeedsReview;
    }

    #region VNPT API Calls

    private async Task<Result<string, Error>> UploadImageFromUrlAsync(
        string imageUrl, string title, CancellationToken ct)
    {
        _logger.LogDebug("Downloading image for VNPT upload: {Title} from {Url}", title, imageUrl);

        byte[] imageBytes;
        try
        {
            using var downloadClient = new HttpClient();
            imageBytes = await downloadClient.GetByteArrayAsync(imageUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download image from {Url}", imageUrl);
            return Error.Unavailable("Ekyc.DownloadFailed", $"Failed to download image for {title}.");
        }

        _logger.LogDebug("Downloaded {Bytes} bytes for {Title}, uploading to VNPT...", imageBytes.Length, title);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", $"{title}.jpg");
        content.Add(new StringContent(title), "title");
        content.Add(new StringContent($"eKYC {title}"), "description");

        var request = CreateRequest(HttpMethod.Post, "/file-service/v1/addFile");
        request.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "VNPT upload HTTP error for {Title}. BaseUrl={BaseUrl}",
                title, _options.BaseUrl);
            return Error.Unavailable("Ekyc.UploadFailed",
                $"Cannot connect to eKYC provider for {title}: {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("VNPT upload failed for {Title}: {Status} - {Body}", title, response.StatusCode, body);
            return Error.Unavailable("Ekyc.UploadFailed", $"Failed to upload {title} to eKYC provider.");
        }

        var result = JsonSerializer.Deserialize<VnptUploadResponse>(body);
        if (result?.Message != SuccessMessage || result.Object?.Hash is null)
        {
            _logger.LogError("VNPT upload response invalid for {Title}: {Body}", title, body);
            return Error.Unexpected("Ekyc.UploadFailed", $"Invalid upload response for {title}.");
        }

        _logger.LogDebug("VNPT upload success for {Title}: {Hash}", title, result.Object.Hash);
        return result.Object.Hash;
    }

    private async Task<Result<VnptOcrResponse, Error>> OcrDocumentAsync(
        string frontHash, string? backHash, string clientSession, string token, CancellationToken ct)
    {
        var endpoint = backHash is not null ? "/ai/v1/ocr/id" : "/ai/v1/ocr/id/front";
        var requestBody = new VnptOcrRequest
        {
            ImgFront = frontHash,
            ImgBack = backHash,
            ClientSession = clientSession,
            Type = -1,
            ValidatePostcode = true,
            Token = token
        };

        return await PostJsonAsync<VnptOcrRequest, VnptOcrResponse>(endpoint, requestBody, "OCR", ct);
    }

    private async Task<Result<VnptCardLivenessResponse, Error>> CheckCardLivenessAsync(
        string imageHash, string clientSession, CancellationToken ct)
    {
        var requestBody = new VnptCardLivenessRequest
        {
            Img = imageHash,
            ClientSession = clientSession
        };

        return await PostJsonAsync<VnptCardLivenessRequest, VnptCardLivenessResponse>(
            "/ai/v1/card/liveness", requestBody, "CardLiveness", ct);
    }

    private async Task<Result<VnptFaceCompareResponse, Error>> CompareFacesAsync(
        string idFrontHash, string selfieHash, string clientSession, string token, CancellationToken ct)
    {
        var requestBody = new VnptFaceCompareRequest
        {
            ImgFront = idFrontHash,
            ImgFace = selfieHash,
            ClientSession = clientSession,
            Token = token
        };

        return await PostJsonAsync<VnptFaceCompareRequest, VnptFaceCompareResponse>(
            "/ai/v1/face/compare", requestBody, "FaceCompare", ct);
    }

    private async Task<Result<VnptFaceLivenessResponse, Error>> CheckFaceLivenessAsync(
        string selfieHash, string clientSession, string token, CancellationToken ct)
    {
        var requestBody = new VnptFaceLivenessRequest
        {
            Img = selfieHash,
            ClientSession = clientSession,
            Token = token
        };

        return await PostJsonAsync<VnptFaceLivenessRequest, VnptFaceLivenessResponse>(
            "/ai/v1/face/liveness", requestBody, "FaceLiveness", ct);
    }

    #endregion

    #region HTTP Helpers

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{_options.BaseUrl}{path}");
        request.Headers.Add("Authorization", $"Bearer {_options.AccessToken}");
        request.Headers.Add("Token-id", _options.TokenId);
        request.Headers.Add("Token-key", _options.TokenKey);
        request.Headers.Add("mac-address", _options.MacAddress);
        return request;
    }

    private async Task<Result<TResponse, Error>> PostJsonAsync<TRequest, TResponse>(
        string path, TRequest body, string operationName, CancellationToken ct)
    {
        var request = CreateRequest(HttpMethod.Post, path);
        request.Content = JsonContent.Create(body);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "VNPT {Operation} HTTP error. Url={Url}",
                operationName, $"{_options.BaseUrl}{path}");
            return Error.Unavailable($"Ekyc.{operationName}Failed",
                $"Cannot connect to eKYC provider for {operationName}: {ex.Message}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("VNPT {Operation} failed: {Status} - {Body}", operationName, response.StatusCode, responseBody);
            return Error.Unavailable($"Ekyc.{operationName}Failed", $"eKYC {operationName} request failed.");
        }

        var result = JsonSerializer.Deserialize<TResponse>(responseBody);
        if (result is null)
        {
            _logger.LogError("VNPT {Operation} deserialization failed: {Body}", operationName, responseBody);
            return Error.Unexpected($"Ekyc.{operationName}Failed", $"Invalid response from eKYC {operationName}.");
        }

        return result;
    }

    #endregion
}
