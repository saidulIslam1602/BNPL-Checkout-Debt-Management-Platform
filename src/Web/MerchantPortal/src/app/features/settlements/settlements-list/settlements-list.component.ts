import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-settlements-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    RouterModule
  ],
  template: `
    <div class="settlements-container">
      <div class="header-section">
        <h1>Settlements</h1>
        <p class="subtitle">Track your payment settlements and payouts</p>
      </div>

      <mat-card class="settlements-table-card">
        <mat-card-content>
          <table mat-table [dataSource]="settlements()" class="settlements-table">
            <ng-container matColumnDef="settlementId">
              <th mat-header-cell *matHeaderCellDef>Settlement ID</th>
              <td mat-cell *matCellDef="let settlement">{{settlement.id}}</td>
            </ng-container>

            <ng-container matColumnDef="amount">
              <th mat-header-cell *matHeaderCellDef>Amount</th>
              <td mat-cell *matCellDef="let settlement">
                {{settlement.amount | currency:'NOK':'symbol':'1.2-2'}}
              </td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let settlement">
                <mat-chip [color]="getStatusColor(settlement.status)">
                  {{settlement.status | titlecase}}
                </mat-chip>
              </td>
            </ng-container>

            <ng-container matColumnDef="date">
              <th mat-header-cell *matHeaderCellDef>Settlement Date</th>
              <td mat-cell *matCellDef="let settlement">{{settlement.settlementDate | date:'short'}}</td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let settlement">
                <button mat-icon-button [routerLink]="['/settlements', settlement.id]">
                  <mat-icon>visibility</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .settlements-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .header-section {
      text-align: center;
      margin-bottom: 2rem;
      
      h1 {
        font-size: 2.5rem;
        margin-bottom: 1rem;
        color: #1976d2;
      }
      
      .subtitle {
        font-size: 1.1rem;
        color: #666;
      }
    }

    .settlements-table {
      width: 100%;
    }
  `]
})
export class SettlementsListComponent implements OnInit {
  displayedColumns = ['settlementId', 'amount', 'status', 'date', 'actions'];

  settlements = signal([
    {
      id: 'SETT-001',
      amount: 45000,
      status: 'completed',
      settlementDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000)
    },
    {
      id: 'SETT-002',
      amount: 32000,
      status: 'pending',
      settlementDate: new Date()
    }
  ]);

  ngOnInit() {
    this.loadSettlements();
  }

  loadSettlements() {
    // TODO: Load from API
  }

  getStatusColor(status: string): 'primary' | 'accent' | 'warn' {
    switch (status.toLowerCase()) {
      case 'completed': return 'primary';
      case 'pending': return 'accent';
      case 'failed': return 'warn';
      default: return 'primary';
    }
  }
}