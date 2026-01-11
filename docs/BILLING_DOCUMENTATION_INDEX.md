# Billing Engine Documentation Index - TentMan

Complete index and navigation guide for all billing engine documentation.

---

## 📚 Documentation Overview

The TentMan billing engine documentation is organized into specialized guides covering different aspects of the system:

### **For Developers**
- Technical architecture and implementation details
- Database schema and entity relationships
- Service layer documentation
- API reference

### **For Administrators**
- User interface guides
- Business rules and workflows
- Configuration and setup

### **For Business Users**
- Billing workflows and processes
- User guides for daily operations

---

## 📖 Complete Documentation Set

### 1. **Technical Documentation**

#### [Billing Engine Guide](BILLING_ENGINE.md)
**Primary technical reference for the billing system**

**Contents**:
- Complete database schema with all tables and fields
- Entity relationships (ER diagram textual representation)
- Enums and their values
- Database indexes for performance
- Migrations history
- Frontend UI components overview
- Background jobs and automation
- Edge case handling implementation
- Future enhancements roadmap

**Audience**: Backend developers, database administrators, system architects

**Use When**: Understanding database design, implementing new features, troubleshooting data issues

---

#### [Billing Services README](../src/TentMan.Application/Billing/README.md)
**Service layer implementation details**

**Contents**:
- Invoice generation service
- Invoice run service (batch billing)
- Invoice management service (issue/void)
- Credit note service
- Proration calculators (ActualDays vs ThirtyDay)
- Rent calculation service
- Recurring charge calculation service
- Utility calculation service
- Number generation services
- Testing information
- Edge case handling
- Design decisions

**Audience**: Backend developers implementing billing logic

**Use When**: Implementing invoice generation, understanding calculation logic, writing tests

---

#### [API Reference](BILLING_API_REFERENCE.md)
**Complete REST API endpoint documentation**

**Contents**:
- Authentication and authorization
- Billing settings endpoints
- Recurring charges endpoints
- Charge types endpoints
- Utility statements endpoints
- Invoices endpoints
- Invoice runs endpoints
- Credit notes endpoints
- Request/response examples
- Error handling
- Validation rules

**Audience**: Frontend developers, API consumers, integration developers

**Use When**: Implementing UI features, integrating with external systems, testing API

---

#### [Background Jobs](BACKGROUND_JOBS.md)
**Automated billing job processing**

**Contents**:
- Hangfire architecture
- Monthly rent generation job (runs 26th @ 2 AM UTC)
- Utility billing job (runs Monday @ 3 AM UTC)
- Job configuration and scheduling
- Monitoring and management via Hangfire dashboard
- Idempotency and error handling
- Multi-tenant processing

**Audience**: DevOps, system administrators, backend developers

**Use When**: Configuring automated billing, troubleshooting job failures, monitoring execution

---

### 2. **User Documentation**

#### [Billing UI Guide](BILLING_UI_GUIDE.md)
**Complete user interface documentation**

**Contents**:
- Billing dashboard usage
- Lease billing settings configuration
- Recurring charges management
- Utility statements entry
- Invoice management (list, detail, actions)
- Invoice runs execution and monitoring
- Credit notes creation
- Tenant portal - My Bills section
- Common workflows and best practices
- Troubleshooting

**Audience**: Property managers, administrators, billing staff

**Use When**: Daily billing operations, setting up new leases, generating invoices, handling disputes

---

#### [Billing Business Rules](BILLING_BUSINESS_RULES.md)
**Business rules, timing conventions, and policies**

**Contents**:
- **Rent Timing Rules**: Advance vs arrears billing, India-specific conventions
- **Utility Timing Rules**: Always arrears, meter-based vs amount-based
- **Proration Rules**: ActualDaysInMonth vs ThirtyDayMonth methods with examples
- **Immutability Rules**: Invoice state transitions, when invoices can/cannot be modified
- **Credit Note Workflow**: When to use, creation process, effects on invoices
- **Invoice Lifecycle**: Complete state diagram (Draft → Issued → Paid → etc.)
- **Billing Day Rules**: Valid days (1-28), February handling
- **Tax Rules**: GST/tax application, taxable charge types
- **Edge Cases**: Comprehensive list of handled scenarios

**Audience**: Business analysts, managers, administrators, new team members

**Use When**: Understanding billing policies, configuring billing settings, resolving disputes, training users

---

### 3. **Reference Documentation**

#### [Database Guide](DATABASE_GUIDE.md)
**General database documentation (covers all modules)**

**Contents** (Billing-related sections):
- Connection string configuration
- Entity Framework Core usage
- Migration commands
- Seeding data
- Query optimization

**Audience**: Database administrators, backend developers

---

#### [Architecture Guide](ARCHITECTURE.md)
**Overall system architecture (covers all modules)**

**Contents** (Billing-related sections):
- Clean architecture layers
- CQRS pattern (where applicable)
- Dependency injection
- Repository pattern

**Audience**: Solution architects, senior developers

---

#### [API Guide](API_GUIDE.md)
**General API documentation (covers all modules)**

**Contents** (Billing-related sections):
- API versioning strategy
- Authentication/authorization patterns
- Error handling conventions
- Response format standards

**Audience**: API developers, frontend developers

---

## 🗺️ Quick Navigation by Task

### **Setting Up Billing for a New Lease**

1. Start: [Billing UI Guide - Configure New Lease Billing](BILLING_UI_GUIDE.md#workflow-2-configure-new-lease-billing)
2. Reference: [Billing Business Rules - Billing Day Rules](BILLING_BUSINESS_RULES.md#billing-day-rules)
3. API: [API Reference - Billing Settings](BILLING_API_REFERENCE.md#billing-settings)

---

### **Generating Monthly Invoices**

1. Start: [Billing UI Guide - Monthly Billing Process](BILLING_UI_GUIDE.md#workflow-1-monthly-billing-process)
2. Automated: [Background Jobs - Monthly Rent Generation](BACKGROUND_JOBS.md)
3. Manual: [API Reference - Invoice Runs](BILLING_API_REFERENCE.md#invoice-runs)
4. Rules: [Business Rules - Rent Timing Rules](BILLING_BUSINESS_RULES.md#rent-timing-rules)

---

### **Recording Utility Consumption**

1. Start: [Billing UI Guide - Record Utility Consumption](BILLING_UI_GUIDE.md#workflow-4-record-utility-consumption)
2. API: [API Reference - Utility Statements](BILLING_API_REFERENCE.md#utility-statements)
3. Rules: [Business Rules - Utility Timing Rules](BILLING_BUSINESS_RULES.md#utility-timing-rules)

---

### **Handling Billing Disputes**

1. Start: [Billing UI Guide - Handle Billing Dispute](BILLING_UI_GUIDE.md#workflow-3-handle-billing-dispute)
2. Process: [Business Rules - Credit Note Workflow](BILLING_BUSINESS_RULES.md#credit-note-workflow)
3. API: [API Reference - Credit Notes](BILLING_API_REFERENCE.md#credit-notes)

---

### **Understanding Proration**

1. Overview: [Business Rules - Proration Rules](BILLING_BUSINESS_RULES.md#proration-rules)
2. Implementation: [Billing Services - Proration Calculators](../src/TentMan.Application/Billing/README.md#proration-calculators)
3. Edge Cases: [Business Rules - Edge Cases](BILLING_BUSINESS_RULES.md#edge-cases-and-exceptions)

---

### **Implementing New Billing Features**

1. Architecture: [Billing Engine Guide - Overview](BILLING_ENGINE.md)
2. Services: [Billing Services README](../src/TentMan.Application/Billing/README.md)
3. Database: [Billing Engine Guide - Database Schema](BILLING_ENGINE.md#database-schema)
4. API: [API Reference](BILLING_API_REFERENCE.md)
5. Testing: [Billing Services - Testing](../src/TentMan.Application/Billing/README.md#testing)

---

### **Troubleshooting Issues**

1. UI Issues: [Billing UI Guide - Troubleshooting](BILLING_UI_GUIDE.md#troubleshooting)
2. Job Failures: [Background Jobs - Monitoring](BACKGROUND_JOBS.md)
3. Business Logic: [Business Rules - Edge Cases](BILLING_BUSINESS_RULES.md#edge-cases-and-exceptions)
4. Edge Cases: [Billing Engine - Edge Case Handling](BILLING_ENGINE.md#edge-case-handling)

---

## 📋 Document Relationships

```
BILLING_ENGINE.md (Technical Foundation)
    ├─ Database Schema
    ├─ Entity Relationships
    └─ Migrations
    
../src/TentMan.Application/Billing/README.md (Service Layer)
    ├─ Invoice Generation
    ├─ Calculations
    └─ Business Logic
    
BILLING_API_REFERENCE.md (API Contracts)
    ├─ Endpoints
    ├─ Request/Response
    └─ Validation
    
BILLING_UI_GUIDE.md (User Interface)
    ├─ Component Usage
    ├─ Workflows
    └─ Best Practices
    
BILLING_BUSINESS_RULES.md (Business Logic)
    ├─ Timing Rules
    ├─ Proration
    ├─ State Transitions
    └─ Edge Cases
    
BACKGROUND_JOBS.md (Automation)
    ├─ Job Scheduling
    └─ Monitoring
```

---

## 🎯 By Role

### **Property Managers / Billing Staff**

**Primary Documents**:
1. [Billing UI Guide](BILLING_UI_GUIDE.md) - Daily operations
2. [Billing Business Rules](BILLING_BUSINESS_RULES.md) - Policies and procedures

**Optional**:
- [API Reference](BILLING_API_REFERENCE.md) - If integrating with external tools

---

### **Frontend Developers**

**Primary Documents**:
1. [API Reference](BILLING_API_REFERENCE.md) - Endpoint contracts
2. [Billing UI Guide](BILLING_UI_GUIDE.md) - Existing UI patterns

**Optional**:
- [Business Rules](BILLING_BUSINESS_RULES.md) - Business logic understanding
- [Billing Engine](BILLING_ENGINE.md) - Data model

---

### **Backend Developers**

**Primary Documents**:
1. [Billing Engine Guide](BILLING_ENGINE.md) - Database and architecture
2. [Billing Services README](../src/TentMan.Application/Billing/README.md) - Service implementation
3. [API Reference](BILLING_API_REFERENCE.md) - API contracts
4. [Business Rules](BILLING_BUSINESS_RULES.md) - Business logic requirements

**Optional**:
- [Background Jobs](BACKGROUND_JOBS.md) - Automated processes
- [Billing UI Guide](BILLING_UI_GUIDE.md) - User workflows

---

### **QA / Testers**

**Primary Documents**:
1. [Billing UI Guide](BILLING_UI_GUIDE.md) - User workflows to test
2. [Business Rules](BILLING_BUSINESS_RULES.md) - Edge cases and validation rules
3. [API Reference](BILLING_API_REFERENCE.md) - API testing

**Optional**:
- [Billing Engine - Edge Cases](BILLING_ENGINE.md#edge-case-handling) - Test scenarios

---

### **DevOps / System Administrators**

**Primary Documents**:
1. [Background Jobs](BACKGROUND_JOBS.md) - Job configuration and monitoring
2. [Billing Engine - Migrations](BILLING_ENGINE.md#migrations) - Database updates

**Optional**:
- [API Reference](BILLING_API_REFERENCE.md) - Endpoint health checking

---

### **Business Analysts / Product Owners**

**Primary Documents**:
1. [Billing Business Rules](BILLING_BUSINESS_RULES.md) - Complete business logic
2. [Billing UI Guide](BILLING_UI_GUIDE.md) - User workflows
3. [Billing Engine - Features](BILLING_ENGINE.md#features) - Capabilities overview

---

## 🔍 By Topic

### **Rent Billing**
- [Business Rules - Rent Timing Rules](BILLING_BUSINESS_RULES.md#rent-timing-rules)
- [Services - Rent Calculation](../src/TentMan.Application/Billing/README.md#rent-calculation-service)
- [Background Jobs - Monthly Rent](BACKGROUND_JOBS.md)

### **Utility Billing**
- [Business Rules - Utility Timing Rules](BILLING_BUSINESS_RULES.md#utility-timing-rules)
- [Services - Utility Calculation](../src/TentMan.Application/Billing/README.md#utility-calculation-service)
- [API - Utility Statements](BILLING_API_REFERENCE.md#utility-statements)
- [UI - Utility Statements](BILLING_UI_GUIDE.md#utility-statements)

### **Proration**
- [Business Rules - Proration Rules](BILLING_BUSINESS_RULES.md#proration-rules)
- [Services - Proration Calculators](../src/TentMan.Application/Billing/README.md#proration-calculators)

### **Invoices**
- [Engine - Invoice Tables](BILLING_ENGINE.md#invoice-tables)
- [Services - Invoice Generation](../src/TentMan.Application/Billing/README.md#invoice-generation-service)
- [API - Invoices](BILLING_API_REFERENCE.md#invoices)
- [UI - Invoice Management](BILLING_UI_GUIDE.md#invoice-management)
- [Business Rules - Invoice Lifecycle](BILLING_BUSINESS_RULES.md#invoice-lifecycle)

### **Credit Notes**
- [Engine - Credit Note Tables](BILLING_ENGINE.md#credit-note-tables)
- [Services - Credit Note Service](../src/TentMan.Application/Billing/README.md#credit-note-service)
- [API - Credit Notes](BILLING_API_REFERENCE.md#credit-notes)
- [Business Rules - Credit Note Workflow](BILLING_BUSINESS_RULES.md#credit-note-workflow)

### **Automation**
- [Background Jobs](BACKGROUND_JOBS.md)
- [Engine - Background Jobs Section](BILLING_ENGINE.md#background-jobs--automation)
- [UI - Invoice Runs](BILLING_UI_GUIDE.md#invoice-runs)

### **Edge Cases**
- [Business Rules - Edge Cases](BILLING_BUSINESS_RULES.md#edge-cases-and-exceptions)
- [Engine - Edge Case Handling](BILLING_ENGINE.md#edge-case-handling)
- [Services - Edge Cases](../src/TentMan.Application/Billing/README.md#edge-case-handling)

---

## 📝 Document Versions

| Document | Last Updated | Version |
|----------|--------------|---------|
| BILLING_ENGINE.md | 2026-01-11 | Latest |
| BILLING_UI_GUIDE.md | 2026-01-11 | 1.0 |
| BILLING_BUSINESS_RULES.md | 2026-01-11 | 1.0 |
| BILLING_API_REFERENCE.md | 2026-01-11 | 1.0 |
| BACKGROUND_JOBS.md | 2026-01-11 | Latest |
| ../src/TentMan.Application/Billing/README.md | 2026-01-11 | Latest |

---

## 🆕 Recent Updates

### 2026-01-11
- ✅ Created comprehensive API Reference
- ✅ Created Billing Business Rules documentation
- ✅ Enhanced all billing DTOs with XML comments
- ✅ Created this documentation index

### Previous Updates
- Edge case handling implementation and documentation
- Utility statement versioning
- Invoice immutability rules
- Background jobs with Hangfire
- Complete UI component documentation
- Database schema and migrations

---

## 🔗 External Resources

- **GitHub Repository**: https://github.com/chethandvg/TenantManagement
- **Issues**: https://github.com/chethandvg/TenantManagement/issues
- **Billing Engine Feature**: Issue #45
- **Hangfire Dashboard**: `/hangfire` (when application is running)
- **API Documentation**: `/swagger` (when application is running, if Swagger UI is enabled)

---

## 📞 Support

For questions or issues related to billing engine documentation:
- Review this index to find the appropriate document
- Check the specific guide for detailed information
- Search the codebase for implementation details
- Report documentation gaps or errors as GitHub issues with `documentation` label

---

**Last Updated**: 2026-01-11  
**Version**: 1.0  
**Maintained By**: TentMan Development Team
