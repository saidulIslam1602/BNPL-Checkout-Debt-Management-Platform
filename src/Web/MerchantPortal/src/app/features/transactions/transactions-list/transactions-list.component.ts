import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSelectModule } from '@angular/material/select';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-transactions-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatSelectModule,
    RouterModule
  ],
  template: `
    <div class="transactions-container">
      <div class="header-section">
        <h1>Transactions</h1>
        <p class="subtitle">Monitor and manage your BNPL transactions</p>
      </div>

      <mat-card class="filters-card">
        <mat-card-content>
          <div class="filters-row">
            <mat-form-field appearance="outline">
              <mat-label>Search</mat-label>
              <input matInput [(ngModel)]="searchTerm" placeholder="Order ID, Customer...">
              <mat-icon matSuffix>search</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Status</mat-label>
              <mat-select [(ngModel)]="statusFilter">
                <mat-option value="">All Statuses</mat-option>
                <mat-option value="pending">Pending</mat-option>
                <mat-option value="completed">Completed</mat-option>
                <mat-option value="failed">Failed</mat-option>
                <mat-option value="cancelled">Cancelled</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Date Range</mat-label>
              <mat-date-range-input>
                <input matStartDate [(ngModel)]="startDate" placeholder="Start date">
                <input matEndDate [(ngModel)]="endDate" placeholder="End date">
              </mat-date-range-input>
            </mat-form-field>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card class="transactions-table-card">
        <mat-card-content>
          <table mat-table [dataSource]="transactions()" class="transactions-table">
            <ng-container matColumnDef="orderId">
              <th mat-header-cell *matHeaderCellDef>Order ID</th>
              <td mat-cell *matCellDef="let transaction">{{transaction.orderId}}</td>
            </ng-container>

            <ng-container matColumnDef="customer">
              <th mat-header-cell *matHeaderCellDef>Customer</th>
              <td mat-cell *matCellDef="let transaction">{{transaction.customerName}}</td>
            </ng-container>

            <ng-container matColumnDef="amount">
              <th mat-header-cell *matHeaderCellDef>Amount</th>
              <td mat-cell *matCellDef="let transaction">
                {{transaction.amount | currency:'NOK':'symbol':'1.2-2'}}
              </td>
            </ng-container>

            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let transaction">
                <mat-chip [color]="getStatusColor(transaction.status)">
                  {{transaction.status | titlecase}}
                </mat-chip>
              </td>
            </ng-container>

            <ng-container matColumnDef="date">
              <th mat-header-cell *matHeaderCellDef>Date</th>
              <td mat-cell *matCellDef="let transaction">{{transaction.createdAt | date:'short'}}</td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let transaction">
                <button mat-icon-button [routerLink]="['/transactions', transaction.id]">
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
    .transactions-container {
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

    .filters-card {
      margin-bottom: 2rem;
    }

    .filters-row {
      display: grid;
      grid-template-columns: 2fr 1fr 2fr;
      gap: 1rem;
      
      @media (max-width: 768px) {
        grid-template-columns: 1fr;
      }
    }

    .transactions-table {
      width: 100%;
    }
  `]
})
export class TransactionsListComponent implements OnInit {
  displayedColumns = ['orderId', 'customer', 'amount', 'status', 'date', 'actions'];
  searchTerm = '';
  statusFilter = '';
  startDate: Date | null = null;
  endDate: Date | null = null;

  transactions = signal([
    {
      id: '1',
      orderId: 'ORD-12345678',
      customerName: 'John Doe',
      amount: 15000,
      status: 'completed',
      createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000)
    },
    {
      id: '2',
      orderId: 'ORD-87654321',
      customerName: 'Jane Smith',
      amount: 8500,
      status: 'pending',
      createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000)
    }
  ]);

  ngOnInit() {
    this.loadTransactions();
  }

  loadTransactions() {
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