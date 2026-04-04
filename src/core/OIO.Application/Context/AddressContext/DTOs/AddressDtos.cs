namespace OIO.Application.Context.AddressContext.DTOs;

public sealed record ProvinceDto(int ProvinceId, string ProvinceName);

public sealed record DistrictDto(int DistrictId, string DistrictName, int ProvinceId);

public sealed record WardDto(string WardCode, string WardName, int DistrictId);
