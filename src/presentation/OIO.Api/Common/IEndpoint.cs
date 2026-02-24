namespace OIO.Api.Common;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}