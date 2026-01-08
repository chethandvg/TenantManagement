using TentMan.Contracts.Buildings;
using TentMan.Contracts.Common;
using TentMan.Contracts.Units;
using Microsoft.Extensions.Logging;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Implementation of the Buildings API client.
/// </summary>
public sealed class BuildingsApiClient : ApiClientServiceBase, IBuildingsApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuildingsApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public BuildingsApiClient(HttpClient httpClient, ILogger<BuildingsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc/>
    protected override string BasePath => "api/buildings";

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<BuildingListDto>>> GetBuildingsAsync(
        Guid orgId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<BuildingListDto>>(
            $"?orgId={orgId}",
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<BuildingDetailDto>> GetBuildingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<BuildingDetailDto>($"{id}", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<BuildingDetailDto>> CreateBuildingAsync(
        CreateBuildingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateBuildingRequest, BuildingDetailDto>(string.Empty, request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<BuildingDetailDto>> UpdateBuildingAsync(
        Guid id,
        UpdateBuildingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<UpdateBuildingRequest, BuildingDetailDto>($"{id}", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<BuildingDetailDto>> SetBuildingAddressAsync(
        Guid id,
        SetBuildingAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<SetBuildingAddressRequest, BuildingDetailDto>($"{id}/address", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<BuildingDetailDto>> SetBuildingOwnershipAsync(
        Guid id,
        SetBuildingOwnershipRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PutAsync<SetBuildingOwnershipRequest, BuildingDetailDto>($"{id}/ownership-shares", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<BuildingDetailDto>> AddBuildingFileAsync(
        Guid id,
        AddFileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<AddFileRequest, BuildingDetailDto>($"{id}/files", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<UnitListDto>> CreateUnitAsync(
        Guid buildingId,
        CreateUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<CreateUnitRequest, UnitListDto>($"{buildingId}/units", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<List<UnitListDto>>> BulkCreateUnitsAsync(
        Guid buildingId,
        BulkCreateUnitsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return PostAsync<BulkCreateUnitsRequest, List<UnitListDto>>($"{buildingId}/units/bulk", request, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ApiResponse<IEnumerable<UnitListDto>>> GetUnitsAsync(
        Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<IEnumerable<UnitListDto>>($"{buildingId}/units", cancellationToken);
    }
}
