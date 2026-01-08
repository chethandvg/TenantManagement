using TentMan.Contracts.Buildings;
using TentMan.Contracts.Common;
using TentMan.Contracts.Units;

namespace TentMan.ApiClient.Services;

/// <summary>
/// Interface for the Buildings API client.
/// </summary>
public interface IBuildingsApiClient
{
    /// <summary>
    /// Gets all buildings for an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of buildings.</returns>
    Task<ApiResponse<IEnumerable<BuildingListDto>>> GetBuildingsAsync(
        Guid orgId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a building by its identifier.
    /// </summary>
    /// <param name="id">The building identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The building details.</returns>
    Task<ApiResponse<BuildingDetailDto>> GetBuildingAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new building.
    /// </summary>
    /// <param name="request">The building creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created building.</returns>
    Task<ApiResponse<BuildingDetailDto>> CreateBuildingAsync(
        CreateBuildingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing building.
    /// </summary>
    /// <param name="id">The building identifier.</param>
    /// <param name="request">The building update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated building.</returns>
    Task<ApiResponse<BuildingDetailDto>> UpdateBuildingAsync(
        Guid id,
        UpdateBuildingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the address for a building.
    /// </summary>
    /// <param name="id">The building identifier.</param>
    /// <param name="request">The address request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated building.</returns>
    Task<ApiResponse<BuildingDetailDto>> SetBuildingAddressAsync(
        Guid id,
        SetBuildingAddressRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets ownership shares for a building.
    /// </summary>
    /// <param name="id">The building identifier.</param>
    /// <param name="request">The ownership request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated building.</returns>
    Task<ApiResponse<BuildingDetailDto>> SetBuildingOwnershipAsync(
        Guid id,
        SetBuildingOwnershipRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a file to a building.
    /// </summary>
    /// <param name="id">The building identifier.</param>
    /// <param name="request">The file request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated building.</returns>
    Task<ApiResponse<BuildingDetailDto>> AddBuildingFileAsync(
        Guid id,
        AddFileRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new unit in a building.
    /// </summary>
    /// <param name="buildingId">The building identifier.</param>
    /// <param name="request">The unit creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created unit.</returns>
    Task<ApiResponse<UnitListDto>> CreateUnitAsync(
        Guid buildingId,
        CreateUnitRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple units in a building.
    /// </summary>
    /// <param name="buildingId">The building identifier.</param>
    /// <param name="request">The bulk unit creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created units.</returns>
    Task<ApiResponse<List<UnitListDto>>> BulkCreateUnitsAsync(
        Guid buildingId,
        BulkCreateUnitsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all units for a building.
    /// </summary>
    /// <param name="buildingId">The building identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of units.</returns>
    Task<ApiResponse<IEnumerable<UnitListDto>>> GetUnitsAsync(
        Guid buildingId,
        CancellationToken cancellationToken = default);
}
