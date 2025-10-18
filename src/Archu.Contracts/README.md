# Archu.Contracts

## Overview
The Contracts project contains data transfer objects (DTOs) and request/response models that define the API contract. This project is shared between the API and clients.

## Target Framework
- .NET 9.0

## Responsibilities
- **Request Models**: Input data structures for API operations
- **Response Models**: Output data structures from API operations
- **DTOs**: Data transfer objects for various features

## Key Components

### Products
- **CreateProductRequest**: Request model for creating new products
- **UpdateProductRequest**: Request model for updating existing products
- **ProductDto**: Data transfer object representing a product

## Design Principles
- **API Contract**: Defines the shape of data exchanged with clients
- **Validation**: Can include data annotations for input validation
- **Versioning**: Consider organizing by API version (v1, v2, etc.)
- **Immutability**: Prefer immutable records for DTOs

## Usage
This project can be:
- Referenced by `Archu.Api` for API controllers
- Shared with client applications (Blazor, Mobile, etc.)
- Published as a NuGet package for external consumers

## Best Practices
- Keep contracts simple and focused
- Avoid exposing domain entities directly
- Use appropriate data annotations for validation
- Consider using records for immutability
- Document properties with XML comments

## Future Enhancements
Consider adding:
- **Validation Attributes**: For input validation
- **API Versioning**: Organize contracts by version
- **Common Response Models**: Standardized error and success responses
- **Pagination Models**: For list endpoints
