export interface Transaction {
  id: string;
  merchantId: string;
  customerId: string;
  amount: number;
  currency: Currency;
  status: TransactionStatus;
  paymentMethod: PaymentMethod;
  bnplPlan?: BNPLPlan;
  orderReference?: string;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
  processedAt?: Date;
  settledAt?: Date;
  customer: Customer;
  installments?: Installment[];
  refunds?: Refund[];
  events: TransactionEvent[];
}

export enum Currency {
  NOK = 'NOK',
  EUR = 'EUR',
  USD = 'USD'
}

export enum TransactionStatus {
  PENDING = 'PENDING',
  PROCESSING = 'PROCESSING',
  COMPLETED = 'COMPLETED',
  FAILED = 'FAILED',
  CANCELLED = 'CANCELLED',
  REFUNDED = 'REFUNDED',
  PARTIALLY_REFUNDED = 'PARTIALLY_REFUNDED'
}

export enum PaymentMethod {
  CREDIT_CARD = 'CREDIT_CARD',
  DEBIT_CARD = 'DEBIT_CARD',
  BANK_TRANSFER = 'BANK_TRANSFER',
  VIPPS = 'VIPPS',
  BNPL = 'BNPL'
}

export interface BNPLPlan {
  type: BNPLPlanType;
  installmentCount: number;
  installmentAmount: number;
  interestRate: number;
  downPayment: number;
  totalAmount: number;
  firstPaymentDate: Date;
  lastPaymentDate: Date;
}

export enum BNPLPlanType {
  PAY_IN_3 = 'PAY_IN_3',
  PAY_IN_4 = 'PAY_IN_4',
  PAY_IN_6 = 'PAY_IN_6',
  PAY_IN_12 = 'PAY_IN_12',
  PAY_IN_24 = 'PAY_IN_24'
}

export interface Customer {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  dateOfBirth?: Date;
  address?: Address;
  creditScore?: number;
  riskLevel: RiskLevel;
  totalSpent: number;
  totalTransactions: number;
  registeredAt: Date;
}

export interface Address {
  street: string;
  city: string;
  postalCode: string;
  country: string;
}

export enum RiskLevel {
  LOW = 'LOW',
  MEDIUM = 'MEDIUM',
  HIGH = 'HIGH',
  VERY_HIGH = 'VERY_HIGH'
}

export interface Installment {
  id: string;
  transactionId: string;
  amount: number;
  dueDate: Date;
  status: InstallmentStatus;
  paidAt?: Date;
  attemptCount: number;
  lastAttemptAt?: Date;
}

export enum InstallmentStatus {
  PENDING = 'PENDING',
  PAID = 'PAID',
  OVERDUE = 'OVERDUE',
  FAILED = 'FAILED',
  CANCELLED = 'CANCELLED'
}

export interface Refund {
  id: string;
  transactionId: string;
  amount: number;
  reason: string;
  status: RefundStatus;
  processedAt?: Date;
  createdAt: Date;
}

export enum RefundStatus {
  PENDING = 'PENDING',
  PROCESSING = 'PROCESSING',
  COMPLETED = 'COMPLETED',
  FAILED = 'FAILED'
}

export interface TransactionEvent {
  id: string;
  transactionId: string;
  type: TransactionEventType;
  description: string;
  metadata?: Record<string, any>;
  createdAt: Date;
}

export enum TransactionEventType {
  CREATED = 'CREATED',
  PAYMENT_INITIATED = 'PAYMENT_INITIATED',
  PAYMENT_COMPLETED = 'PAYMENT_COMPLETED',
  PAYMENT_FAILED = 'PAYMENT_FAILED',
  REFUND_INITIATED = 'REFUND_INITIATED',
  REFUND_COMPLETED = 'REFUND_COMPLETED',
  INSTALLMENT_PAID = 'INSTALLMENT_PAID',
  INSTALLMENT_FAILED = 'INSTALLMENT_FAILED',
  RISK_ASSESSMENT_COMPLETED = 'RISK_ASSESSMENT_COMPLETED',
  FRAUD_DETECTED = 'FRAUD_DETECTED',
  SETTLEMENT_PROCESSED = 'SETTLEMENT_PROCESSED'
}

// Search and filter interfaces
export interface TransactionSearchRequest {
  merchantId?: string;
  customerId?: string;
  status?: TransactionStatus[];
  paymentMethod?: PaymentMethod[];
  amountMin?: number;
  amountMax?: number;
  dateFrom?: Date;
  dateTo?: Date;
  orderReference?: string;
  customerEmail?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface TransactionSearchResponse {
  transactions: Transaction[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}