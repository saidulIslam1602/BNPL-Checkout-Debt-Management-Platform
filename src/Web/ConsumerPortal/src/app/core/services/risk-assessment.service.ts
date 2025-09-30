import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface RiskAssessmentRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: Date;
  socialSecurityNumber: string;
  address: {
    street: string;
    city: string;
    postalCode: string;
    country: string;
  };
  requestedAmount: number;
  merchantId?: string;
  sessionId?: string;
}

export interface RiskAssessmentResponse {
  assessmentId: string;
  creditScore: number;
  riskLevel: 'low' | 'medium' | 'high';
  approvalStatus: 'approved' | 'declined' | 'manual_review';
  maxApprovedAmount: number;
  recommendedBNPLOptions: string[];
  declineReason?: string;
  factors: {
    creditHistory: number;
    incomeStability: number;
    debtToIncomeRatio: number;
    paymentHistory: number;
    fraudRisk: number;
  };
  externalChecks: {
    creditBureau: boolean;
    fraudDatabase: boolean;
    identityVerification: boolean;
    addressVerification: boolean;
  };
}

@Injectable({
  providedIn: 'root'
})
export class RiskAssessmentService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/risk-assessment`;

  /**
   * Assess customer for BNPL eligibility
   */
  assessCustomer(request: RiskAssessmentRequest): Observable<RiskAssessmentResponse> {
    if (environment.features.mockData) {
      // Simulate risk assessment based on requested amount and SSN
      const ssn = request.socialSecurityNumber.replace(/\s/g, '');
      const lastDigit = parseInt(ssn.slice(-1));
      const requestedAmount = request.requestedAmount;

      // Simple risk scoring simulation
      let creditScore = 600 + (lastDigit * 15); // Base score 600-750
      let riskLevel: 'low' | 'medium' | 'high' = 'medium';
      let approvalStatus: 'approved' | 'declined' | 'manual_review' = 'approved';
      let maxApprovedAmount = requestedAmount;

      // Adjust based on amount
      if (requestedAmount > 30000) {
        creditScore -= 50;
      } else if (requestedAmount > 15000) {
        creditScore -= 25;
      }

      // Determine risk level and approval
      if (creditScore >= 700) {
        riskLevel = 'low';
        maxApprovedAmount = Math.min(50000, requestedAmount * 1.2);
      } else if (creditScore >= 600) {
        riskLevel = 'medium';
        maxApprovedAmount = Math.min(25000, requestedAmount);
      } else {
        riskLevel = 'high';
        if (creditScore < 550) {
          approvalStatus = 'declined';
          maxApprovedAmount = 0;
        } else {
          approvalStatus = 'manual_review';
          maxApprovedAmount = Math.min(10000, requestedAmount * 0.8);
        }
      }

      // Determine recommended BNPL options
      const recommendedOptions: string[] = [];
      if (approvalStatus === 'approved') {
        if (creditScore >= 700) {
          recommendedOptions.push('PAY_IN_3', 'PAY_IN_4');
          if (requestedAmount <= 20000) {
            recommendedOptions.push('PAY_IN_6');
          }
        } else if (creditScore >= 650) {
          recommendedOptions.push('PAY_IN_3');
          if (requestedAmount <= 15000) {
            recommendedOptions.push('PAY_IN_4', 'PAY_IN_6');
          }
        } else {
          recommendedOptions.push('PAY_IN_3');
          if (requestedAmount <= 10000) {
            recommendedOptions.push('PAY_IN_6', 'PAY_IN_12');
          }
        }
      }

      const response: RiskAssessmentResponse = {
        assessmentId: 'RISK-' + Math.random().toString(36).substr(2, 9).toUpperCase(),
        creditScore,
        riskLevel,
        approvalStatus,
        maxApprovedAmount,
        recommendedBNPLOptions: recommendedOptions,
        declineReason: approvalStatus === 'declined' ? 'Insufficient credit history' : undefined,
        factors: {
          creditHistory: Math.min(100, creditScore / 7),
          incomeStability: Math.min(100, 60 + (lastDigit * 4)),
          debtToIncomeRatio: Math.max(0, 100 - (requestedAmount / 1000)),
          paymentHistory: Math.min(100, creditScore / 7.5),
          fraudRisk: Math.max(0, 20 - lastDigit)
        },
        externalChecks: {
          creditBureau: true,
          fraudDatabase: true,
          identityVerification: true,
          addressVerification: true
        }
      };

      return of(response).pipe(delay(2500)); // Simulate processing time
    }

    return this.http.post<RiskAssessmentResponse>(`${this.baseUrl}/assess`, request);
  }

  /**
   * Get assessment details
   */
  getAssessment(assessmentId: string): Observable<RiskAssessmentResponse> {
    if (environment.features.mockData) {
      return of({
        assessmentId,
        creditScore: 720,
        riskLevel: 'low' as const,
        approvalStatus: 'approved' as const,
        maxApprovedAmount: 50000,
        recommendedBNPLOptions: ['PAY_IN_3', 'PAY_IN_4', 'PAY_IN_6'],
        factors: {
          creditHistory: 85,
          incomeStability: 78,
          debtToIncomeRatio: 92,
          paymentHistory: 88,
          fraudRisk: 5
        },
        externalChecks: {
          creditBureau: true,
          fraudDatabase: true,
          identityVerification: true,
          addressVerification: true
        }
      }).pipe(delay(500));
    }

    return this.http.get<RiskAssessmentResponse>(`${this.baseUrl}/${assessmentId}`);
  }

  /**
   * Validate Norwegian Social Security Number
   */
  validateNorwegianSSN(ssn: string): Observable<{ valid: boolean; message?: string }> {
    if (environment.features.mockData) {
      const cleanSSN = ssn.replace(/\s/g, '');
      
      // Basic Norwegian SSN validation (simplified)
      if (cleanSSN.length !== 11) {
        return of({
          valid: false,
          message: 'Personnummer må være 11 siffer'
        }).pipe(delay(200));
      }

      if (!/^\d{11}$/.test(cleanSSN)) {
        return of({
          valid: false,
          message: 'Personnummer kan kun inneholde tall'
        }).pipe(delay(200));
      }

      // Check birth date part (DDMMYY)
      const day = parseInt(cleanSSN.substr(0, 2));
      const month = parseInt(cleanSSN.substr(2, 2));
      const year = parseInt(cleanSSN.substr(4, 2));

      if (day < 1 || day > 31 || month < 1 || month > 12) {
        return of({
          valid: false,
          message: 'Ugyldig fødselsdato i personnummer'
        }).pipe(delay(200));
      }

      return of({ valid: true }).pipe(delay(200));
    }

    return this.http.post<{ valid: boolean; message?: string }>(`${this.baseUrl}/validate-ssn`, { ssn });
  }

  /**
   * Check fraud database
   */
  checkFraudDatabase(email: string, phoneNumber: string): Observable<{ flagged: boolean; reason?: string }> {
    if (environment.features.mockData) {
      // Simulate fraud check - flag certain test emails
      const flaggedEmails = ['fraud@test.com', 'scammer@example.com'];
      const flagged = flaggedEmails.includes(email.toLowerCase());

      return of({
        flagged,
        reason: flagged ? 'Email address found in fraud database' : undefined
      }).pipe(delay(800));
    }

    return this.http.post<{ flagged: boolean; reason?: string }>(`${this.baseUrl}/fraud-check`, {
      email,
      phoneNumber
    });
  }

  /**
   * Get credit score explanation
   */
  getCreditScoreExplanation(creditScore: number): string {
    if (creditScore >= 750) {
      return 'Utmerket kreditthistorikk. Du kvalifiserer for våre beste BNPL-tilbud.';
    } else if (creditScore >= 700) {
      return 'Meget god kreditthistorikk. Du har tilgang til de fleste BNPL-alternativene.';
    } else if (creditScore >= 650) {
      return 'God kreditthistorikk. Du kvalifiserer for standard BNPL-alternativer.';
    } else if (creditScore >= 600) {
      return 'Akseptabel kreditthistorikk. Begrensede BNPL-alternativer tilgjengelig.';
    } else if (creditScore >= 550) {
      return 'Lav kredittscore. Kun grunnleggende BNPL-alternativer kan være tilgjengelige.';
    } else {
      return 'Meget lav kredittscore. BNPL-alternativer er sannsynligvis ikke tilgjengelige.';
    }
  }

  /**
   * Get risk level description
   */
  getRiskLevelDescription(riskLevel: 'low' | 'medium' | 'high'): string {
    switch (riskLevel) {
      case 'low':
        return 'Lav risiko - Stabil økonomi og god betalingshistorikk';
      case 'medium':
        return 'Middels risiko - Akseptabel økonomi med noen forbehold';
      case 'high':
        return 'Høy risiko - Ustabil økonomi eller dårlig betalingshistorikk';
      default:
        return 'Ukjent risikonivå';
    }
  }
}