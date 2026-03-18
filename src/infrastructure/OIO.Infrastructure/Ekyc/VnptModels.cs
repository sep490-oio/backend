using System.Text.Json.Serialization;

namespace OIO.Infrastructure.Ekyc;

#region Upload File

internal sealed class VnptUploadResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public VnptUploadObject? Object { get; set; }
}

internal sealed class VnptUploadObject
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = string.Empty;
}

#endregion

#region OCR

internal sealed class VnptOcrRequest
{
    [JsonPropertyName("img_front")]
    public string ImgFront { get; set; } = string.Empty;

    [JsonPropertyName("img_back")]
    public string? ImgBack { get; set; }

    [JsonPropertyName("client_session")]
    public string ClientSession { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; set; } = -1;

    [JsonPropertyName("validate_postcode")]
    public bool ValidatePostcode { get; set; } = true;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

internal sealed class VnptOcrResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public VnptOcrObject? Object { get; set; }
}

internal sealed class VnptOcrObject
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("name_prob")]
    public double NameProb { get; set; }

    [JsonPropertyName("birth_day")]
    public string? BirthDay { get; set; }

    [JsonPropertyName("birth_day_prob")]
    public double BirthDayProb { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    [JsonPropertyName("nation")]
    public string? Nation { get; set; }

    [JsonPropertyName("origin_location")]
    public string? OriginLocation { get; set; }

    [JsonPropertyName("recent_location")]
    public string? RecentLocation { get; set; }

    [JsonPropertyName("valid_date")]
    public string? ValidDate { get; set; }

    [JsonPropertyName("issue_date")]
    public string? IssueDate { get; set; }

    [JsonPropertyName("issue_place")]
    public string? IssuePlace { get; set; }

    [JsonPropertyName("card_type")]
    public string? CardType { get; set; }

    [JsonPropertyName("type_id")]
    public int TypeId { get; set; }

    [JsonPropertyName("id_fake_prob")]
    public double IdFakeProb { get; set; }

    [JsonPropertyName("id_fake_warning")]
    public string? IdFakeWarning { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("msg_back")]
    public string? MsgBack { get; set; }

    [JsonPropertyName("tampering")]
    public VnptTamperingObject? Tampering { get; set; }

    [JsonPropertyName("warning")]
    public List<string>? Warning { get; set; }

    [JsonPropertyName("warning_msg")]
    public List<string>? WarningMsg { get; set; }

    [JsonPropertyName("expire_warning")]
    public string? ExpireWarning { get; set; }
    
    [JsonPropertyName("post_code")]
    public List<VnptPostCodeObject>? PostCode { get; set; }
}

internal sealed class VnptTamperingObject
{
    [JsonPropertyName("is_legal")]
    public string? IsLegal { get; set; }

    [JsonPropertyName("warning")]
    public List<string>? Warning { get; set; }
}

internal sealed class VnptPostCodeObject
{   
    [JsonPropertyName("city")]
    public List<object>? City { get; set; }
    
    [JsonPropertyName("district")]
    public List<object>? District { get; set; }
    
    [JsonPropertyName("ward")]
    public List<object>? Ward { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

#endregion

#region Card Liveness

internal sealed class VnptCardLivenessRequest
{
    [JsonPropertyName("img")]
    public string Img { get; set; } = string.Empty;

    [JsonPropertyName("client_session")]
    public string ClientSession { get; set; } = string.Empty;
}

internal sealed class VnptCardLivenessResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public VnptCardLivenessObject? Object { get; set; }
}

internal sealed class VnptCardLivenessObject
{
    [JsonPropertyName("liveness")]
    public string? Liveness { get; set; }

    [JsonPropertyName("liveness_msg")]
    public string? LivenessMsg { get; set; }

    [JsonPropertyName("face_swapping")]
    public bool FaceSwapping { get; set; }

    [JsonPropertyName("fake_liveness")]
    public bool FakeLiveness { get; set; }
}

#endregion

#region Face Compare

internal sealed class VnptFaceCompareRequest
{
    [JsonPropertyName("img_front")]
    public string ImgFront { get; set; } = string.Empty;

    [JsonPropertyName("img_face")]
    public string ImgFace { get; set; } = string.Empty;

    [JsonPropertyName("client_session")]
    public string ClientSession { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

internal sealed class VnptFaceCompareResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public VnptFaceCompareObject? Object { get; set; }
}

internal sealed class VnptFaceCompareObject
{
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("prob")]
    public double Prob { get; set; }
}

#endregion

#region Face Liveness

internal sealed class VnptFaceLivenessRequest
{
    [JsonPropertyName("img")]
    public string Img { get; set; } = string.Empty;

    [JsonPropertyName("client_session")]
    public string ClientSession { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

internal sealed class VnptFaceLivenessResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public VnptFaceLivenessObject? Object { get; set; }
}

internal sealed class VnptFaceLivenessObject
{
    [JsonPropertyName("liveness")]
    public string? Liveness { get; set; }

    [JsonPropertyName("liveness_msg")]
    public string? LivenessMsg { get; set; }

    [JsonPropertyName("is_eye_open")]
    public string? IsEyeOpen { get; set; }
}

#endregion

#region Error

internal sealed class VnptErrorResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("statusCode")]
    public string? StatusCode { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }
}

#endregion
