import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="customer-detail-container">
      <h1>Customer Details</h1>
      <mat-card>
        <mat-card-content>
          <p>Customer ID: {{customerId()}}</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .customer-detail-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }
  `]
})
export class CustomerDetailComponent implements OnInit {
  customerId = signal('');

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.customerId.set(params['id']);
    });
  }
}