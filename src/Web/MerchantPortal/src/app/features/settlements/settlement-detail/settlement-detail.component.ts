import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-settlement-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="settlement-detail-container">
      <h1>Settlement Details</h1>
      <mat-card>
        <mat-card-content>
          <p>Settlement ID: {{settlementId()}}</p>
          <!-- TODO: Add settlement details -->
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .settlement-detail-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }
  `]
})
export class SettlementDetailComponent implements OnInit {
  settlementId = signal('');

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.settlementId.set(params['id']);
    });
  }
}