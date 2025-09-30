import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface CheckoutSession {
  sessionId: string;
  merchantId: string;
  merchantName: string;
  items: CheckoutItem[];
  totalAmount: number;
  currency: string;
  returnUrl: string;
  cancelUrl: string;
  webhookUrl?: string;
}

export interface CheckoutItem {
  id: string;
  name: string;
  description: string;
  price: number;
  quantity: number;
  imageUrl: string;
  merchantName: string;
  category?: string;
  sku?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CheckoutService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/checkout`;

  /**
   * Initialize checkout session
   */
  initializeSession(sessionData: Partial<CheckoutSession>): Observable<CheckoutSession> {
    if (environment.features.mockData) {
      return of({
        sessionId: 'sess_' + Math.random().toString(36).substr(2, 9),
        merchantId: 'merchant_123',
        merchantName: 'Elkjøp',
        items: sessionData.items || [],
        totalAmount: sessionData.totalAmount || 0,
        currency: 'NOK',
        returnUrl: sessionData.returnUrl || window.location.origin + '/checkout/success',
        cancelUrl: sessionData.cancelUrl || window.location.origin + '/checkout/cancel'
      }).pipe(delay(500));
    }

    return this.http.post<CheckoutSession>(`${this.baseUrl}/initialize`, sessionData);
  }

  /**
   * Get checkout session by ID
   */
  getSession(sessionId: string): Observable<CheckoutSession> {
    if (environment.features.mockData) {
      return of({
        sessionId,
        merchantId: 'merchant_123',
        merchantName: 'Elkjøp',
        items: [
          {
            id: '1',
            name: 'iPhone 15 Pro 128GB',
            description: 'Titanium Natural',
            price: 13990,
            quantity: 1,
            imageUrl: 'assets/images/iphone-15-pro.jpg',
            merchantName: 'Elkjøp',
            category: 'Electronics',
            sku: 'IPH15PRO128TN'
          },
          {
            id: '2',
            name: 'AirPods Pro (2nd generation)',
            description: 'Med MagSafe Charging Case',
            price: 2990,
            quantity: 1,
            imageUrl: 'assets/images/airpods-pro.jpg',
            merchantName: 'Elkjøp',
            category: 'Electronics',
            sku: 'AIRPODSPRO2'
          }
        ],
        totalAmount: 16980,
        currency: 'NOK',
        returnUrl: window.location.origin + '/checkout/success',
        cancelUrl: window.location.origin + '/checkout/cancel'
      }).pipe(delay(500));
    }

    return this.http.get<CheckoutSession>(`${this.baseUrl}/session/${sessionId}`);
  }

  /**
   * Update checkout session
   */
  updateSession(sessionId: string, updates: Partial<CheckoutSession>): Observable<CheckoutSession> {
    if (environment.features.mockData) {
      return this.getSession(sessionId).pipe(delay(300));
    }

    return this.http.patch<CheckoutSession>(`${this.baseUrl}/session/${sessionId}`, updates);
  }

  /**
   * Calculate shipping cost
   */
  calculateShipping(items: CheckoutItem[], address: any): Observable<number> {
    if (environment.features.mockData) {
      // Simple shipping calculation
      const totalWeight = items.reduce((sum, item) => sum + (item.quantity * 0.5), 0); // Assume 0.5kg per item
      const shippingCost = totalWeight > 2 ? 99 : 49; // Free shipping over certain weight
      return of(shippingCost).pipe(delay(200));
    }

    return this.http.post<number>(`${this.baseUrl}/calculate-shipping`, { items, address });
  }

  /**
   * Calculate tax (Norwegian VAT)
   */
  calculateTax(subtotal: number): number {
    return Math.round(subtotal * 0.25); // 25% Norwegian VAT
  }

  /**
   * Validate Norwegian postal code
   */
  validatePostalCode(postalCode: string): Observable<{ valid: boolean; city?: string }> {
    if (environment.features.mockData) {
      const norwegianPostalCodes: { [key: string]: string } = {
        '0001': 'Oslo',
        '0010': 'Oslo',
        '0015': 'Oslo',
        '0020': 'Oslo',
        '0030': 'Oslo',
        '0050': 'Oslo',
        '0150': 'Oslo',
        '0151': 'Oslo',
        '0152': 'Oslo',
        '0153': 'Oslo',
        '0154': 'Oslo',
        '0155': 'Oslo',
        '0156': 'Oslo',
        '0157': 'Oslo',
        '0158': 'Oslo',
        '0159': 'Oslo',
        '0160': 'Oslo',
        '0161': 'Oslo',
        '0162': 'Oslo',
        '0163': 'Oslo',
        '0164': 'Oslo',
        '0165': 'Oslo',
        '0166': 'Oslo',
        '0167': 'Oslo',
        '0168': 'Oslo',
        '0169': 'Oslo',
        '0170': 'Oslo',
        '0171': 'Oslo',
        '0172': 'Oslo',
        '0173': 'Oslo',
        '0174': 'Oslo',
        '0175': 'Oslo',
        '0176': 'Oslo',
        '0177': 'Oslo',
        '0178': 'Oslo',
        '0179': 'Oslo',
        '0180': 'Oslo',
        '0181': 'Oslo',
        '0182': 'Oslo',
        '0183': 'Oslo',
        '0184': 'Oslo',
        '0185': 'Oslo',
        '0186': 'Oslo',
        '0187': 'Oslo',
        '0188': 'Oslo',
        '0189': 'Oslo',
        '0190': 'Oslo',
        '0191': 'Oslo',
        '0192': 'Oslo',
        '0193': 'Oslo',
        '0194': 'Oslo',
        '0195': 'Oslo',
        '0196': 'Oslo',
        '0197': 'Oslo',
        '0198': 'Oslo',
        '0199': 'Oslo',
        '5020': 'Bergen',
        '5021': 'Bergen',
        '5022': 'Bergen',
        '5023': 'Bergen',
        '5024': 'Bergen',
        '5025': 'Bergen',
        '5026': 'Bergen',
        '5027': 'Bergen',
        '5028': 'Bergen',
        '5029': 'Bergen',
        '5030': 'Bergen',
        '5031': 'Bergen',
        '5032': 'Bergen',
        '5033': 'Bergen',
        '5034': 'Bergen',
        '5035': 'Bergen',
        '5036': 'Bergen',
        '5037': 'Bergen',
        '5038': 'Bergen',
        '5039': 'Bergen',
        '5040': 'Bergen',
        '5041': 'Bergen',
        '5042': 'Bergen',
        '5043': 'Bergen',
        '5044': 'Bergen',
        '5045': 'Bergen',
        '5046': 'Bergen',
        '5047': 'Bergen',
        '5048': 'Bergen',
        '5049': 'Bergen',
        '5050': 'Bergen',
        '5051': 'Bergen',
        '5052': 'Bergen',
        '5053': 'Bergen',
        '5054': 'Bergen',
        '5055': 'Bergen',
        '5056': 'Bergen',
        '5057': 'Bergen',
        '5058': 'Bergen',
        '5059': 'Bergen',
        '5060': 'Bergen',
        '5061': 'Bergen',
        '5062': 'Bergen',
        '5063': 'Bergen',
        '5064': 'Bergen',
        '5065': 'Bergen',
        '5066': 'Bergen',
        '5067': 'Bergen',
        '5068': 'Bergen',
        '5069': 'Bergen',
        '5070': 'Bergen',
        '5071': 'Bergen',
        '5072': 'Bergen',
        '5073': 'Bergen',
        '5074': 'Bergen',
        '5075': 'Bergen',
        '5076': 'Bergen',
        '5077': 'Bergen',
        '5078': 'Bergen',
        '5079': 'Bergen',
        '5080': 'Bergen',
        '5081': 'Bergen',
        '5082': 'Bergen',
        '5083': 'Bergen',
        '5084': 'Bergen',
        '5085': 'Bergen',
        '5086': 'Bergen',
        '5087': 'Bergen',
        '5088': 'Bergen',
        '5089': 'Bergen',
        '5090': 'Bergen',
        '5091': 'Bergen',
        '5092': 'Bergen',
        '5093': 'Bergen',
        '5094': 'Bergen',
        '5095': 'Bergen',
        '5096': 'Bergen',
        '5097': 'Bergen',
        '5098': 'Bergen',
        '5099': 'Bergen',
        '7010': 'Trondheim',
        '7011': 'Trondheim',
        '7012': 'Trondheim',
        '7013': 'Trondheim',
        '7014': 'Trondheim',
        '7015': 'Trondheim',
        '7016': 'Trondheim',
        '7017': 'Trondheim',
        '7018': 'Trondheim',
        '7019': 'Trondheim',
        '7020': 'Trondheim',
        '7021': 'Trondheim',
        '7022': 'Trondheim',
        '7023': 'Trondheim',
        '7024': 'Trondheim',
        '7025': 'Trondheim',
        '7026': 'Trondheim',
        '7027': 'Trondheim',
        '7028': 'Trondheim',
        '7029': 'Trondheim',
        '7030': 'Trondheim',
        '7031': 'Trondheim',
        '7032': 'Trondheim',
        '7033': 'Trondheim',
        '7034': 'Trondheim',
        '7035': 'Trondheim',
        '7036': 'Trondheim',
        '7037': 'Trondheim',
        '7038': 'Trondheim',
        '7039': 'Trondheim',
        '7040': 'Trondheim',
        '7041': 'Trondheim',
        '4010': 'Stavanger',
        '4011': 'Stavanger',
        '4012': 'Stavanger',
        '4013': 'Stavanger',
        '4014': 'Stavanger',
        '4015': 'Stavanger',
        '4016': 'Stavanger',
        '4017': 'Stavanger',
        '4018': 'Stavanger',
        '4019': 'Stavanger',
        '4020': 'Stavanger',
        '4021': 'Stavanger',
        '4022': 'Stavanger',
        '4023': 'Stavanger',
        '4024': 'Stavanger',
        '4025': 'Stavanger',
        '4026': 'Stavanger',
        '4027': 'Stavanger',
        '4028': 'Stavanger',
        '4029': 'Stavanger',
        '4030': 'Stavanger',
        '4031': 'Stavanger',
        '4032': 'Stavanger',
        '4033': 'Stavanger',
        '4034': 'Stavanger',
        '4035': 'Stavanger',
        '4036': 'Stavanger',
        '4037': 'Stavanger',
        '4038': 'Stavanger',
        '4039': 'Stavanger',
        '4040': 'Stavanger',
        '4041': 'Stavanger',
        '4042': 'Stavanger',
        '4043': 'Stavanger',
        '4044': 'Stavanger',
        '4045': 'Stavanger',
        '4046': 'Stavanger',
        '4047': 'Stavanger',
        '4048': 'Stavanger',
        '4049': 'Stavanger',
        '4050': 'Stavanger',
        '4051': 'Stavanger',
        '4052': 'Stavanger',
        '4053': 'Stavanger',
        '4054': 'Stavanger',
        '4055': 'Stavanger',
        '4056': 'Stavanger',
        '4057': 'Stavanger',
        '4058': 'Stavanger',
        '4059': 'Stavanger',
        '4060': 'Stavanger',
        '4061': 'Stavanger',
        '4062': 'Stavanger',
        '4063': 'Stavanger',
        '4064': 'Stavanger',
        '4065': 'Stavanger',
        '4066': 'Stavanger',
        '4067': 'Stavanger',
        '4068': 'Stavanger',
        '4069': 'Stavanger',
        '4070': 'Stavanger',
        '4071': 'Stavanger',
        '4072': 'Stavanger',
        '4073': 'Stavanger',
        '4074': 'Stavanger',
        '4075': 'Stavanger',
        '4076': 'Stavanger',
        '4077': 'Stavanger',
        '4078': 'Stavanger',
        '4079': 'Stavanger',
        '4080': 'Stavanger',
        '4081': 'Stavanger',
        '4082': 'Stavanger',
        '4083': 'Stavanger',
        '4084': 'Stavanger',
        '4085': 'Stavanger',
        '4086': 'Stavanger',
        '4087': 'Stavanger',
        '4088': 'Stavanger',
        '4089': 'Stavanger',
        '4090': 'Stavanger',
        '4091': 'Stavanger',
        '4092': 'Stavanger',
        '4093': 'Stavanger',
        '4094': 'Stavanger',
        '4095': 'Stavanger',
        '4096': 'Stavanger',
        '4097': 'Stavanger',
        '4098': 'Stavanger',
        '4099': 'Stavanger'
      };

      const city = norwegianPostalCodes[postalCode];
      return of({
        valid: !!city,
        city: city
      }).pipe(delay(300));
    }

    return this.http.get<{ valid: boolean; city?: string }>(`${this.baseUrl}/validate-postal-code/${postalCode}`);
  }

  /**
   * Get merchant information
   */
  getMerchantInfo(merchantId: string): Observable<any> {
    if (environment.features.mockData) {
      return of({
        id: merchantId,
        name: 'Elkjøp',
        logo: 'assets/images/merchants/elkjop-logo.svg',
        description: 'Norges største elektronikkjede',
        website: 'https://elkjop.no',
        supportEmail: 'kundeservice@elkjop.no',
        supportPhone: '+47 815 35 100'
      }).pipe(delay(200));
    }

    return this.http.get(`${this.baseUrl}/merchant/${merchantId}`);
  }
}