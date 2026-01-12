# Payment UI Updates

## Overview
Added payment recording functionality to the Invoice Detail page, enabling owners and managers to record payments via a user-friendly dialog interface.

## Changes Made

### Updated Pages
- **InvoiceDetail.razor**: Invoice detail page with payment functionality
  - New "Record Payment" button (visible for Issued, PartiallyPaid, Overdue invoices)
  - Payment History section displaying all recorded payments
  - Payment recording dialog with payment mode selection

### UI Components

#### Record Payment Dialog
- **Payment Mode Selection**: Dropdown with options
  - Cash
  - Online
  - Bank Transfer
  - UPI
  - Cheque
  - Other
- **Amount Input**: Numeric field with currency formatting (defaults to remaining balance)
- **Payment Date**: Date picker (defaults to today)
- **Payer Name**: Optional text field
- **Transaction Reference**: Required for non-cash payments (bank ref, UPI ID, cheque number, etc.)
- **Receipt Number**: Optional for cash payments
- **Notes**: Optional multiline text field

#### Payment History Table
Displays all payments for the invoice with columns:
- **Date**: Payment date and time (local time)
- **Mode**: Payment mode chip (Cash, Online, etc.)
- **Amount**: Formatted currency amount
- **Status**: Status chip with color coding
  - Completed (Green)
  - Pending (Yellow)
  - Processing (Blue)
  - Failed (Red)
  - Cancelled (Gray)
  - Refunded (Secondary)
- **Payer**: Name of person who made the payment
- **Reference**: Transaction reference or receipt number
- **Notes**: Additional notes

### API Client Updates
- **IBillingApiClient**: Added payment methods
  - RecordCashPaymentAsync
  - RecordOnlinePaymentAsync
  - GetInvoicePaymentsAsync
- **BillingApiClient**: Implemented payment methods

### User Experience
- Auto-populates payment amount with remaining invoice balance
- Validates payment amount against remaining balance
- Requires transaction reference for non-cash payments
- Real-time invoice status updates after payment recording
- Payment history refreshes automatically after recording
- Snackbar notifications for success/error feedback
