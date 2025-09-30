export interface Payment {
  id: string;
  orderNumber: string;
  amount: number;
  currency: string;
  status: 'pending' | 'completed' | 'failed' | 'processing' | 'cancelled';
  totalInstallments: number;
  paidInstallments: number;
  nextPaymentDate: Date;
  createdAt: Date;
  updatedAt: Date;
}

export interface PaymentStatus {
  paymentId: string;
  status: 'failed' | 'pending' | 'completed' | 'processing' | 'cancelled';
  amount: number;
  currency: string;
  createdAt: Date;
  updatedAt: Date;
  failureReason?: string;
}

export interface BNPLPaymentResponse {
  success: boolean;
  orderNumber: string;
  paymentId: string;
  status: 'pending' | 'approved' | 'declined';
  nextPaymentDate: Date;
  errorMessage?: string;
  redirectUrl?: string;
}

export interface BNPLPaymentRequest {
  amount: number;
  currency: string;
  merchantId: string;
  customerId: string;
  items: CartItem[];
  billingAddress: Address;
  shippingAddress?: Address;
  bnplOption: string;
  total: number;
  terms: any;
}

export interface CartItem {
  id: string;
  name: string;
  price: number;
  quantity: number;
  imageUrl?: string;
}

export interface Address {
  street: string;
  city: string;
  postalCode: string;
  country: string;
}